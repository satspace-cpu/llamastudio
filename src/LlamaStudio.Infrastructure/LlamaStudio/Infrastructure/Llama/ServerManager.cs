using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class ServerManager : IServerManager, IDisposable
{
	private readonly ILogService _log;

	private readonly ISettings _settings;

	private Process? _process;

	private CancellationTokenSource? _cts;

	private CancellationTokenSource? _healthCts;

	private readonly object _lock = new object();

	private DateTime? _startedAt;

	private ServerStatus _status = new ServerStatus
	{
		State = ServerState.Stopped
	};

	private string? _lastHost;

	private int _lastPort;

	private double _currentTps;

	private DateTime _lastTpsUpdate = DateTime.UtcNow;

	private int _tickCounter;

	private static readonly Regex s_tpsRegexTg = new Regex("tg\\s*=\\s*([\\d.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex s_tpsRegexEval = new Regex("([\\d.]+)\\s+tokens?\\s+per\\s+second", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly HttpClient s_sharedHttpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10.0)
	};

	public event EventHandler<ServerStatus>? StatusChanged;

	public event EventHandler<LogEntry>? LogReceived;

	public ServerManager(ILogService log, ISettings settings)
	{
		_log = log;
		_settings = settings;
	}

	private static string NormalizeHostForClient(string host)
	{
		if (!(host == "0.0.0.0") && !(host == "::"))
		{
			return host;
		}
		return "127.0.0.1";
	}

	async Task<ServerStatus> IServerManager.GetStatusAsync()
	{
		bool processRunning;
		ServerStatus status;
		lock (_lock)
		{
			processRunning = _process != null && !_process.HasExited;
			status = new ServerStatus
			{
				State = _status.State,
				Port = _status.Port,
				Host = _status.Host,
				ModelName = _status.ModelName,
				ContextSize = _status.ContextSize,
				Threads = _status.Threads,
				GpuLayers = _status.GpuLayers,
				VramUsedGb = _status.VramUsedGb,
				RamUsedGb = _status.RamUsedGb,
				RamCommittedGb = _status.RamCommittedGb,
				CacheRamLimitGb = _status.CacheRamLimitGb,
				TokensPerSecond = _status.TokensPerSecond,
				QueueSize = _status.QueueSize,
				ActiveSlots = _status.ActiveSlots,
				TotalTokensProcessed = _status.TotalTokensProcessed,
				Uptime = (_startedAt.HasValue ? (DateTime.Now - _startedAt.Value) : TimeSpan.Zero),
				StartedAt = _startedAt,
				ErrorMessage = _status.ErrorMessage,
				ProcessId = _process?.Id
			};
		}
		if (!processRunning && status.State == ServerState.Stopped)
		{
			try
			{
				List<Process> externalProcs = (from p in Process.GetProcessesByName("llama-server")
					where !p.HasExited
					select p).ToList();
				if (externalProcs.Count > 0)
				{
					string host = NormalizeHostForClient(_settings.DefaultHost ?? "127.0.0.1");
					int port = ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080);
					ServerStatus serverStatus = await HealthCheckInternalAsync(host, port);
					if (serverStatus.State == ServerState.Running)
					{
						lock (_lock)
						{
							_process = externalProcs[0];
						}
						lock (_lock)
						{
							_status.State = ServerState.Running;
							_status.Port = serverStatus.Port;
							_status.Host = serverStatus.Host;
							_status.ModelName = serverStatus.ModelName;
							_status.ContextSize = serverStatus.ContextSize;
							_status.Threads = serverStatus.Threads;
							_status.GpuLayers = serverStatus.GpuLayers;
						}
						RaiseStatusChanged();
						_healthCts?.Cancel();
						_healthCts?.Dispose();
						StartHealthPolling(host, port);
						return new ServerStatus
						{
							State = ServerState.Running,
							Port = serverStatus.Port,
							Host = serverStatus.Host,
							ModelName = serverStatus.ModelName,
							ContextSize = serverStatus.ContextSize,
							Threads = serverStatus.Threads,
							GpuLayers = serverStatus.GpuLayers,
							VramUsedGb = 0.0,
							RamUsedGb = 0.0,
							RamCommittedGb = 0.0,
							TokensPerSecond = 0.0,
							QueueSize = 0,
							ActiveSlots = serverStatus.ActiveSlots,
							TotalTokensProcessed = 0,
							Uptime = TimeSpan.Zero,
							StartedAt = null,
							ErrorMessage = null,
							ProcessId = externalProcs[0].Id
						};
					}
				}
			}
			catch
			{
			}
		}
		await Task.CompletedTask;
		return status;
	}

	async Task IServerManager.StartAsync(ServerProfile profile, CancellationToken cancellationToken)
	{
		if (_process != null && !_process.HasExited)
		{
			_log.Warning("Server already running", "ServerManager");
			return;
		}
		lock (_lock)
		{
			_status = new ServerStatus
			{
				State = ServerState.Starting,
				Port = profile.Port,
				Host = profile.Host,
				ModelName = Path.GetFileName(profile.ModelPath ?? string.Empty),
				CacheRamLimitGb = profile.CacheRam > 0 ? profile.CacheRam / 1024.0 : 0.0
			};
		}
		RaiseStatusChanged();
		_log.Information("Starting server with profile: " + profile.Name, "ServerManager");
		try
		{
			string dir = _settings.LlamaCppDirectory ?? string.Empty;
			// r71: STRICT active-version resolution. The version selected in the UI
			// must be the version that actually launches: resolve the active
			// version's folder from baseDir and prefer it over the configured
			// LlamaCppDirectory (which may be stale after uninstall/switch). If the
			// selected folder cannot be found, log it loudly and fall back to the
			// configured directory (resolution then fails with a clear error).
			string strictBaseDir = _settings.LlamaCppBaseDirectory ?? string.Empty;
			string strictActiveVer = _settings.ActiveLlamaCppVersion ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(strictBaseDir) && strictActiveVer.Contains('|') && Directory.Exists(strictBaseDir))
			{
				string strictVersionPart = strictActiveVer.Split('|')[0];
				string strictBuildPart = strictActiveVer.Split('|', 2)[1].ToLowerInvariant();
				string? strictExpectedDir = null;
				foreach (string sub in Directory.GetDirectories(strictBaseDir))
				{
					string name = Path.GetFileName(sub);
					if (name.StartsWith(strictVersionPart + "-" + strictBuildPart, StringComparison.OrdinalIgnoreCase)
						&& File.Exists(Path.Combine(sub, "llama-server")))
					{
						strictExpectedDir = sub;
						break;
					}
				}
				if (strictExpectedDir != null)
				{
					if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir.TrimEnd('/', '\\'))
						|| !string.Equals(Path.GetFullPath(strictExpectedDir), Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
					{
						_log.Information($"Active version {strictActiveVer} -> folder {strictExpectedDir} (configured LlamaCppDirectory: '{_settings.LlamaCppDirectory}'); launching the selected version.", "ServerManager");
						_log.ServerOutput($"ACTIVE Llama VERSION: {strictActiveVer} -> {strictExpectedDir}");
						dir = strictExpectedDir;
					}
				}
				else
				{
					_log.Warning($"Active version folder not found under {strictBaseDir} (expected prefix '{strictVersionPart}-{strictBuildPart}'); using configured directory '{dir}' as fallback", "ServerManager");
				_log.ServerOutput($"WARNING: active version folder not found under {strictBaseDir} (expected '{strictVersionPart}-{strictBuildPart}'); falling back to '{dir}'");
				}
			}
			// The profile pins a different version than the global active one —
			// the launch uses the active version; make that visible in the log.
			string? profileVer = profile.SelectedServerVersion;
			if (!string.IsNullOrWhiteSpace(profileVer) && !string.Equals(profileVer, strictActiveVer, StringComparison.OrdinalIgnoreCase))
			{
				_log.Warning($"Profile '{profile.Name}' pins version {profileVer}, but the active Studio version is {strictActiveVer}; launching with the active version.", "ServerManager");
				_log.ServerOutput($"WARNING: profile '{profile.Name}' pins version {profileVer}, but the active Studio version is {strictActiveVer}; launching with the active version");
			}
			if (!string.Equals(dir, _settings.LlamaCppDirectory, StringComparison.OrdinalIgnoreCase))
			{
				_settings.LlamaCppDirectory = dir;
				_ = _settings.SaveAsync();
			}
			string executablePath = ResolveExecutablePath(dir, _settings.ActiveLlamaCppVersion);
			if (executablePath == null)
			{
				string execNotFoundMessage = "llama-server executable not found in: " + _settings.LlamaCppDirectory;
				_log.Error(execNotFoundMessage, "ServerManager");
				lock (_lock)
				{
					_status.State = ServerState.Error;
					_status.ErrorMessage = execNotFoundMessage;
				}
				RaiseStatusChanged();
				return;
			}
			if (string.IsNullOrWhiteSpace(profile.ModelPath) && string.IsNullOrWhiteSpace(profile.HfRepo) && string.IsNullOrWhiteSpace(profile.HfFile))
			{
				string modelNotSpecifiedMessage = "Model path or Hugging Face repo/file is not specified in profile";
				_log.Error(modelNotSpecifiedMessage, "ServerManager");
				lock (_lock)
				{
					_status.State = ServerState.Error;
					_status.ErrorMessage = modelNotSpecifiedMessage;
				}
				RaiseStatusChanged();
				return;
			}
			if (!string.IsNullOrWhiteSpace(profile.ModelPath) && !File.Exists(profile.ModelPath))
			{
				string modelFileNotFoundMessage = "Model file not found: " + profile.ModelPath;
				_log.Error(modelFileNotFoundMessage, "ServerManager");
				lock (_lock)
				{
					_status.State = ServerState.Error;
					_status.ErrorMessage = modelFileNotFoundMessage;
				}
				RaiseStatusChanged();
				return;
			}
			if (IsPortInUse(profile.Port))
			{
				// Port is in use — check whether a healthy llama-server is already
				// listening there. If so, attach instead of raising an error.
				string healthHost = NormalizeHostForClient(profile.Host ?? "127.0.0.1");
				ServerStatus? healthy = null;
				try { healthy = await HealthCheckInternalAsync(healthHost, profile.Port); } catch { }
				if (healthy != null && healthy.State == ServerState.Running)
				{
					_log.Information($"Port {profile.Port} is already in use by a healthy llama-server — attaching", "ServerManager");
					// Find the external process
					Process? extProc = null;
					try
					{
						extProc = Process.GetProcessesByName("llama-server")
							.FirstOrDefault(p => { try { return !p.HasExited; } catch { return false; } });
					}
					catch { }
					lock (_lock)
					{
						_process = extProc;
						_lastHost = healthHost;
						_lastPort = profile.Port;
						_status.State = ServerState.Running;
						_status.Host = profile.Host ?? healthHost;
						_status.Port = profile.Port;
						_status.ModelName = healthy.ModelName ?? _status.ModelName;
						_status.ContextSize = healthy.ContextSize;
						_status.Threads = healthy.Threads;
						_status.GpuLayers = healthy.GpuLayers;
						_status.ProcessId = extProc?.Id ?? _status.ProcessId;
					}
					_startedAt = DateTime.Now;
					RaiseStatusChanged();
					_healthCts?.Cancel();
					_healthCts?.Dispose();
					StartHealthPolling(healthHost, profile.Port);
					return;
				}
				string portInUseMessage = $"Port {profile.Port} is already in use by another process (not a healthy llama-server). Choose a different port or stop the process using it.";
				_log.Error(portInUseMessage, "ServerManager");
				lock (_lock)
				{
					_status.State = ServerState.Error;
					_status.ErrorMessage = portInUseMessage;
				}
				RaiseStatusChanged();
				return;
			}
			// r71: first lines of the launch log — exactly which binary starts and
			// what it reports as its own version (commit/build), so the user can
			// see the real binary behind the selected version.
			// r72: the visible «Логи» page shows only the server-output console, so
			// mirror these lines into it as well (the structured buffer alone was
			// invisible in the UI).
			_log.Information("LLAMA SERVER BINARY: " + executablePath, "ServerManager");
			_log.ServerOutput("LLAMA SERVER BINARY: " + executablePath);
			try
			{
				ProcessStartInfo versionProbe = new ProcessStartInfo(executablePath, "--version")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using Process versionProcess = Process.Start(versionProbe) ?? throw new InvalidOperationException("version probe did not start");
				string versionOutput = await versionProcess.StandardOutput.ReadToEndAsync();
				try
				{
					await versionProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
				}
				catch
				{
					try { versionProcess.Kill(); } catch { }
				}
				_log.Information("LLAMA SERVER VERSION: " + versionOutput.Trim().Replace("\r", string.Empty).Replace("\n", " | "), "ServerManager");
				_log.ServerOutput("LLAMA SERVER VERSION: " + versionOutput.Trim().Replace("\r", string.Empty).Replace("\n", " | "));
			}
			catch (Exception versionEx)
			{
				_log.Warning("Could not run 'llama-server --version' before start: " + versionEx.Message, "ServerManager");
			}
			string serverArgs = BuildServerArgs(profile);
			_log.Information("Command: " + executablePath + " " + serverArgs, "ServerManager");
			_cts = new CancellationTokenSource();
			string executableDir = Path.GetDirectoryName(executablePath) ?? _settings.LlamaCppDirectory;
			string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			string newPath = executableDir + Path.PathSeparator + currentPath;
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				FileName = executablePath,
				Arguments = serverArgs,
				WorkingDirectory = executableDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};
			foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
			{
				processStartInfo.Environment[(string)environmentVariable.Key] = (string)environmentVariable.Value;
			}
			processStartInfo.Environment["LLAMA_NO_COLOR"] = "1";
			processStartInfo.Environment["PATH"] = newPath;
			_process = new Process
			{
				StartInfo = processStartInfo
			};
			_process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
			{
				if (!string.IsNullOrEmpty(eventArgs.Data))
				{
					_log.ServerOutput(eventArgs.Data);
					ParseTpsFromOutput(eventArgs.Data, this);
				}
			};
			_process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
			{
				if (!string.IsNullOrEmpty(eventArgs.Data))
				{
					_log.ServerOutput(eventArgs.Data);
					ParseTpsFromOutput(eventArgs.Data, this);
				}
			};
			_process.Exited += delegate
			{
				_log.Information("Server process exited", "ServerManager");
				lock (_lock)
				{
					if (_status.State != ServerState.Stopped)
					{
						_status.State = ServerState.Stopped;
						_startedAt = null;
					}
				}
				RaiseStatusChanged();
			};
			_process.Start();
			_process.BeginOutputReadLine();
			_process.BeginErrorReadLine();
			if (!string.IsNullOrEmpty(profile.ProcessPriority) && profile.ProcessPriority != "Normal")
			{
				try
				{
					_process.PriorityClass = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), profile.ProcessPriority);
				}
				catch
				{
					_log.Warning("Invalid process priority: " + profile.ProcessPriority, "ServerManager");
				}
			}
			lock (_lock)
			{
				_status.State = ServerState.Running;
				_status.ProcessId = _process.Id;
				_startedAt = DateTime.Now;
			}
			RaiseStatusChanged();
			_log.Information($"Server started (PID: {_process.Id}, WorkingSet: {(double)_process.WorkingSet64 / 1048576.0:F1} MB)", "ServerManager");
			_lastHost = profile.Host;
			_lastPort = profile.Port;
			lock (_lock)
			{
				_currentTps = 0.0;
				_lastTpsUpdate = DateTime.UtcNow;
			}
			_healthCts?.Cancel();
			_healthCts?.Dispose();
			StartHealthPolling(_lastHost, _lastPort);
			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to start server", "ServerManager");
			lock (_lock)
			{
				_status.State = ServerState.Error;
				_status.ErrorMessage = ex.Message;
			}
			RaiseStatusChanged();
		}
	}

	async Task IServerManager.StopAsync(CancellationToken cancellationToken)
	{
		if (_process != null && !_process.HasExited)
		{
			lock (_lock)
			{
				_status.State = ServerState.Stopping;
			}
			RaiseStatusChanged();
			_log.Information("Stopping server...", "ServerManager");
			_healthCts?.Cancel();
			_healthCts?.Dispose();
			_healthCts = null;
			_cts?.Cancel();
			if (!_process.HasExited)
			{
				// Never block the Avalonia UI thread while llama-server exits.
				Task exitTask = _process.WaitForExitAsync();
				if (await Task.WhenAny(exitTask, Task.Delay(5000)) != exitTask)
				{
					_log.Warning("Server did not stop gracefully, forcing kill", "ServerManager");
					try
					{
						_process.Kill(entireProcessTree: true);
						await Task.WhenAny(exitTask, Task.Delay(5000));
					}
					catch
					{
					}
				}
				else
				{
					_log.Information("Server stopped gracefully", "ServerManager");
				}
			}
			lock (_lock)
			{
				_status.State = ServerState.Stopped;
				_startedAt = null;
				_status.RamUsedGb = 0.0;
				_status.RamCommittedGb = 0.0;
				_status.VramUsedGb = 0.0;
				_status.ActiveSlots = 0;
			}
			RaiseStatusChanged();
			return;
		}
		try
		{
			List<Process> externalProcs = (from p in Process.GetProcessesByName("llama-server")
				where !p.HasExited
				select p).ToList();
			if (externalProcs.Count > 0)
			{
				lock (_lock)
				{
					_status.State = ServerState.Stopping;
				}
				RaiseStatusChanged();
				_log.Information($"Stopping {externalProcs.Count} external llama-server process(es)", "ServerManager");
				foreach (Process extProc in externalProcs)
				{
					try
					{
						extProc.Kill(entireProcessTree: true);
						await Task.WhenAny(extProc.WaitForExitAsync(), Task.Delay(5000));
					}
					catch (Exception exception)
					{
						_log.Error(exception, $"Failed to kill external server PID={extProc.Id}", "ServerManager");
					}
				}
				lock (_lock)
				{
					_status.State = ServerState.Stopped;
					_startedAt = null;
				}
				RaiseStatusChanged();
			}
		}
		catch (Exception exception2)
		{
			_log.Error(exception2, "Failed to stop external server", "ServerManager");
		}
		lock (_lock)
		{
			_status.State = ServerState.Stopped;
		}
		RaiseStatusChanged();
	}

	void IServerManager.AttachExternalServer(string host, int port)
	{
		Process process = null;
		try
		{
			List<Process> externalProcs = Process.GetProcessesByName("llama-server").Where(delegate(Process p)
			{
				try
				{
					return !p.HasExited;
				}
				catch
				{
					return false;
				}
			}).ToList();
			if (externalProcs.Count > 0)
			{
				process = externalProcs[0];
			}
		}
		catch
		{
		}
		lock (_lock)
		{
			_process = process;
			_lastHost = host;
			_lastPort = port;
			_status.State = ServerState.Running;
			_status.Host = host;
			_status.Port = port;
			_status.ProcessId = process?.Id;
		}
		RaiseStatusChanged();
		_healthCts?.Cancel();
		_healthCts?.Dispose();
		StartHealthPolling(host, port);
		_log.Information($"Attached to external server at {host}:{port} (PID: {process?.Id ?? (-1)})", "ServerManager");
	}

	private void StartHealthPolling(string host, int port)
	{
		host = NormalizeHostForClient(host);
		_healthCts = new CancellationTokenSource();
		Task.Run(async delegate
		{
			try
			{
				while (!_healthCts.Token.IsCancellationRequested)
				{
					CancellationTokenSource? cts = _cts;
					if (cts != null && cts.Token.IsCancellationRequested)
					{
						break;
					}
					await Task.Delay(1500, _healthCts.Token);
					try
					{
						ServerStatus health = await HealthCheckInternalAsync(host, port);
						if (health.State == ServerState.Running)
						{
							var (activeSlots, genTps, apiPromptTps) = await FetchActiveSlotsAsync(host, port);
							double currentTps;
							lock (_lock)
							{
								currentTps = _currentTps;
							}
							double finalGenTps = ((genTps > 0.0) ? genTps : currentTps);
							var (ramGb, ramCommittedGb) = GetProcessMemoryGb();
							double vramUsedGb = await FetchGpuMetricsAsync(host, port);
							lock (_lock)
							{
								_status.TokensPerSecond = ((finalGenTps > 0.0) ? finalGenTps : _status.TokensPerSecond);
								_status.PromptTokensPerSecond = ((apiPromptTps > 0.0) ? apiPromptTps : _status.PromptTokensPerSecond);
								_status.ActiveSlots = activeSlots;
								_status.ModelName = health.ModelName;
								_status.ContextSize = health.ContextSize;
								_status.RamUsedGb = ramGb;
								_status.RamCommittedGb = ramCommittedGb;
								_status.VramUsedGb = vramUsedGb;
							}
							_tickCounter++;
							RaiseStatusChanged();
						}
					}
					catch (Exception ex)
					{
						_log.Debug("Health poll error: " + ex.Message, "ServerManager");
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
		}, _healthCts.Token);
	}

	private (double ResidentGb, double CommittedGb) GetProcessMemoryGb()
	{
		Process process = null;
		lock (_lock)
		{
			process = _process;
		}
		if (process == null || process.HasExited)
		{
			try
			{
				List<Process> externalProcs = Process.GetProcessesByName("llama-server").Where(delegate(Process p)
				{
					try
					{
						return !p.HasExited;
					}
					catch
					{
						return false;
					}
				}).ToList();
				if (externalProcs.Count > 0)
				{
					int? knownPid = null;
					lock (_lock)
					{
						knownPid = _status.ProcessId;
					}
					process = ((!knownPid.HasValue) ? externalProcs[0] : (externalProcs.FirstOrDefault((Process p) => p.Id == knownPid.Value) ?? externalProcs[0]));
				}
			}
			catch
			{
			}
		}
		if (process != null && !process.HasExited)
		{
			try
			{
				process.Refresh();
				long privateBytes = 0L;
				long workingSet = 0L;
				try
				{
					privateBytes = process.PrivateMemorySize64;
				}
				catch
				{
				}
				try
				{
					workingSet = process.WorkingSet64;
				}
				catch
				{
				}
				if (workingSet > 0)
				{
					return ((double)workingSet / 1073741824.0, (double)privateBytes / 1073741824.0);
				}
				// llama.cpp loads models with memory mapping.  Mapped, resident model pages are
				// counted by Windows in the process working set, but not in private bytes.
				// Prefer WorkingSet64 so the Studio RAM indicator represents memory actually
				// resident for llama-server; private bytes are only a fallback.
				if (privateBytes > 0)
				{
					return (0.0, (double)privateBytes / 1073741824.0);
				}
			}
			catch
			{
			}
		}
		return (0.0, 0.0);
	}

	async Task<ServerStatus> IServerManager.HealthCheckAsync(string host, int port)
	{
		return await HealthCheckInternalAsync(host, port);
	}

	private async Task<ServerStatus> HealthCheckInternalAsync(string host, int port)
	{
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5.0));
			HttpResponseMessage httpResponseMessage = await s_sharedHttpClient.GetAsync($"http://{host}:{port}/health", cts.Token);
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				using (JsonDocument doc = JsonDocument.Parse(await httpResponseMessage.Content.ReadAsStringAsync()))
				{
					JsonElement rootElement = doc.RootElement;
					JsonElement modelNameValue;
					JsonElement ctxSizeValue;
					JsonElement nThreadsValue;
					JsonElement gpuLayersValue;
					JsonElement tpsValue;
					JsonElement loadValue;
					JsonElement currentValue;
					ServerStatus status = new ServerStatus
					{
						State = ServerState.Running,
						Port = port,
						Host = host,
						ModelName = (rootElement.TryGetProperty("model_name", out modelNameValue) ? modelNameValue.GetString() : null),
						ContextSize = (rootElement.TryGetProperty("ctx_size", out ctxSizeValue) ? ctxSizeValue.GetInt32() : 0),
						Threads = (rootElement.TryGetProperty("n_threads", out nThreadsValue) ? nThreadsValue.GetInt32() : 0),
						GpuLayers = (rootElement.TryGetProperty("n_gpu_layers", out gpuLayersValue) ? gpuLayersValue.GetInt32() : 0),
						TokensPerSecond = (rootElement.TryGetProperty("tps", out tpsValue) ? tpsValue.GetDouble() : 0.0),
						ActiveSlots = (rootElement.TryGetProperty("load", out loadValue) ? (loadValue.TryGetProperty("current", out currentValue) ? currentValue.GetInt32() : 0) : 0)
					};
					if (status.ModelName == null)
					{
						try
						{
							HttpResponseMessage httpResponseMessage2 = await s_sharedHttpClient.GetAsync($"http://{host}:{port}/v1/models", cts.Token);
							if (httpResponseMessage2.IsSuccessStatusCode)
							{
								using JsonDocument jsonDocument = JsonDocument.Parse(await httpResponseMessage2.Content.ReadAsStringAsync());
								if (jsonDocument.RootElement.TryGetProperty("data", out var dataValue) && dataValue.GetArrayLength() > 0)
								{
									JsonElement jsonElement = dataValue[0];
									status.ModelName = (jsonElement.TryGetProperty("id", out var idValue) ? idValue.GetString() : null);
									if (jsonElement.TryGetProperty("meta", out var metaValue))
									{
										status.ContextSize = (metaValue.TryGetProperty("n_ctx", out var nCtxValue) ? nCtxValue.GetInt32() : 0);
									}
								}
							}
						}
						catch
						{
						}
					}
					return status;
				}
			}
		}
		catch (Exception ex)
		{
			_log.Debug("Health check failed: " + ex.Message, "ServerManager");
		}
		return new ServerStatus
		{
			State = ServerState.Stopped
		};
	}

	private async Task<(int ActiveSlots, double GenTps, double PromptTps)> FetchActiveSlotsAsync(string host, int port)
	{
		int activeSlots = 0;
		double genTps = 0.0;
		double promptTps = 0.0;
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3.0));
			HttpResponseMessage httpResponseMessage = await s_sharedHttpClient.GetAsync($"http://{host}:{port}/slots", cts.Token);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return (ActiveSlots: activeSlots, GenTps: genTps, PromptTps: promptTps);
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await httpResponseMessage.Content.ReadAsStringAsync());
			JsonElement rootElement = jsonDocument.RootElement;
			JsonElement? jsonElement = null;
			if (rootElement.TryGetProperty("value", out var slotsValue) && slotsValue.ValueKind == JsonValueKind.Array)
			{
				jsonElement = slotsValue;
			}
			else if (rootElement.ValueKind == JsonValueKind.Array)
			{
				jsonElement = rootElement;
			}
			if (jsonElement.HasValue)
			{
				foreach (JsonElement slot in jsonElement.Value.EnumerateArray())
				{
					bool isProcessing = false;
					if (slot.TryGetProperty("is_processing", out var isProcessingValue))
					{
						isProcessing = isProcessingValue.GetBoolean();
					}
					if (!isProcessing && slot.TryGetProperty("state", out var stateValue) && stateValue.GetString() == "processing")
					{
						isProcessing = true;
					}
					if (!isProcessing)
					{
						continue;
					}
					activeSlots++;
					int sampleTokens = 0;
					if (slot.TryGetProperty("n_eval", out var evalCount) && evalCount.TryGetInt32(out var parsedEvalCount))
					{
						sampleTokens = parsedEvalCount;
					}
					if (sampleTokens >= 8 && slot.TryGetProperty("t_token_ms", out var tokenTimeValue) && tokenTimeValue.GetDouble() > 0.0)
					{
						double tokenTps = 1000.0 / tokenTimeValue.GetDouble();
						if (tokenTps > genTps && tokenTps <= 1000.0)
						{
							genTps = tokenTps;
						}
					}
					if (genTps <= 0.0 && sampleTokens >= 8 && slot.TryGetProperty("generation_t", out var generationTimeValue))
					{
						double generationTime = generationTimeValue.GetDouble();
						if (generationTime > 0.01)
						{
							genTps = (double)sampleTokens / generationTime;
						}
					}
				}
			}
		}
		catch
		{
		}
		return (ActiveSlots: activeSlots, GenTps: genTps, PromptTps: 0.0);
	}

	private async Task<double> FetchGpuMetricsAsync(string host, int port)
	{
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0));
			HttpResponseMessage httpResponseMessage = await s_sharedHttpClient.GetAsync($"http://{host}:{port}/metrics", cts.Token);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return 0.0;
			}
			string metricsText = await httpResponseMessage.Content.ReadAsStringAsync();
			double totalBytes = 0.0;
			string[] lines = metricsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (string line in lines)
			{
				if (line.StartsWith("#") || line.StartsWith("llama_server_") || line.StartsWith("process_"))
				{
					continue;
				}
				string[] fields = line.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (fields.Length >= 2)
				{
					string metricName = fields[0];
					string metricValue = fields[1];
					if ((metricName.Contains("kv_cache_used") || metricName.Contains("gpu_buffer_used") || metricName.Contains("gpu_split_buffer_used")) && double.TryParse(metricValue, CultureInfo.InvariantCulture, out var parsedValue))
					{
						totalBytes += parsedValue;
					}
				}
			}
			return totalBytes / 1073741824.0;
		}
		catch
		{
			return 0.0;
		}
	}

	private string BuildServerArgs(ServerProfile profile)
	{
		return profile.BuildFullArgsString();
	}

	public void Dispose()
	{
		_healthCts?.Cancel();
		_healthCts?.Dispose();
		_cts?.Cancel();
		_cts?.Dispose();
		if (_process != null && !_process.HasExited)
		{
			try
			{
				_process.Kill(entireProcessTree: true);
			}
			catch
			{
			}
			_process.Dispose();
		}
	}

	private void OnLogReceived(LogEntry entry)
	{
		this.LogReceived?.Invoke(this, entry);
	}

	private bool IsPortInUse(int port)
	{
		try
		{
			using TcpListener tcpListener = new TcpListener(IPAddress.Loopback, port);
			tcpListener.Start();
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static string? ResolveExecutablePath(string configuredPath, string? activeVersion)
	{
		if (string.IsNullOrWhiteSpace(configuredPath))
		{
			return FindOnPath("llama-server");
		}
		if (!string.IsNullOrWhiteSpace(activeVersion))
		{
			string resolvedPath = ResolveSinglePath(Path.Combine(configuredPath, activeVersion));
			if (resolvedPath != null)
			{
				return resolvedPath;
			}
		}
		string fallbackPath = ResolveSinglePath(configuredPath);
		if (fallbackPath != null)
		{
			return fallbackPath;
		}
		return FindOnPath(configuredPath) ?? FindOnPath("llama-server");
	}

	private static string? ResolveSinglePath(string path)
	{
		if (File.Exists(path))
		{
			return Path.GetFullPath(path);
		}
		if (Directory.Exists(path))
		{
			foreach (string executableName in GetExecutableNames("llama-server"))
			{
				string candidatePath = Path.Combine(path, executableName);
				if (File.Exists(candidatePath))
				{
					return Path.GetFullPath(candidatePath);
				}
			}
		}
		return null;
	}

	private static string? FindOnPath(string executableName)
	{
		string[] pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (string path in pathEntries)
		{
			foreach (string candidateName in GetExecutableNames(executableName))
			{
				string candidatePath = Path.Combine(path, candidateName);
				if (File.Exists(candidatePath))
				{
					return Path.GetFullPath(candidatePath);
				}
			}
		}
		return null;
	}

	private static IEnumerable<string> GetExecutableNames(string executableName)
	{
		yield return executableName;
		if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(Path.GetExtension(executableName)))
		{
			yield return executableName + ".exe";
			yield return executableName + ".cmd";
			yield return executableName + ".bat";
		}
	}

	private static void ParseTpsFromOutput(string line, ServerManager manager)
	{
		if (string.IsNullOrEmpty(line))
		{
			return;
		}
		Match match = Regex.Match(line, "(?:kv\\s+self\\s+size|compute\\s+buffer\\s+size|ggml\\s+tensor\\s+size|uploading\\s+.*?to\\s+GPU)\\s*=\\s*([\\d.]+)\\s*MiB", RegexOptions.IgnoreCase);
		if (match.Success && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var sizeMiB) && sizeMiB > 0.0)
		{
			lock (manager._lock)
			{
				manager._status.VramUsedGb += sizeMiB / 1024.0;
			}
			manager.RaiseStatusChanged();
		}
		Match match2 = Regex.Match(line, "prompt processing.*?([\\d.]+)\\s+tokens?\\s+per\\s+second");
		if (match2.Success && double.TryParse(match2.Groups[1].Value, CultureInfo.InvariantCulture, out var promptTps) && promptTps > 0.0)
		{
			lock (manager._lock)
			{
				manager._status.PromptTokensPerSecond = promptTps;
			}
			manager.RaiseStatusChanged();
			return;
		}
		Match match3 = s_tpsRegexTg.Match(line);
		if (match3.Success && double.TryParse(match3.Groups[1].Value, CultureInfo.InvariantCulture, out var tgTps) && IsReliableGenerationRate(line, tgTps))
		{
			lock (manager._lock)
			{
				manager._currentTps = tgTps;
				manager._lastTpsUpdate = DateTime.UtcNow;
				manager._status.TokensPerSecond = tgTps;
			}
			manager.RaiseStatusChanged();
			return;
		}
		match3 = s_tpsRegexEval.Match(line);
		if (match3.Success && double.TryParse(match3.Groups[1].Value, CultureInfo.InvariantCulture, out var evalTps) && IsReliableGenerationRate(line, evalTps))
		{
			lock (manager._lock)
			{
				manager._currentTps = evalTps;
				manager._lastTpsUpdate = DateTime.UtcNow;
				manager._status.TokensPerSecond = evalTps;
			}
			manager.RaiseStatusChanged();
			return;
		}
		Match match4 = Regex.Match(line, "([\\d.]+)\\s+tokens?\\s+per\\s+second");
		if (match4.Success && double.TryParse(match4.Groups[1].Value, CultureInfo.InvariantCulture, out var genericTps) && IsReliableGenerationRate(line, genericTps))
		{
			lock (manager._lock)
			{
				manager._currentTps = genericTps;
				manager._lastTpsUpdate = DateTime.UtcNow;
				manager._status.TokensPerSecond = genericTps;
			}
			manager.RaiseStatusChanged();
		}
	}

	private static bool IsReliableGenerationRate(string line, double rate)
	{
		if (rate <= 0.0 || rate > 1000.0)
		{
			return false;
		}
		// A one-token probe can produce an artificial 100000+ tok/s value.
		Match sample = Regex.Match(line, @"(?:/|n_decoded\s*=)\s*(\d+)\s*(?:tokens?)?", RegexOptions.IgnoreCase);
		return !sample.Success || !int.TryParse(sample.Groups[1].Value, out var count) || count >= 8;
	}

	private void RaiseStatusChanged()
	{
		ServerStatus status;
		lock (_lock)
		{
			status = new ServerStatus
			{
				State = _status.State,
				Port = _status.Port,
				Host = _status.Host,
				ModelName = _status.ModelName,
				ContextSize = _status.ContextSize,
				Threads = _status.Threads,
				GpuLayers = _status.GpuLayers,
				VramUsedGb = _status.VramUsedGb,
				RamUsedGb = _status.RamUsedGb,
				RamCommittedGb = _status.RamCommittedGb,
				CacheRamLimitGb = _status.CacheRamLimitGb,
				TokensPerSecond = _status.TokensPerSecond,
				PromptTokensPerSecond = _status.PromptTokensPerSecond,
				QueueSize = _status.QueueSize,
				ActiveSlots = _status.ActiveSlots,
				TotalTokensProcessed = _status.TotalTokensProcessed,
				Uptime = _status.Uptime,
				StartedAt = _status.StartedAt,
				ErrorMessage = _status.ErrorMessage,
				ProcessId = _status.ProcessId
			};
		}
		this.StatusChanged?.Invoke(this, status);
	}
}
