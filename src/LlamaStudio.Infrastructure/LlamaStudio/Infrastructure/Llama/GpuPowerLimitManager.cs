using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LlamaStudio.Infrastructure.Llama;

/// <summary>One GPU's current and allowed power-limit range.</summary>
public sealed record GpuPowerLimitItem(int Index, string Name, double CurrentWatts, double MinWatts, double MaxWatts, double LimitWatts)
{
public string RangeDisplay => MinWatts > 0
		? $"{MinWatts:F0}-{MaxWatts:F0} W"
		: $"{MaxWatts:F0} W max";

	public string CurrentDisplay => $"{CurrentWatts:F0} W";

public string LimitDisplay => $"{LimitWatts:F0} W";
}

/// <summary>
/// Reads and sets per-GPU power limits through nvidia-smi.
/// <c>nvidia-smi -i &lt;id&gt; -pl &lt;watts&gt;</c> needs root; the manager
/// first tries without sudo and transparently retries with plain
/// <c>sudo</c> when the driver reports Insufficient Permissions.
/// r89: NO <c>sudo -n</c> — the Rust sudo (sudo-rs) on Debian/Ubuntu builds
/// rejects the classic -n flag with a clap error ("unexpected argument
/// '-n' found"), which surfaced to the user instead of the real result.
/// Plain sudo works with cached/no-password sudoers and fails fast with a
/// detectable no-tty/password message otherwise (plus a hard timeout).
/// </summary>
public static class GpuPowerLimitManager
{
	/// <summary>Lists all local GPUs with their current power limit and the allowed range.</summary>
	public static async Task<IReadOnlyList<GpuPowerLimitItem>> GetItemsAsync()
	{
		(string output, _) = await RunAsync(new[] { "nvidia-smi", "--query-gpu=index,name,power.draw,power.min_limit,power.max_limit,power.limit", "--format=csv,noheader" }, null).ConfigureAwait(false);
		List<GpuPowerLimitItem> items = new List<GpuPowerLimitItem>();
		foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] parts = line.Split(',', StringSplitOptions.TrimEntries);
			if (parts.Length < 6)
			{
				continue;
			}
			if (!int.TryParse(parts[0], out int index))
			{
				continue;
			}
			items.Add(new GpuPowerLimitItem(index, parts[1], Number(parts[2]), Number(parts[3]), Number(parts[4]), Number(parts[5])));
		}
		return items;
	}

	/// <summary>
	/// Sets the power limit for one GPU. Returns (ok, human-readable message).
	/// minWatts/maxWatts (from the query, optional) enable a client-side
	/// range check before touching nvidia-smi at all.
	/// </summary>
	public static async Task<(bool Ok, string Message)> SetPowerLimitAsync(int index, double watts, double? minWatts = null, double? maxWatts = null)
	{
		if (!double.IsFinite(watts) || watts <= 0.0)
		{
			return (false, "Power limit must be a positive number of watts.");
		}
		// r89: client-side range check against nvidia-smi's own reported
		// power.min_limit / power.max_limit — a clean message instead of the
		// driver's cryptic "not a valid power limit which should be between".
		double rangeMin = minWatts ?? 0.0;
		double rangeMax = maxWatts ?? 0.0;
		if (rangeMin > 0.0 && watts < rangeMin)
		{
			return (false, $"GPU {index}: {watts:0.##} W ниже минимума {rangeMin:0.##} W (диапазон {RangeText(rangeMin, rangeMax)}).");
		}
		if (rangeMax > 0.0 && watts > rangeMax)
		{
			return (false, $"GPU {index}: {watts:0.##} W выше максимума {rangeMax:0.##} W (диапазон {RangeText(rangeMin, rangeMax)}).");
		}
		string formatted = watts.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
		string idx = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
		(string Output, bool SudoUsed) result = await RunAsync(new[] { "nvidia-smi", "-i", idx, "-pl", formatted }, "plain").ConfigureAwait(false);
		bool needsSudo = !LooksApplied(result.Output) && LooksLikePermissionFailure(result.Output);
		if (needsSudo)
		{
			if (OperatingSystem.IsWindows())
			{
				return (false, $"GPU {index}: nvidia-smi требует права администратора. Запустите приложение от имени администратора или в elevated-консоли: nvidia-smi -i {idx} -pl {formatted}");
			}
			// r89: plain sudo (no -n — see the class comment). The 20 s
			// timeout covers any sudo build that waits for a password even
			// without a tty.
			result = await RunAsync(new[] { "sudo", "nvidia-smi", "-i", idx, "-pl", formatted }, "sudo", 20).ConfigureAwait(false);
		}
		if (LooksApplied(result.Output))
		{
			string applied = ParseAppliedWatts(result.Output);
			string how = result.SudoUsed ? " (через sudo)" : string.Empty;
			return (true, $"GPU {index}: лимит мощности установлен: {applied} W{how}.");
		}
		// r89: translate the driver's range error into the REAL settable
		// range (can differ from the queried min/max on some GPUs/drivers).
		string rangeDetail = ParsePowerRangeError(result.Output);
		if (rangeDetail != null)
		{
			return (false, $"GPU {index}: {rangeDetail}");
		}
		string detail = FirstLine(result.Output, 300);
		if (LooksLikeSudoBlocked(detail))
		{
			return (false, $"GPU {index}: sudo требует пароль или недоступен без терминала. Выполните вручную: sudo nvidia-smi -i {idx} -pl {formatted}");
		}
		return (false, $"GPU {index}: {detail}");
	}

	private static string RangeText(double min, double max)
	{
		string lo = min > 0.0 ? min.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "…";
		return $"{lo}–{max:0.##} W";
	}

	/// <summary>"...should be between 100.00 W and 300.00 W for GPU..." → clean RU message.</summary>
	private static string? ParsePowerRangeError(string output)
	{
		Match match = Regex.Match(output, @"should be between\s+([0-9]+(?:\.[0-9]+)?)\s*W\s+and\s+([0-9]+(?:\.[0-9]+)?)\s*W", RegexOptions.IgnoreCase);
		return match.Success
			? $"значение вне диапазона {match.Groups[1].Value}–{match.Groups[2].Value} W (nvidia-smi)"
			: null;
	}

	private static bool LooksLikeSudoBlocked(string detail) =>
		detail.Contains("password", StringComparison.OrdinalIgnoreCase)
		|| detail.Contains("terminal is required", StringComparison.OrdinalIgnoreCase)
		|| detail.Contains("no tty", StringComparison.OrdinalIgnoreCase)
		|| detail.Contains("sudoers", StringComparison.OrdinalIgnoreCase)
		|| detail.Contains("not authorized", StringComparison.OrdinalIgnoreCase);

	private static bool LooksApplied(string output) =>
		output.Contains("was set to", StringComparison.OrdinalIgnoreCase);

	private static bool LooksLikePermissionFailure(string output) =>
		output.Contains("Insufficient Permissions", StringComparison.OrdinalIgnoreCase)
		|| output.Contains("Insufficient permissions", StringComparison.OrdinalIgnoreCase)
		|| output.Contains("not allowed", StringComparison.OrdinalIgnoreCase);

	private static string ParseAppliedWatts(string output)
	{
		Match match = Regex.Match(output, @"set to\s+(?<w>[0-9]+(?:\.[0-9]+)?)\s*W", RegexOptions.IgnoreCase);
		return match.Success ? match.Groups["w"].Value : string.Empty;
	}

	private static string FirstLine(string output, int max)
	{
		string text = output.Replace('\r', ' ').Trim();
		return text.Length <= max ? text : text[..max] + "…";
	}

	private static double Number(string value)
	{
		return double.TryParse(value.Replace("W", string.Empty).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result)
			? result
			: 0.0;
	}

	private static async Task<(string Output, bool SudoUsed)> RunAsync(string[] argv, string? mode, int? timeoutSeconds = null)
	{
		var startInfo = new ProcessStartInfo(argv[0], string.Empty)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		foreach (string argument in argv.Skip(1))
		{
			startInfo.ArgumentList.Add(argument);
		}
		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start " + argv[0]);
		Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
		Task<string> errorTask = process.StandardError.ReadToEndAsync();
		using System.Threading.CancellationTokenSource? timeoutCts = timeoutSeconds.HasValue
			? new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value))
			: null;
		bool timedOut = false;
		try
		{
			await process.WaitForExitAsync(timeoutCts?.Token ?? System.Threading.CancellationToken.None).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// r89: sudo (or anything else) hung waiting for input — kill it.
			timedOut = true;
			try { process.Kill(true); } catch { /* already gone */ }
		}
		string output, error;
		try
		{
			output = timedOut ? string.Empty : await outputTask.ConfigureAwait(false);
			error = timedOut ? string.Empty : await errorTask.ConfigureAwait(false);
		}
		catch
		{
			output = string.Empty;
			error = string.Empty;
		}
		if (timedOut)
		{
			return ("timeout after " + timeoutSeconds + " s (процесс, вероятно, ждал пароль в терминале)", string.Equals(mode, "sudo", StringComparison.Ordinal));
		}
		return (output + Environment.NewLine + error, string.Equals(mode, "sudo", StringComparison.Ordinal));
	}
}
