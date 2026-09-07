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
/// first tries without sudo and transparently retries with passwordless
/// sudo (<c>sudo -n</c>) when the driver reports Insufficient Permissions.
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
	/// </summary>
	public static async Task<(bool Ok, string Message)> SetPowerLimitAsync(int index, double watts)
	{
		if (!double.IsFinite(watts) || watts <= 0.0)
		{
			return (false, "Power limit must be a positive number of watts.");
		}
		string formatted = watts.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
		(string Output, bool SudoUsed) result = await RunAsync(new[] { "nvidia-smi", "-i", index.ToString(System.Globalization.CultureInfo.InvariantCulture), "-pl", formatted }, "plain").ConfigureAwait(false);
		bool needsSudo = !LooksApplied(result.Output) && LooksLikePermissionFailure(result.Output);
		if (needsSudo)
		{
			result = await RunAsync(new[] { "sudo", "-n", "nvidia-smi", "-i", index.ToString(System.Globalization.CultureInfo.InvariantCulture), "-pl", formatted }, "sudo").ConfigureAwait(false);
		}
		if (LooksApplied(result.Output))
		{
			string applied = ParseAppliedWatts(result.Output);
			string how = result.SudoUsed ? " (via sudo)" : string.Empty;
			return (true, $"GPU {index}: power limit set to {applied} W{how}.");
		}
		string detail = FirstLine(result.Output, 300);
		if (needsSudo && result.SudoUsed && detail.Contains("password", StringComparison.OrdinalIgnoreCase))
		{
			return (false, $"GPU {index}: sudo requires a password. Run: sudo nvidia-smi -i {index} -pl {formatted}");
		}
		return (false, $"GPU {index}: {detail}");
	}

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

	private static async Task<(string Output, bool SudoUsed)> RunAsync(string[] argv, string? mode)
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
		try
		{
			await process.WaitForExitAsync().ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// No cancellation token in this path; keep the signature simple.
		}
		string output = await outputTask.ConfigureAwait(false);
		string error = await errorTask.ConfigureAwait(false);
		return (output + Environment.NewLine + error, string.Equals(mode, "sudo", StringComparison.Ordinal));
	}
}
