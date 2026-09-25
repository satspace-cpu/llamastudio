using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Core.Services;

namespace LlamaStudio.Infrastructure.Llama;

/// <summary>
/// r101: Benchmark Center runner.
/// - Server benchmarks run against the ALREADY RUNNING llama-server over HTTP
///   (they never restart the server).
/// - Engine benchmarks run the llama-bench binary from the SAME llama.cpp build
///   directory as llama-server; capabilities are detected from the real
///   binary's --help (never assumed), JSON output is preferred, markdown table
///   is the fallback parser.
/// - All process work is async (stdout/stderr drained concurrently), cancellable
///   via CancellationToken with process-tree kill, cross-platform (ArgumentList).
/// </summary>
public sealed class BenchmarkRunner
{
	private readonly ILogService _log;
	private readonly ISettings _settings;
	private readonly IGpuMonitor _gpuMonitor;
	private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
	private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(300) };

	public BenchmarkRunner(ISettings settings, ILogService log, IGpuMonitor gpuMonitor)
	{
		_settings = settings;
		_log = log;
		_gpuMonitor = gpuMonitor;
	}

	public static bool IsWindows => OperatingSystem.IsWindows();

	// ==================== llama.cpp build resolution ====================

	/// <summary>
	/// Resolves the running llama-server executable: from the process (PID) when
	/// available — the authoritative "same build" source — otherwise from the
	/// profile/settings directories.
	/// </summary>
	public string? ResolveServerExecutableAsync(ServerProfile profile, int? processId)
	{
		if (processId.HasValue && processId.Value > 0)
		{
			string? fromProc = ExecutableOfProcess(processId.Value);
			if (fromProc != null)
			{
				return fromProc;
			}
		}
		string dir = string.IsNullOrWhiteSpace(profile.LlamaCppBaseDirectory)
			? (_settings.LlamaCppDirectory ?? string.Empty)
			: profile.LlamaCppBaseDirectory;
		string? resolved = null;
		if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
		{
			resolved = BenchmarkPaths.ResolveInDirectory(Path.Combine(dir, _settings.ActiveLlamaCppVersion), "llama-server", IsWindows);
		}
		if (resolved == null)
		{
			resolved = BenchmarkPaths.ResolveInDirectory(dir, "llama-server", IsWindows);
		}
		return resolved;
	}

	/// <summary>Executable path of a running process (Linux: /proc/pid/exe; Windows: MainModule).</summary>
	private static string? ExecutableOfProcess(int pid)
	{
		try
		{
			if (!IsWindows)
			{
				string link = $"/proc/{pid}/exe";
				if (System.IO.File.Exists(link))
				{
					System.IO.FileSystemInfo? target = System.IO.Directory.ResolveLinkTarget(link, returnFinalTarget: false);
					return target?.FullName;
				}
				return null;
			}
			using var proc = Process.GetProcessById(pid);
			return proc.MainModule?.FileName;
		}
		catch
		{
			return null;
		}
	}

	// ==================== environment snapshot ====================

	public BenchmarkEnvironmentSnapshot BuildEnvironmentSnapshot(ServerProfile profile, string? serverExe, string? benchExe, List<BenchmarkGpuSnapshot> gpus)
	{
		var env = new BenchmarkEnvironmentSnapshot
		{
			ProfileId = profile.Id,
			ProfileName = profile.Name,
			ModelPath = profile.ModelPath ?? string.Empty,
			ModelName = string.IsNullOrWhiteSpace(profile.ModelPath) ? string.Empty : Path.GetFileName(profile.ModelPath),
			ContextSize = profile.ContextSize,
			BatchSize = profile.BatchSize,
			UbatchSize = profile.UbatchSize,
			FlashAttention = profile.FlashAttention,
			KvTypeK = profile.CacheTypeK.ToString(),
			KvTypeV = profile.CacheTypeV.ToString(),
			GpuLayers = profile.GetEffectiveGpuLayers(),
			SplitMode = profile.GpuSplitMode.ToString(),
			TensorSplit = string.Join("/", profile.TensorSplit),
			SelectedGpuIndices = string.Join(",", profile.SelectedGpuIndices),
			Threads = profile.Threads,
			Temperature = profile.Temperature,
			MtpEnabled = !string.IsNullOrWhiteSpace(profile.DraftModelPath),
			MtpDraftModel = profile.DraftModelPath ?? string.Empty,
			MtpNMax = profile.SpecDraftNMax,
			ServerPath = serverExe ?? string.Empty,
			BenchPath = benchExe ?? string.Empty,
			LlamaCppBuild = BenchmarkPaths.ExtractBuildNumber(serverExe),
			GpuCount = gpus.Count,
			GpuModels = string.Join("; ", gpus.Select(g => g.Name).Distinct()),
			VramTotalGb = gpus.Sum(g => g.VramTotalGb),
			OperatingSystem = IsWindows ? "Windows" : "Linux",
			CpuCores = Environment.ProcessorCount
		};
		if (System.IO.File.Exists(profile.ModelPath ?? ""))
		{
			try
			{
				env.ModelFileSizeMb = new FileInfo(profile.ModelPath).Length / 1048576.0;
			}
			catch { }
		}
		var quant = Regex.Match(env.ModelName, @"(Q\d+[_A-Z0-9]*|BF16|F16|FP16)", RegexOptions.IgnoreCase);
		if (quant.Success)
		{
			env.ModelQuantization = quant.Value;
		}
		env.OsVersion = ReadOsVersion();
		env.CpuName = ReadCpuName();
		env.RamGb = ReadTotalRamGb();
		if (profile.GpuPowerLimits != null && profile.GpuPowerLimits.Count > 0)
		{
			env.PowerLimitW = string.Join(", ", profile.GpuPowerLimits.Values.Select(v => v + "W"));
		}
		env.Gpus = gpus;
		return env;
	}

	private static string ReadOsVersion()
	{
		try
		{
			if (!IsWindows && File.Exists("/etc/os-release"))
			{
				string text = File.ReadAllText("/etc/os-release");
				var name = Regex.Match(text, "^PRETTY_NAME=\"?([^\"]+)\"?", RegexOptions.Multiline);
				if (name.Success)
				{
					string kernel = "";
					try { kernel = File.ReadAllText("/proc/sys/kernel/osrelease"); } catch { }
					return name.Value.Trim('"').Replace("PRETTY_NAME=", "") + " (" + kernel.Trim() + ")";
				}
			}
			return RuntimeInformation.OSDescription;
		}
		catch
		{
			return RuntimeInformation.OSDescription;
		}
	}

	private static string ReadCpuName()
	{
		try
		{
			if (!IsWindows && File.Exists("/proc/cpuinfo"))
			{
				var m = Regex.Match(File.ReadAllText("/proc/cpuinfo"), "^model name\\s*:\\s*(.+)$", RegexOptions.Multiline);
				if (m.Success)
				{
					return m.Groups[1].Value.Trim();
				}
			}
			return RuntimeInformation.OSArchitecture.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static double ReadTotalRamGb()
	{
		try
		{
			if (!IsWindows && File.Exists("/proc/meminfo"))
			{
				var m = Regex.Match(File.ReadAllText("/proc/meminfo"), "^MemTotal:\\s+(\\d+)\\s+kB", RegexOptions.Multiline);
				if (m.Success && long.TryParse(m.Groups[1].Value, out long kb))
				{
					return kb / 1048576.0;
				}
			}
			if (IsWindows)
			{
				var status = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>() };
				if (GlobalMemoryStatusEx(ref status))
				{
					return status.ullTotalPhys / 1073741824.0;
				}
			}
			return 0;
		}
		catch
		{
			return 0;
		}
	}

	private struct MEMORYSTATUSEX
	{
		public uint dwLength;
		public uint dwMemoryLoad;
		public ulong ullTotalPhys;
		public ulong ullAvailPhys;
		public ulong ullTotalPageFile;
		public ulong ullAvailPageFile;
		public ulong ullTotalVirtual;
		public ulong ullAvailVirtual;
		public ulong ullAvailExtendedVirtual;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx([In, Out] ref MEMORYSTATUSEX lpBuffer);

	// ==================== GPU sampling ====================

	/// <summary>
	/// Samples all GPUs every interval while running; produces per-GPU
	/// avg/peak aggregates for the run snapshot. tok/s/W must be computed from
	/// the actual avg power draw (never from the power limit).
	/// </summary>
	public GpuSampler StartGpuSampling(TimeSpan interval) => new GpuSampler(_gpuMonitor, interval);

	// ==================== Server benchmark (HTTP) ====================

	/// <summary>
	/// Builds a filler prompt targeting ~targetTokens tokens. Token density of
	/// mixed Latin/Cyrillic text is ~3.4 chars/token for Qwen-like tokenizers;
	/// the ACTUAL count always comes from usage.prompt_tokens (never assumed).
	/// </summary>
	public static string BuildFillerPrompt(int targetTokens)
	{
		const string unit = "The quick brown fox jumps over the lazy dog near the riverbank while the old clocktower chimes across the quiet valley at dusk ";
		int chars = (int)(targetTokens * 3.4);
		var sb = new StringBuilder();
		while (sb.Length < chars)
		{
			sb.Append(unit);
		}
		sb.Length = Math.Min(sb.Length, chars);
		return sb.ToString().Trim() + " Continue the pattern briefly:";
	}

	/// <summary>
	/// Quick server test: one real request, streaming. Measures TTFT, prompt
	/// tok/s (server timings), generation tok/s (server timings / token count).
	/// </summary>
	public async Task<BenchmarkPoint> ServerQuickTestAsync(string host, int port, string model, string prompt, int maxTokens, CancellationToken ct)
	{
		return await RunServerRequestAsync(host, port, model, prompt, maxTokens, ct);
	}

	/// <summary>
	/// r106: the Monitoring-page quick test, same flow — two requests:
	/// 1) PREFILL PROBE — a large neutral context (~12K tokens), max_tokens=1,
	///    non-streaming. Prefill speed comes from the server's own
	///    timings.prompt_per_second (same probe the monitoring test uses).
	/// 2) STREAMED GENERATION — the same real prompt as the monitoring test,
	///    max_tokens=1536, stream=true. Generation speed from
	///    timings.predicted_per_second, TTFT from the first chunk.
	/// One BenchmarkPoint: PP (probe) + TG (stream) — both numbers visible.
	/// </summary>
	public async Task<BenchmarkPoint> ServerQuickMonitorAsync(string host, int port, string model, CancellationToken ct)
	{
		string url = "http://" + host + ":" + port + "/v1/completions";
		var totalSw = Stopwatch.StartNew();

		// ---- stage 1: prefill probe (same as the monitoring quick test) ----
		string probe = "Use this context only to measure prompt processing speed.\n"
			+ string.Concat(Enumerable.Repeat("Это нейтральный контекст для измерения скорости обработки входного prompt. Не отвечай подробно. ", 900));
		double prefillTps = 0;
		int probeTokens = 0;
		var probeSw = Stopwatch.StartNew();
		{
			var probePayload = new Dictionary<string, object>
			{
				["model"] = model,
				["prompt"] = probe,
				["max_tokens"] = 1,
				["stream"] = false,
				["cache_prompt"] = false
			};
			using var content = new StringContent(JsonSerializer.Serialize(probePayload), Encoding.UTF8, "application/json");
			using var response = await Http.PostAsync(url, content, ct).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				throw new InvalidOperationException("llama-server HTTP " + (int)response.StatusCode + " (prefill probe): " + body.Substring(0, Math.Min(200, body.Length)));
			}
			string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			using var doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;
			if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var ptv))
			{
				probeTokens = ptv;
			}
			if (root.TryGetProperty("timings", out var timings) && timings.TryGetProperty("prompt_per_second", out var pps) && pps.TryGetDouble(out var p))
			{
				prefillTps = p;
			}
		}
		probeSw.Stop();
		int probeUsed = probeTokens > 0 ? probeTokens : (int)(probe.Length / 3.4);
		if (prefillTps <= 0 && probeSw.ElapsedMilliseconds > 0)
		{
			prefillTps = probeUsed / (probeSw.ElapsedMilliseconds / 1000.0);
		}
		_log.Information("Quick test (Monitoring-like): prefill probe = " + probeUsed + " tokens, " + prefillTps.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " tok/s", "Benchmark");

		// ---- stage 2: streamed generation (same request as the monitoring quick test) ----
		string genPrompt = "Build a single-file HTML visualization of this screenshot using three.js. Include OrbitControls. Create an accurate, detailed representation. Make the edges of most furniture visibly rounded, to make the objects look smoother and more realistic. Use high-quality lighting while ensuring visibility like in the screenshot. Prevent Z-fighting. Ensure code correctness.";
		var payload = new Dictionary<string, object>
		{
			["model"] = model,
			["prompt"] = genPrompt,
			["temperature"] = 0.7,
			["top_p"] = 0.9,
			["max_tokens"] = 1536,
			["stream"] = true,
			["stream_options"] = new Dictionary<string, object> { ["include_usage"] = true },
			["cache_prompt"] = false
		};
		var genSw = Stopwatch.StartNew();
		double? ttftMs = null;
		int generatedTokens = 0;
		int genPromptTokens = 0;
		double? serverGenTps = null;
		using (var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"))
		using (var response = await Http.PostAsync(url, content, ct).ConfigureAwait(false))
		{
			if (!response.IsSuccessStatusCode)
			{
				string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				throw new InvalidOperationException("llama-server HTTP " + (int)response.StatusCode + " (generation): " + body.Substring(0, Math.Min(200, body.Length)));
			}
			using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
			using var reader = new StreamReader(stream, Encoding.UTF8);
			while (!reader.EndOfStream)
			{
				string line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
				{
					continue;
				}
				string data = line.Substring(5).Trim();
				if (data == "[DONE]")
				{
					break;
				}
				try
				{
					using var doc = JsonDocument.Parse(data);
					JsonElement root = doc.RootElement;
					if (root.TryGetProperty("usage", out var usage))
					{
						if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var ptv)) genPromptTokens = ptv;
						if (usage.TryGetProperty("completion_tokens", out var cte) && cte.TryGetInt32(out var ctv)) generatedTokens = Math.Max(generatedTokens, ctv);
					}
					if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
					{
						JsonElement choice = choices[0];
						if ((choice.TryGetProperty("text", out var text) && !string.IsNullOrEmpty(text.GetString()))
							|| (choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var dc) && !string.IsNullOrEmpty(dc.GetString())))
						{
							if (ttftMs == null)
							{
								ttftMs = genSw.Elapsed.TotalMilliseconds;
							}
							generatedTokens++;
						}
					}
					if (root.TryGetProperty("timings", out var timings) && timings.ValueKind == JsonValueKind.Object)
					{
						if (timings.TryGetProperty("predicted_per_second", out var gps) && gps.TryGetDouble(out var g)) serverGenTps = g;
					}
				}
				catch (JsonException)
				{
					// partial/malformed SSE line — skip
				}
				if ((generatedTokens & 31) == 31)
				{
					await Task.Delay(1, ct).ConfigureAwait(false);
				}
			}
		}
		genSw.Stop();

		double genWallMs = genSw.Elapsed.TotalMilliseconds;
		double genElapsedMs = ttftMs.HasValue ? Math.Max(1, genWallMs - ttftMs.Value) : 0;
		double genTps = serverGenTps ?? (generatedTokens > 0 && genElapsedMs > 0 ? generatedTokens / (genElapsedMs / 1000.0) : 0);
		double e2eTps = generatedTokens > 0 ? generatedTokens / (totalSw.Elapsed.TotalMilliseconds / 1000.0) : 0;
		_log.Information("Quick test (Monitoring-like): TG = " + genTps.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " tok/s (" + generatedTokens + " tok, TTFT " + (ttftMs ?? 0).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms)", "Benchmark");

		return new BenchmarkPoint
		{
			Label = "SERVER",
			Tag = "SERVER",
			ContextRequested = probeUsed, // prefill was measured on the probe context
			PromptTokensActual = probeUsed,
			GeneratedTokens = generatedTokens,
			PrefillTps = prefillTps,
			GenerationTps = genTps,
			E2eTps = e2eTps,
			TtftMs = ttftMs ?? 0,
			WallTimeS = totalSw.Elapsed.TotalMilliseconds / 1000.0,
			MsPerToken = genTps > 0 ? 1000.0 / genTps : 0
		};
	}

	/// <summary>
	/// Prefill point: long filler prompt, max_tokens=1, cache_prompt=false.
	/// Returns actual prompt tokens + prompt tok/s from server timings.
	/// If the real tokenization overshoots the request by more than 5%, the
	/// prompt is rescaled and retried once (spec: never exceed safe context).
	/// </summary>
	public async Task<BenchmarkPoint> ServerPrefillPointAsync(string host, int port, string model, int targetTokens, CancellationToken ct)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			string prompt = BuildFillerPrompt(targetTokens);
			var point = await RunServerRequestAsync(host, port, model, prompt, 1, ct);
			if (point.PromptTokensActual <= targetTokens * 1.05)
			{
				return point;
			}
			targetTokens = Math.Max(256, (int)(targetTokens * (double)targetTokens / Math.Max(1, point.PromptTokensActual)));
		}
		// Should not reach here (2nd attempt returns whatever it measured).
		return await RunServerRequestAsync(host, port, model, BuildFillerPrompt(targetTokens), 1, ct);
	}

	/// <summary>
	/// Context depth point: a prompt of ~depthTokens, then generate outputTokens.
	/// Generation tok/s is the SERVER's timings.predicted_per_second when
	/// present (authoritative), otherwise measured from streamed tokens.
	/// </summary>
	public async Task<BenchmarkPoint> ServerDepthPointAsync(string host, int port, string model, int depthTokens, int outputTokens, CancellationToken ct)
	{
		string prompt = BuildFillerPrompt(depthTokens) + "\nNow write a short continuation.";
		var point = await RunServerRequestAsync(host, port, model, prompt, outputTokens, ct);
		point.ContextRequested = depthTokens;
		return point;
	}

	private async Task<BenchmarkPoint> RunServerRequestAsync(string host, int port, string model, string prompt, int maxTokens, CancellationToken ct)
	{
		string url = "http://" + host + ":" + port + "/v1/completions";
		var payload = new Dictionary<string, object>
		{
			["model"] = model,
			["prompt"] = prompt,
			["max_tokens"] = maxTokens,
			["temperature"] = 0.0,
			["stream"] = true,
			["stream_options"] = new Dictionary<string, object> { ["include_usage"] = true },
			["cache_prompt"] = false
		};
		var sw = Stopwatch.StartNew();
		double? ttftMs = null;
		int generatedTokens = 0;
		int promptTokens = 0;
		double? serverGenTps = null;
		double? serverPromptTps = null;
		string answer = string.Empty;
		var sb = new StringBuilder();

		using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
		using var response = await Http.PostAsync(url, content, ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			throw new InvalidOperationException("llama-server HTTP " + (int)response.StatusCode + ": " + body.Substring(0, Math.Min(200, body.Length)));
		}
		using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		while (!reader.EndOfStream)
		{
			string line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
			{
				continue;
			}
			string data = line.Substring(5).Trim();
			if (data == "[DONE]")
			{
				break;
			}
			try
			{
				using var doc = JsonDocument.Parse(data);
				JsonElement root = doc.RootElement;
				if (root.TryGetProperty("usage", out var usage))
				{
					if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var ptv)) promptTokens = ptv;
					if (usage.TryGetProperty("completion_tokens", out var ct2) && ct2.TryGetInt32(out var ctv)) generatedTokens = Math.Max(generatedTokens, ctv);
				}
				if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
				{
					JsonElement choice = choices[0];
					if (choice.TryGetProperty("text", out var text) && !string.IsNullOrEmpty(text.GetString()))
					{
						string piece = text.GetString()!;
						sb.Append(piece);
						generatedTokens++;
						if (ttftMs == null)
						{
							ttftMs = sw.Elapsed.TotalMilliseconds;
						}
					}
				}
				// llama-server authoritative timings (final event).
				if (root.TryGetProperty("timings", out var timings) && timings.ValueKind == JsonValueKind.Object)
				{
					if (timings.TryGetProperty("predicted_per_second", out var gps) && gps.TryGetDouble(out var g)) serverGenTps = g;
					if (timings.TryGetProperty("prompt_per_second", out var pps) && pps.TryGetDouble(out var p)) serverPromptTps = p;
				}
			}
			catch (JsonException)
			{
				// partial/malformed SSE line — skip
			}
			if ((generatedTokens & 31) == 31)
			{
				await Task.Delay(1, ct).ConfigureAwait(false);
			}
		}
		sw.Stop();

		double wallMs = sw.Elapsed.TotalMilliseconds;
		double genElapsedMs = ttftMs.HasValue ? Math.Max(1, wallMs - ttftMs.Value) : 0;
		double genTps = serverGenTps ?? (generatedTokens > 0 && genElapsedMs > 0 ? generatedTokens / (genElapsedMs / 1000.0) : 0);
		int usedPromptTokens = promptTokens > 0 ? promptTokens : (int)(prompt.Length / 3.4);
		double prefillTps = serverPromptTps ?? (ttftMs.HasValue && ttftMs.Value > 0 ? usedPromptTokens / (ttftMs.Value / 1000.0) : 0);
		answer = sb.ToString();

		return new BenchmarkPoint
		{
			Label = BenchmarkContextPointGenerator.FormatPoint(usedPromptTokens),
			Tag = "SERVER",
			ContextRequested = usedPromptTokens,
			PromptTokensActual = promptTokens > 0 ? promptTokens : usedPromptTokens,
			GeneratedTokens = generatedTokens,
			PrefillTps = prefillTps,
			GenerationTps = genTps,
			E2eTps = generatedTokens > 0 ? generatedTokens / (wallMs / 1000.0) : 0,
			TtftMs = ttftMs ?? 0,
			WallTimeS = wallMs / 1000.0,
			MsPerToken = genTps > 0 ? 1000.0 / genTps : 0,
			// The answer is logged by the caller for transparency; keep a short tail here.
			Mean = null
		};
	}

	/// <summary>
	/// MTP / speculative acceptance: parsed from the most recent server log line
	/// "draft acceptance = 0.38 (618 accepted / 1605 generated), mean len = 2.93".
	/// Honest nulls when the server doesn't provide the metric (spec §26).
	/// </summary>
	public (double? Percent, int? Accepted, int? GeneratedDraft, double? MeanLen) ReadMtpAcceptanceFromLogs(List<LogEntry> recentLogs)
	{
		for (int i = recentLogs.Count - 1; i >= 0; i--)
		{
			string text = recentLogs[i].Message ?? string.Empty;
			var m = Regex.Match(text, @"draft acceptance\s*=\s*([\d.]+)\s*\((\d+)\s+accepted\s*/\s*(\d+)\s+generated\)\s*,\s*mean len\s*=\s*([\d.]+)");
			if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double acc)
				&& int.TryParse(m.Groups[2].Value, out int accepted)
				&& int.TryParse(m.Groups[3].Value, out int generated)
				&& double.TryParse(m.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double meanLen))
			{
				return (generated > 0 ? accepted * 100.0 / generated : acc * 100.0, accepted, generated, meanLen);
			}
		}
		return (null, null, null, null);
	}

	// ==================== Engine benchmark (llama-bench) ====================

	/// <summary>Runs `llama-bench --help` and detects the real capabilities.</summary>
	public async Task<LlamaBenchCapabilities> DetectCapabilitiesAsync(string benchPath, CancellationToken ct)
	{
		try
		{
			var (output, _) = await RunCaptureAsync(benchPath, new[] { "--help" }, ct, timeoutSeconds: 30);
			return LlamaBenchCapabilities.ParseHelp(output);
		}
		catch (Exception ex)
		{
			_log.Warning("llama-bench --help failed: " + ex.Message, "Benchmark");
			return new LlamaBenchCapabilities();
		}
	}

	/// <summary>llama.cpp version string from `llama-bench --version` ("" on failure).</summary>
	public async Task<string> ReadLlamaBenchVersionAsync(string benchPath, CancellationToken ct)
	{
		try
		{
			var (output, _) = await RunCaptureAsync(benchPath, new[] { "--version" }, ct, timeoutSeconds: 15);
			var line = output.Split('\n').FirstOrDefault(l => l.Contains("version", StringComparison.OrdinalIgnoreCase) || l.Contains("build", StringComparison.OrdinalIgnoreCase));
			return line == null ? output.Split('\n')[0].Trim() : line.Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	public class EngineBenchRequest
	{
		public required string BenchPath { get; init; }
		public required string ModelPath { get; init; }
		public required LlamaBenchCapabilities Caps { get; init; }
		public int PromptTokens { get; init; } = 512;
		public int GenTokens { get; init; } = 128;
		public int? Depth { get; init; }
		public int Repetitions { get; init; } = 3;
		public bool Warmup { get; init; } = true;
		// Profile parity for the engine run:
		public int? GpuLayers { get; init; }
		public bool? FlashAttn { get; init; }
		public string? SplitMode { get; init; }
		public int? MainGpu { get; init; }
		public string? TensorSplit { get; init; }
		public int? Threads { get; init; }
		public int? BatchSize { get; init; }
		public int? UbatchSize { get; init; }
		public string? CacheTypeK { get; init; }
		public string? CacheTypeV { get; init; }
	}

	/// <summary>
	/// Runs llama-bench with args built ONLY from detected capabilities,
	/// parses JSON output (preferred) or the markdown table fallback.
	/// </summary>
	public async Task<LlamaBenchResult> RunLlamaBenchAsync(EngineBenchRequest req, Action<string>? onOutput, CancellationToken ct)
	{
		var args = new List<string>();
		void AddFlag(string flag, string value) { args.Add(flag); args.Add(value); }
		if (req.Caps.SupportsModel) AddFlag("-m", req.ModelPath);
		if (req.Caps.SupportsPrompt) AddFlag("-p", req.PromptTokens.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsGen) AddFlag("-n", req.GenTokens.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsDepth && req.Depth.HasValue) AddFlag("-d", req.Depth.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsRepetitions) AddFlag("-r", Math.Max(1, req.Repetitions).ToString(CultureInfo.InvariantCulture));
		if (!req.Warmup && req.Caps.SupportsNoWarmup) args.Add("--no-warmup");
		string output = req.Caps.SupportsJsonOutput ? "json" : "md";
		if (req.Caps.SupportsOutput) AddFlag("-o", output);
		if (req.Caps.SupportsNgl && req.GpuLayers.HasValue) AddFlag("-ngl", req.GpuLayers.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsFlashAttn && req.FlashAttn.HasValue) AddFlag("-fa", req.FlashAttn.Value ? "on" : "off");
		if (req.Caps.SupportsSplitMode && !string.IsNullOrWhiteSpace(req.SplitMode)) AddFlag("-sm", req.SplitMode!.ToLowerInvariant());
		if (req.Caps.SupportsMainGpu && req.MainGpu.HasValue) AddFlag("-mg", req.MainGpu.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsTensorSplit && !string.IsNullOrWhiteSpace(req.TensorSplit)) AddFlag("-ts", req.TensorSplit!.Replace(',', '/'));
		if (req.Caps.SupportsThreads && req.Threads.HasValue && req.Threads.Value > 0) AddFlag("-t", req.Threads.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsBatch && req.BatchSize.HasValue) AddFlag("-b", req.BatchSize.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsUbatch && req.UbatchSize.HasValue) AddFlag("-ub", req.UbatchSize.Value.ToString(CultureInfo.InvariantCulture));
		if (req.Caps.SupportsCacheTypeK && !string.IsNullOrWhiteSpace(req.CacheTypeK)) AddFlag("-ctk", req.CacheTypeK!.ToLowerInvariant());
		if (req.Caps.SupportsCacheTypeV && !string.IsNullOrWhiteSpace(req.CacheTypeV)) AddFlag("-ctv", req.CacheTypeV!.ToLowerInvariant());

		_log.Information("llama-bench: " + req.BenchPath + " " + string.Join(" ", args), "Benchmark");
		var (stdout, exitCode) = await RunCaptureAsync(req.BenchPath, args.ToArray(), ct, timeoutSeconds: 3600, onOutput: onOutput).ConfigureAwait(false);

		// r110: persist the RAW llama-bench stdout so the exact output format can
		// be inspected (the in-app log buffer is not exposed anywhere — the
		// «Логи» page shows only the server console, and WriteToFile is a no-op).
		SaveRawBenchOutput(req.PromptTokens, req.GenTokens, args.ToArray(), exitCode, stdout);

		var result = req.Caps.SupportsJsonOutput ? LlamaBenchResultParser.ParseJson(ExtractJson(stdout)) : LlamaBenchResultParser.ParseText(stdout);
		result.ProcessExitCode = exitCode;
		if (result.Results.Count == 0 && !result.ParsedAsJson)
		{
			// JSON requested but the binary printed non-JSON (version drift) — try the table.
			var fallback = LlamaBenchResultParser.ParseText(stdout);
			fallback.ProcessExitCode = exitCode;
			if (fallback.Results.Count > 0)
			{
				return fallback;
			}
		}
		return result;
	}

	/// <summary>
	/// r110: writes the full raw llama-bench stdout (plus args/exit code) to
	/// ~/.config/LlamaStudio/benchmarks/raw/ so the exact output format can be
	/// inspected after a run. Best effort — never throws.
	/// </summary>
	private void SaveRawBenchOutput(int promptTokens, int genTokens, string[] args, int exitCode, string stdout)
	{
		try
		{
			string dir = Path.Combine(BenchmarkDirectory(), "raw");
			Directory.CreateDirectory(dir);
			string name = "bench-p" + promptTokens + "-n" + genTokens + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt";
			string path = Path.Combine(dir, name);
			using var w = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
			w.WriteLine("# llama-bench raw capture (r110)");
			w.WriteLine("# captured: " + DateTime.Now.ToString("O"));
			w.WriteLine("# exitCode: " + exitCode);
			w.WriteLine("# args: " + string.Join(" ", args));
			w.WriteLine("--- stdout (" + (stdout?.Length ?? 0) + " chars) ---");
			w.WriteLine(stdout ?? string.Empty);
		}
		catch
		{
			// best effort — a logging failure must not break the benchmark
		}
	}

	/// <summary>
	/// Extracts the JSON payload from stdout. llama-bench prints CUDA-init and
	/// model-load log lines BEFORE the result, so we must slice the actual JSON
	/// value. r109: b11042+ builds print a top-level ARRAY of per-test objects
	/// ([ {"pp512": 775.8, "samples_ns":[...]}, {"tg128": 49.6, ...} ]); older
	/// builds print a single object ({"runs":[...]}). The old slice (first '{'
	/// .. last '}') mangled the array into "{...}, {...}" — invalid JSON — and
	/// the whole parse silently failed (empty PP/TG). Now: try every '['/'{'
	/// candidate start, balance the matching bracket (string-aware), and return
	/// the valid JSON container whose close is LASTEST (the real result is
	/// printed last and contains all nested containers).
	/// </summary>
	public static string ExtractJson(string stdout)
	{
		// r110: two passes. (1) prefer a balanced container that actually parses
		// as a JSON array/object (lenient: tolerates trailing commas) — the real
		// result is the one that closes LASTEST. (2) if NONE parses (e.g. the
		// build prints slightly-off JSON), still return the balanced container
		// that closes lastest so ParseJson can attempt a lenient parse instead
		// of giving up and returning the whole stdout.
		string? validBest = null;
		int validBestEnd = -1;
		string? anyBest = null;
		int anyBestEnd = -1;
		for (int i = 0; i < stdout.Length; i++)
		{
			char c = stdout[i];
			if (c != '[' && c != '{')
			{
				continue;
			}
			int end = MatchingClose(stdout, i);
			if (end < 0)
			{
				continue;
			}
			if (end < anyBestEnd)
			{
				continue;
			}
			string slice = stdout.Substring(i, end - i + 1);
			if (end > anyBestEnd)
			{
				anyBest = slice;
				anyBestEnd = end;
			}
			if (IsValidJsonContainer(slice) && end > validBestEnd)
			{
				validBest = slice;
				validBestEnd = end;
			}
		}
		return validBest ?? anyBest ?? stdout;
	}

	/// <summary>Index of the bracket matching the one at <paramref name="start"/>
	/// (string-aware), or -1 when unbalanced.</summary>
	private static int MatchingClose(string s, int start)
	{
		char open = s[start];
		char close = open == '[' ? ']' : '}';
		int depth = 0;
		bool inStr = false;
		bool esc = false;
		for (int i = start; i < s.Length; i++)
		{
			char c = s[i];
			if (inStr)
			{
				if (esc)
				{
					esc = false;
				}
				else if (c == '\\')
				{
					esc = true;
				}
				else if (c == '"')
				{
					inStr = false;
				}
				continue;
			}
			if (c == '"')
			{
				inStr = true;
				continue;
			}
			if (c == open)
			{
				depth++;
			}
			else if (c == close)
			{
				depth--;
				if (depth == 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	/// <summary>True when <paramref name="slice"/> parses as a JSON array or object
	/// (lenient: tolerates trailing commas, which some pretty-printers emit).</summary>
	private static bool IsValidJsonContainer(string slice)
	{
		if (IsJsonContainerStrict(slice))
		{
			return true;
		}
		try
		{
			string cleaned = System.Text.RegularExpressions.Regex.Replace(slice, ",(\\s*[}\\]])", "$1");
			return IsJsonContainerStrict(cleaned);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsJsonContainerStrict(string json)
	{
		try
		{
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			return doc.RootElement.ValueKind is System.Text.Json.JsonValueKind.Array
				or System.Text.Json.JsonValueKind.Object;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Cross-platform process execution with concurrent stdout/stderr draining
	/// (no pipe deadlock), cancellation with process-tree kill.
	/// Returns combined captured output (stdout).
	/// </summary>
	/// <summary>
	/// r105: returns (combined stdout, process exit code) so callers can tell a
	/// dead/killed process apart from a successful empty run.
	/// </summary>
	public async Task<(string Output, int ExitCode)> RunCaptureAsync(string fileName, string[] args, CancellationToken ct, int timeoutSeconds, Action<string>? onOutput = null)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			CreateNoWindow = true
		};
		foreach (var a in args)
		{
			psi.ArgumentList.Add(a);
		}
		using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();
		var outputLock = new object();
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data == null)
			{
				return;
			}
			lock (outputLock)
			{
				stdout.AppendLine(e.Data);
			}
			onOutput?.Invoke(e.Data);
			_log.Debug(e.Data, "Benchmark");
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data == null)
			{
				return;
			}
			lock (outputLock)
			{
				stderr.AppendLine(e.Data);
			}
			_log.Debug("[stderr] " + e.Data, "Benchmark");
		};
		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
		if (!process.Start())
		{
			throw new InvalidOperationException("Не удалось запустить: " + fileName);
		}
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		try
		{
			await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			KillTree(process);
			throw new OperationCanceledException("Benchmark process cancelled", ct);
		}
		catch (OperationCanceledException)
		{
			KillTree(process);
			throw new TimeoutException("Таймаут (" + timeoutSeconds + " с): " + Path.GetFileName(fileName));
		}
		// Give the readers a moment to flush the final lines.
		await Task.Delay(50).ConfigureAwait(false);
		string combined;
		lock (outputLock)
		{
			combined = stdout.ToString();
		}
		int exitCode = process.ExitCode;
		if (exitCode != 0)
		{
			_log.Warning(Path.GetFileName(fileName) + " exited with code " + exitCode + ": " + stderr.ToString().Substring(0, Math.Min(500, stderr.Length)), "Benchmark");
		}
		return (combined, exitCode);
	}

	/// <summary>Kills the process tree (works on both OSes via Process.Kill(true)).</summary>
	public static void KillTree(Process process)
	{
		try
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}
		catch
		{
			try { process.Kill(); } catch { }
		}
	}

	// ==================== run persistence ====================

	public static string BenchmarkDirectory()
	{
		string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio", "benchmarks");
		Directory.CreateDirectory(dir);
		return dir;
	}

	public static string RunFileName(BenchmarkRun run)
	{
		string safe = string.Concat((run.DisplayName + "-" + run.Id[..Math.Min(6, run.Id.Length)]).Where(c => char.IsLetterOrDigit(c) || c == '-' || c == ' '));
		if (safe.Length == 0)
		{
			safe = "run-" + run.Id[..8];
		}
		return safe.Trim() + ".json";
	}

	public void SaveRun(BenchmarkRun run)
	{
		string path = Path.Combine(BenchmarkDirectory(), RunFileName(run));
		File.WriteAllText(path, JsonSerializer.Serialize(run, Json));
		_log.Information("Benchmark run saved: " + path, "Benchmark");
	}

	public List<BenchmarkRun> LoadRuns()
	{
		var runs = new List<BenchmarkRun>();
		try
		{
			foreach (string file in Directory.EnumerateFiles(BenchmarkDirectory(), "*.json"))
			{
				try
				{
					var run = JsonSerializer.Deserialize<BenchmarkRun>(File.ReadAllText(file), Json);
					if (run != null)
					{
						runs.Add(run);
					}
				}
				catch (Exception ex)
				{
					_log.Warning("Skipping bad benchmark file " + Path.GetFileName(file) + ": " + ex.Message, "Benchmark");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Warning("Benchmark directory read failed: " + ex.Message, "Benchmark");
		}
		runs = runs.OrderByDescending(r => r.CreatedAt).ToList();
		return runs;
	}

	public void DeleteRun(BenchmarkRun run)
	{
		try
		{
			string path = Path.Combine(BenchmarkDirectory(), RunFileName(run));
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex)
		{
			_log.Warning("Benchmark delete failed: " + ex.Message, "Benchmark");
		}
	}

	/// <summary>Rename = save under the new name, remove the old file.</summary>
	public void RenameRun(BenchmarkRun run, string newDisplayName)
	{
		run.DisplayName = newDisplayName;
		try
		{
			string dir = BenchmarkDirectory();
			string oldPath = Path.Combine(dir, RunFileName(run));
			// Re-serialize with the new name (id is stable, name changed).
			string json = JsonSerializer.Serialize(run, Json);
			string newPath = Path.Combine(dir, RunFileName(run));
			if (File.Exists(oldPath) && !string.Equals(oldPath, newPath, StringComparison.Ordinal))
			{
				File.Delete(oldPath);
			}
			File.WriteAllText(newPath, json);
		}
		catch (Exception ex)
		{
			_log.Warning("Benchmark rename failed: " + ex.Message, "Benchmark");
		}
	}

	/// <summary>Import: reads a JSON run file, marks it Source="imported", saves.</summary>
	public BenchmarkRun ImportRun(string path)
	{
		var run = JsonSerializer.Deserialize<BenchmarkRun>(File.ReadAllText(path), Json);
		if (run == null)
		{
			throw new InvalidOperationException("Файл не похож на benchmark-результат");
		}
		run.Source = "imported";
		if (string.IsNullOrWhiteSpace(run.Id))
		{
			run.Id = Guid.NewGuid().ToString("N");
		}
		SaveRun(run);
		return run;
	}

	public void ExportRun(BenchmarkRun run, string targetPath)
	{
		File.WriteAllText(targetPath, JsonSerializer.Serialize(run, Json));
		_log.Information("Benchmark run exported: " + targetPath, "Benchmark");
	}
}

/// <summary>
/// r101: background GPU sampler — captures avg/peak power, utilization, VRAM,
/// temperature and fan per GPU while a benchmark runs.
/// </summary>
public sealed class GpuSampler : IAsyncDisposable
{
	private readonly IGpuMonitor _monitor;
	private readonly TimeSpan _interval;
	private readonly Task _loop;
	private readonly List<GpuSample> _samples = new();
	private readonly object _lock = new();
	private bool _disposed;

	private class GpuSample
	{
		public int Index;
		public string Name;
		public double VramTotalGb;
		public double VramUsedGb;
		public double PowerW;
		public double UtilPercent;
		public double TempC;
		public int Fan;
	}

	public GpuSampler(IGpuMonitor monitor, TimeSpan interval)
	{
		_monitor = monitor;
		_interval = interval;
		_loop = Task.Run(async () =>
		{
			while (!_disposed)
			{
				try
				{
					var gpus = await _monitor.GetGpuInfosAsync().ConfigureAwait(false);
					lock (_lock)
					{
						foreach (var g in gpus)
						{
							_samples.Add(new GpuSample
							{
								Index = g.Index,
								Name = g.Name,
								VramTotalGb = g.MemoryTotalGb,
								VramUsedGb = g.MemoryUsedGb,
								PowerW = g.PowerDrawWatts,
								UtilPercent = g.GpuUtilization,
								TempC = g.TemperatureCelsius,
								Fan = g.FanSpeed
							});
						}
					}
				}
				catch
				{
					// sampling is best-effort
				}
				await Task.Delay(_interval).ConfigureAwait(false);
			}
		});
	}

	/// <summary>Aggregates captured samples into per-GPU snapshots.</summary>
	public List<BenchmarkGpuSnapshot> Snapshot()
	{
		var byGpu = new Dictionary<int, List<GpuSample>>();
		lock (_lock)
		{
			foreach (var s in _samples)
			{
				if (!byGpu.TryGetValue(s.Index, out var list))
				{
					byGpu[s.Index] = list = new List<GpuSample>();
				}
				list.Add(s);
			}
		}
		var result = new List<BenchmarkGpuSnapshot>();
		foreach (var kvp in byGpu.OrderBy(x => x.Key))
		{
			var list = kvp.Value;
			result.Add(new BenchmarkGpuSnapshot
			{
				Index = kvp.Key,
				Name = list[0].Name,
				VramTotalGb = list[0].VramTotalGb,
				VramUsedPeakGb = list.Max(s => s.VramUsedGb),
				PowerAvgW = list.Average(s => s.PowerW),
				PowerPeakW = list.Max(s => s.PowerW),
				UtilAvgPercent = list.Average(s => s.UtilPercent),
				UtilPeakPercent = list.Max(s => s.UtilPercent),
				TempAvgC = list.Average(s => s.TempC),
				TempMaxC = list.Max(s => s.TempC),
				FanMax = list.Max(s => s.Fan)
			});
		}
		return result;
	}

	public int SampleCount
	{
		get { lock (_lock) { return _samples.Count; } }
	}

	/// <summary>Clears captured samples (used between sweep points so each
	/// point gets its own GPU window).</summary>
	public void Reset()
	{
		lock (_lock)
		{
			_samples.Clear();
		}
	}

	public async ValueTask DisposeAsync()
	{
		_disposed = true;
		try { await _loop.ConfigureAwait(false); } catch { }
	}
}
