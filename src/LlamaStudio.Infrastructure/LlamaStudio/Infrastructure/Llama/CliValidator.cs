using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class CliValidator : ICliValidator
{
	private readonly IHelpParser _helpParser;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private static readonly string[] AllAppFlags = new string[120]
	{
		"-m", "--model", "-hf", "--hf-repo", "-hff", "--hf-file", "--offline", "-hfd", "--hf-repo-draft", "-md",
		"--spec-draft-model", "-mm", "--mmproj", "--host", "--port", "-to", "--timeout", "-np", "--parallel", "--slots",
		"--no-slots", "--api-key", "-a", "--alias", "--log-file", "-c", "--ctx-size", "-t", "--threads", "-tb",
		"--threads-batch", "-n", "--predict", "-ngl", "--gpu-layers", "-sm", "--split-mode", "-ts", "--tensor-split", "-mg",
		"--main-gpu", "-ngld", "--spec-draft-ngl", "--gpu-layers-draft", "-fa", "--flash-attn", "--no-mmap", "--mmap", "--mlock", "-nkvo",
		"--no-kv-offload", "-b", "--batch-size", "-ub", "--ubatch-size", "-ctk", "--cache-type-k", "-ctv", "--cache-type-v", "--cache-ram",
		"--cache-reuse", "--temp", "--temperature", "--top-k", "--top-p", "--min-p", "--typical-p", "--typical", "--repeat-penalty", "--repeat-last-n",
		"--presence-penalty", "--frequency-penalty", "--mirostat", "--mirostat-ent", "--mirostat-lr", "--dry-multiplier", "--dry-base", "--dynatemp-range", "--xtc-probability", "--xtc-threshold",
		"--spec-type", "--spec-draft-n-max", "--spec-draft-n-min", "--spec-draft-p-split", "--draft-p-split", "--spec-draft-p-min", "--draft-p-min", "--rope-scaling", "--rope-freq-base", "--rope-freq-scale",
		"--yarn-orig-ctx", "--yarn-ext-factor", "--yarn-attn-factor", "--yarn-beta-fast", "--yarn-beta-slow", "--cache-prompt", "--no-cache-prompt", "-cb", "--cont-batching", "-nocb",
		"--no-cont-batching", "--context-shift", "--no-context-shift", "-lv", "--ui", "--webui", "--no-ui", "--no-webui", "--metrics", "-rea",
		"--reasoning", "--reasoning-budget", "--embedding", "--embeddings", "--pooling", "--numa", "-C", "--cpu-mask", "-s", "--seed"
	};

	public CliValidator(IHelpParser helpParser, ISettings settings, ILogService log)
	{
		_helpParser = helpParser;
		_settings = settings;
		_log = log;
	}

	public string GetSnapshotsDirectory()
	{
		string baseDirectory = Path.GetDirectoryName(_settings.GetSettingsPath());
		if (string.IsNullOrEmpty(baseDirectory))
		{
			baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}
		string snapshotsDirectory = Path.Combine(baseDirectory, "cli_snapshots");
		Directory.CreateDirectory(snapshotsDirectory);
		return snapshotsDirectory;
	}

	public async Task<ValidationReport> ValidateAllFlagsAsync(string serverExePath, string version)
	{
		_log.Information("Validating CLI flags for " + version + "...", "CliValidator");
		LlamaHelpInfo llamaHelpInfo = await _helpParser.ParseAsync(serverExePath);
		if (llamaHelpInfo == null)
		{
			return new ValidationReport
			{
				Timestamp = DateTime.UtcNow,
				ServerExePath = serverExePath,
				Version = version,
				Status = ValidationStatus.Warnings,
				RemovedFlags = new List<RemovedFlag>(1)
				{
					new RemovedFlag
					{
						OurFlag = "(all flags — failed to parse --help)",
						SuggestedReplacement = "Manually verify llama-server.exe --help"
					}
				}
			};
		}
		SaveSnapshotAsync(llamaHelpInfo, version);
		List<RemovedFlag> removedFlags = new List<RemovedFlag>();
		HashSet<string> seenFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] appFlags = AllAppFlags;
		foreach (string appFlag in appFlags)
		{
			if (!llamaHelpInfo.IsSupported(appFlag))
			{
				string normalizedFlag = NormalizeFlag(appFlag);
				if (!seenFlags.Contains(normalizedFlag))
				{
					seenFlags.Add(normalizedFlag);
					removedFlags.Add(new RemovedFlag
					{
						OurFlag = appFlag,
						SuggestedReplacement = FindSuggestion(appFlag, llamaHelpInfo)
					});
				}
			}
		}
		HashSet<string> normalizedAppFlags = new HashSet<string>(AllAppFlags.Select(NormalizeFlag), StringComparer.OrdinalIgnoreCase);
		List<CliFlagInfo> newAvailableFlags = new List<CliFlagInfo>();
		foreach (KeyValuePair<string, CliFlagInfo> flag in llamaHelpInfo.Flags)
		{
			if (!normalizedAppFlags.Contains(NormalizeFlag(flag.Key)) && !flag.Key.StartsWith(".") && !flag.Key.Contains("version"))
			{
				newAvailableFlags.Add(flag.Value);
			}
		}
		ValidationStatus status = ((removedFlags.Count != 0) ? ValidationStatus.Errors : ValidationStatus.Ok);
		ValidationReport result = new ValidationReport
		{
			Timestamp = DateTime.UtcNow,
			ServerExePath = serverExePath,
			Version = version,
			RemovedFlags = removedFlags,
			NewAvailableFlags = newAvailableFlags.OrderBy((CliFlagInfo f) => f.Name).ToList(),
			ChangedFlags = new List<ChangedFlag>(),
			Status = status
		};
		if (removedFlags.Count > 0)
		{
			_log.Warning($"Found {removedFlags.Count} unsupported flags in {version}", "CliValidator");
			foreach (RemovedFlag removedFlag in removedFlags)
			{
				string message = ((removedFlag.SuggestedReplacement != null) ? ("  " + removedFlag.OurFlag + " → replaced by: " + removedFlag.SuggestedReplacement) : ("  " + removedFlag.OurFlag + " — not found in --help"));
				_log.Warning(message, "CliValidator");
			}
		}
		if (newAvailableFlags.Count > 0)
		{
			_log.Information($"Found {newAvailableFlags.Count} new flags available in {version}", "CliValidator");
		}
		return result;
	}

	public Task<CliChangeReport> CompareVersionsAsync(LlamaHelpInfo oldHelp, LlamaHelpInfo newHelp, string oldVersion, string newVersion)
	{
		_log.Information("Comparing CLI: " + oldVersion + " → " + newVersion, "CliValidator");
		HashSet<string> oldFlags = oldHelp.Flags.Keys.Select(NormalizeFlag).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> newFlags = newHelp.Flags.Keys.Select(NormalizeFlag).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<CliFlagInfo> addedFlags = (from CliFlagInfo f in from n in newFlags.Except(oldFlags).ToList()
				select newHelp.Flags.Values.FirstOrDefault((CliFlagInfo f) => NormalizeFlag(f.Name) == n) into f
				where f != null
				select f
			orderby f.Name
			select f).ToList();
		List<string> removedFlagNames = oldFlags.Except(newFlags).ToList();
		List<string> removedFlags = removedFlagNames.Select((string n) => oldHelp.Flags.Values.FirstOrDefault((CliFlagInfo f) => NormalizeFlag(f.Name) == n)?.Name ?? n).ToList();
		List<string> commonFlags = oldFlags.Intersect(newFlags).ToList();
		List<ChangedFlag> changedFlags = new List<ChangedFlag>();
		foreach (string name in commonFlags)
		{
			CliFlagInfo cliFlagInfo = oldHelp.Flags.Values.FirstOrDefault((CliFlagInfo f) => NormalizeFlag(f.Name) == name);
			CliFlagInfo cliFlagInfo2 = newHelp.Flags.Values.FirstOrDefault((CliFlagInfo f) => NormalizeFlag(f.Name) == name);
			if (cliFlagInfo != null && cliFlagInfo2 != null)
			{
				bool isChanged = false;
				ChangedFlag change = new ChangedFlag
				{
					FlagName = cliFlagInfo2.Name,
					OldDescription = cliFlagInfo.Description,
					NewDescription = cliFlagInfo2.Description,
					OldDefault = cliFlagInfo.DefaultValue,
					NewDefault = cliFlagInfo2.DefaultValue
				};
				if (cliFlagInfo.Description != cliFlagInfo2.Description)
				{
					isChanged = true;
				}
				if (cliFlagInfo.DefaultValue != cliFlagInfo2.DefaultValue)
				{
					isChanged = true;
				}
				if (isChanged)
				{
					changedFlags.Add(change);
				}
			}
		}
		HashSet<string> appFlagSet = new HashSet<string>(AllAppFlags.Select(NormalizeFlag), StringComparer.OrdinalIgnoreCase);
		List<CriticalChange> criticalChanges = new List<CriticalChange>();
		foreach (string removedFlagName in removedFlagNames)
		{
			if (appFlagSet.Contains(removedFlagName))
			{
				criticalChanges.Add(new CriticalChange
				{
					FlagName = removedFlagName,
					OldVersion = oldVersion,
					NewVersion = newVersion,
					SuggestedReplacement = FindSuggestionInNew(removedFlagName, newHelp)
				});
			}
		}
		CliChangeReport cliChangeReport = new CliChangeReport
		{
			OldVersion = oldVersion,
			NewVersion = newVersion,
			AddedFlags = addedFlags,
			RemovedFlags = removedFlags,
			ModifiedFlags = changedFlags,
			CriticalChanges = criticalChanges
		};
		if (cliChangeReport.HasCriticalChanges)
		{
			_log.Error($"CRITICAL: {criticalChanges.Count} used flags removed in {newVersion}", "CliValidator");
			foreach (CriticalChange criticalChange in criticalChanges)
			{
				string message = ((criticalChange.SuggestedReplacement != null) ? ("  " + criticalChange.FlagName + " → use: " + criticalChange.SuggestedReplacement) : ("  " + criticalChange.FlagName + " — removed without replacement"));
				_log.Error(message, "CliValidator");
			}
		}
		return Task.FromResult(cliChangeReport);
	}

	public Task SaveSnapshotAsync(LlamaHelpInfo helpInfo, string version)
	{
		try
		{
			string snapshotsDirectory = GetSnapshotsDirectory();
			string safeVersion = version.Replace("/", "_").Replace("\\", "_");
			string path = Path.Combine(snapshotsDirectory, safeVersion + ".json");
			string contents = JsonSerializer.Serialize(CliFlagSnapshot.FromHelpInfo(helpInfo, version), new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(path, contents);
			_log.Information($"Saved CLI snapshot: {version} ({helpInfo.Flags.Count} flags)", "CliValidator");
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to save CLI snapshot for " + version + ": " + ex.Message, "CliValidator");
		}
		return Task.CompletedTask;
	}

	public async Task<LlamaHelpInfo?> LoadSnapshotAsync(string version)
	{
		try
		{
			string snapshotsDirectory = GetSnapshotsDirectory();
			string safeVersion = version.Replace("/", "_").Replace("\\", "_");
			string path = Path.Combine(snapshotsDirectory, safeVersion + ".json");
			if (!File.Exists(path))
			{
				return null;
			}
			CliFlagSnapshot cliFlagSnapshot = JsonSerializer.Deserialize<CliFlagSnapshot>(await File.ReadAllTextAsync(path));
			if (cliFlagSnapshot == null)
			{
				return null;
			}
			_log.Information($"Loaded CLI snapshot: {version} ({cliFlagSnapshot.Flags.Count} flags)", "CliValidator");
			return cliFlagSnapshot.ToHelpInfo();
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to load CLI snapshot for " + version + ": " + ex.Message, "CliValidator");
			return null;
		}
	}

	public async Task<LlamaHelpInfo?> LoadLatestSnapshotAsync()
	{
		try
		{
			string snapshotsDirectory = GetSnapshotsDirectory();
			if (!Directory.Exists(snapshotsDirectory))
			{
				return null;
			}
			string snapshotFile = (from f in Directory.GetFiles(snapshotsDirectory, "*.json")
				orderby File.GetLastWriteTime(f) descending
				select f).FirstOrDefault();
			if (snapshotFile == null)
			{
				return null;
			}
			CliFlagSnapshot cliFlagSnapshot = JsonSerializer.Deserialize<CliFlagSnapshot>(await File.ReadAllTextAsync(snapshotFile));
			if (cliFlagSnapshot == null)
			{
				return null;
			}
			_log.Information("Loaded latest CLI snapshot: " + cliFlagSnapshot.Version, "CliValidator");
			return cliFlagSnapshot.ToHelpInfo();
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to load latest CLI snapshot: " + ex.Message, "CliValidator");
			return null;
		}
	}

	private static string NormalizeFlag(string flag)
	{
		return flag.TrimStart('-').ToLowerInvariant();
	}

	private static string? FindSuggestion(string removedFlag, LlamaHelpInfo helpInfo)
	{
		string normalized = NormalizeFlag(removedFlag);
		Dictionary<string, string[]> suggestionMap = new Dictionary<string, string[]>();
		suggestionMap.Add("draft-mtp", new string[1] { "--spec-type" });
		suggestionMap.Add("flash-attn-new", new string[2] { "-fa", "--flash-attn" });
		suggestionMap.Add("cfa", new string[2] { "-fa", "--flash-attn" });
		suggestionMap.Add("n-gpu-layers", new string[2] { "-ngl", "--gpu-layers" });
		suggestionMap.Add("model-draft", new string[2] { "-md", "--spec-draft-model" });
		suggestionMap.Add("draft-model", new string[2] { "-md", "--spec-draft-model" });
		suggestionMap.Add("max-tokens", new string[2] { "-n", "--predict" });
		suggestionMap.Add("no-mmq", Array.Empty<string>());
		suggestionMap.Add("mirostat-tau", new string[1] { "--mirostat-ent" });
		suggestionMap.Add("mirostat-learn-rate", new string[1] { "--mirostat-lr" });
		suggestionMap.Add("mirostat-eta", new string[1] { "--mirostat-lr" });
		suggestionMap.Add("rope-frequency-base", new string[1] { "--rope-freq-base" });
		suggestionMap.Add("rope-frequency-scale", new string[1] { "--rope-freq-scale" });
		suggestionMap.Add("n-predict", new string[2] { "-n", "--predict" });
		string removedFlagCompact = normalized.Replace("-", "").Replace("_", "");
		foreach (KeyValuePair<string, string[]> entry in suggestionMap)
		{
			entry.Deconstruct(out var key, out var value);
			string oldFlag = key;
			string[] suggestionFlags = value;
			string oldFlagCompact = oldFlag.Replace("-", "").Replace("_", "");
			if (!(removedFlagCompact == oldFlagCompact) && !removedFlagCompact.Contains(oldFlagCompact) && !oldFlagCompact.Contains(removedFlagCompact))
			{
				continue;
			}
			value = suggestionFlags;
			foreach (string candidate in value)
			{
				if (helpInfo.IsSupported(candidate))
				{
					return candidate;
				}
			}
		}
		return (from f in helpInfo.Flags.Values
			where !AllAppFlags.Contains(f.Name)
			orderby LevenshteinDistance(normalized, NormalizeFlag(f.Name))
			select f).FirstOrDefault((CliFlagInfo f) => LevenshteinDistance(normalized, NormalizeFlag(f.Name)) <= 3)?.Name;
	}

	private static string? FindSuggestionInNew(string removedFlag, LlamaHelpInfo newHelp)
	{
		return FindSuggestion(removedFlag, newHelp);
	}

	private static int LevenshteinDistance(string a, string b)
	{
		if (a == b)
		{
			return 0;
		}
		if (a.Length == 0)
		{
			return b.Length;
		}
		if (b.Length == 0)
		{
			return a.Length;
		}
		int[,] distanceMatrix = new int[a.Length + 1, b.Length + 1];
		for (int i = 0; i <= a.Length; i++)
		{
			distanceMatrix[i, 0] = i;
		}
		for (int j = 0; j <= b.Length; j++)
		{
			distanceMatrix[0, j] = j;
		}
		for (int k = 1; k <= a.Length; k++)
		{
			for (int l = 1; l <= b.Length; l++)
			{
				int cost = ((a[k - 1] != b[l - 1]) ? 1 : 0);
				distanceMatrix[k, l] = Math.Min(Math.Min(distanceMatrix[k - 1, l] + 1, distanceMatrix[k, l - 1] + 1), distanceMatrix[k - 1, l - 1] + cost);
			}
		}
		return distanceMatrix[a.Length, b.Length];
	}
}
