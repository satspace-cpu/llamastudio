using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Llama;

/// <summary>
/// Validates CLI flags against llama.cpp binary, manages snapshots, and compares versions.
/// </summary>
public class CliValidator : ICliValidator
{
    readonly IHelpParser _helpParser;
    readonly ISettings _settings;
    readonly ILogService _log;

    /// <summary>
    /// All CLI flags that our application can potentially generate.
    /// This is the master list used for validation.
    /// </summary>
    static readonly string[] AllAppFlags =
    [
        // Model paths
        "-m", "--model",
        "-hf", "--hf-repo",
        "-hff", "--hf-file",
        "--offline",
        "-hfd", "--hf-repo-draft",
        "-md", "--spec-draft-model",
        "-mm", "--mmproj",

        // Connection
        "--host", "--port",
        "-to", "--timeout",
        "-np", "--parallel",
        "--slots", "--no-slots",
        "--api-key",
        "-a", "--alias",
        "--log-file",

        // Context & Threads
        "-c", "--ctx-size",
        "-t", "--threads",
        "-tb", "--threads-batch",
        "-n", "--predict",

        // GPU
        "-ngl", "--gpu-layers",
        "-sm", "--split-mode",
        "-ts", "--tensor-split",
        "-mg", "--main-gpu",
        "-ngld", "--spec-draft-ngl", "--gpu-layers-draft",

        // Memory flags
        "-fa", "--flash-attn",
        "--no-mmap", "--mmap",
        "--mlock",
        "-nkvo", "--no-kv-offload",

        // Batch
        "-b", "--batch-size",
        "-ub", "--ubatch-size",
        "-ctk", "--cache-type-k",
        "-ctv", "--cache-type-v",
        "--cache-ram",
        "--cache-reuse",

        // Sampling
        "--temp", "--temperature",
        "--top-k",
        "--top-p",
        "--min-p",
        "--typical-p", "--typical",
        "--repeat-penalty",
        "--repeat-last-n",
        "--presence-penalty",
        "--frequency-penalty",

        // Mirostat
        "--mirostat",
        "--mirostat-ent",
        "--mirostat-lr",

        // DRY / Dynatemp / XTC
        "--dry-multiplier",
        "--dry-base",
        "--dynatemp-range",
        "--xtc-probability",
        "--xtc-threshold",

        // Speculative / MTP
        "--spec-type",
        "--spec-draft-n-max",
        "--spec-draft-n-min",
        "--spec-draft-p-split", "--draft-p-split",
        "--spec-draft-p-min", "--draft-p-min",

        // Rope / YARN
        "--rope-scaling",
        "--rope-freq-base",
        "--rope-freq-scale",
        "--yarn-orig-ctx",
        "--yarn-ext-factor",
        "--yarn-attn-factor",
        "--yarn-beta-fast",
        "--yarn-beta-slow",

        // Toggles
        "--cache-prompt", "--no-cache-prompt",
        "-cb", "--cont-batching",
        "-nocb", "--no-cont-batching",
        "--context-shift",
        "--no-context-shift",
        "-lv",
        "--ui", "--webui",
        "--no-ui", "--no-webui",
        "--metrics",
        "-rea", "--reasoning",
        "--reasoning-budget",
        "--embedding", "--embeddings",
        "--pooling",
        "--numa",
        "-C", "--cpu-mask",

        // Misc
        "-s", "--seed",
    ];

    public CliValidator(IHelpParser helpParser, ISettings settings, ILogService log)
    {
        _helpParser = helpParser;
        _settings = settings;
        _log = log;
    }

    /// <summary>
    /// Get the directory where CLI snapshots are stored.
    /// </summary>
    public string GetSnapshotsDirectory()
    {
        var configDir = Path.GetDirectoryName(_settings.GetSettingsPath());
        if (string.IsNullOrEmpty(configDir))
            configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var snapshotDir = Path.Combine(configDir, "cli_snapshots");
        Directory.CreateDirectory(snapshotDir);
        return snapshotDir;
    }

    /// <summary>
    /// Validate all flags our app can generate against the current binary.
    /// </summary>
    public async Task<ValidationReport> ValidateAllFlagsAsync(string serverExePath, string version)
    {
        _log.Information($"Validating CLI flags for {version}...", "CliValidator");

        var helpInfo = await _helpParser.ParseAsync(serverExePath);

        if (helpInfo == null)
        {
            return new ValidationReport
            {
                Timestamp = DateTime.UtcNow,
                ServerExePath = serverExePath,
                Version = version,
                Status = ValidationStatus.Warnings,
                RemovedFlags =
                [
                    new RemovedFlag { OurFlag = "(all flags — failed to parse --help)", SuggestedReplacement = "Manually verify llama-server.exe --help" }
                ],
            };
        }

        // Save snapshot for future comparison
        _ = SaveSnapshotAsync(helpInfo, version);

        // Find removed flags (flags we use but binary doesn't support)
        var removedFlags = new List<RemovedFlag>();
        var checkedCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var flag in AllAppFlags)
        {
            if (!helpInfo.IsSupported(flag))
            {
                // Avoid duplicates: check canonical form
                var canonical = NormalizeFlag(flag);
                if (!checkedCanonical.Contains(canonical))
                {
                    checkedCanonical.Add(canonical);
                    removedFlags.Add(new RemovedFlag
                    {
                        OurFlag = flag,
                        SuggestedReplacement = FindSuggestion(flag, helpInfo),
                    });
                }
            }
        }

        // Find new flags (in binary but not in our app list)
        var appFlagSet = new HashSet<string>(AllAppFlags.Select(NormalizeFlag), StringComparer.OrdinalIgnoreCase);
        var newAvailable = new List<CliFlagInfo>();
        foreach (var kvp in helpInfo.Flags)
        {
            if (!appFlagSet.Contains(NormalizeFlag(kvp.Key)))
            {
                // Skip internal/hidden flags
                if (!kvp.Key.StartsWith(".") && !kvp.Key.Contains("version"))
                    newAvailable.Add(kvp.Value);
            }
        }

        // Determine status
        var status = removedFlags.Count == 0 ? ValidationStatus.Ok : ValidationStatus.Errors;

        var report = new ValidationReport
        {
            Timestamp = DateTime.UtcNow,
            ServerExePath = serverExePath,
            Version = version,
            RemovedFlags = removedFlags,
            NewAvailableFlags = newAvailable.OrderBy(f => f.Name).ToList(),
            ChangedFlags = new List<ChangedFlag>(),
            Status = status,
        };

        if (removedFlags.Count > 0)
        {
            _log.Warning($"Found {removedFlags.Count} unsupported flags in {version}", "CliValidator");
            foreach (var rf in removedFlags)
            {
                var msg = rf.SuggestedReplacement != null
                    ? $"  {rf.OurFlag} → replaced by: {rf.SuggestedReplacement}"
                    : $"  {rf.OurFlag} — not found in --help";
                _log.Warning(msg, "CliValidator");
            }
        }

        if (newAvailable.Count > 0)
        {
            _log.Information($"Found {newAvailable.Count} new flags available in {version}", "CliValidator");
        }

        return report;
    }

    /// <summary>
    /// Compare two versions' help output to detect CLI changes.
    /// </summary>
    public Task<CliChangeReport> CompareVersionsAsync(LlamaHelpInfo oldHelp, LlamaHelpInfo newHelp, string oldVersion, string newVersion)
    {
        _log.Information($"Comparing CLI: {oldVersion} → {newVersion}", "CliValidator");

        var oldFlagNames = oldHelp.Flags.Keys.Select(NormalizeFlag).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newFlagNames = newHelp.Flags.Keys.Select(NormalizeFlag).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Flags added in new version
        var addedNames = newFlagNames.Except(oldFlagNames).ToList();
        var addedFlags = addedNames
            .Select(n => newHelp.Flags.Values.FirstOrDefault(f => NormalizeFlag(f.Name) == n))
            .Where(f => f != null)
            .Cast<CliFlagInfo>()
            .OrderBy(f => f.Name)
            .ToList();

        // Flags removed in new version
        var removedNames = oldFlagNames.Except(newFlagNames).ToList();
        var removedFlags = removedNames.Select(n =>
        {
            var match = oldHelp.Flags.Values.FirstOrDefault(f => NormalizeFlag(f.Name) == n);
            return match?.Name ?? n;
        }).ToList();

        // Flags that exist in both but changed
        var commonNames = oldFlagNames.Intersect(newFlagNames).ToList();
        var changedFlags = new List<ChangedFlag>();
        foreach (var name in commonNames)
        {
            var oldFlag = oldHelp.Flags.Values.FirstOrDefault(f => NormalizeFlag(f.Name) == name);
            var newFlag = newHelp.Flags.Values.FirstOrDefault(f => NormalizeFlag(f.Name) == name);

            if (oldFlag != null && newFlag != null)
            {
                bool changed = false;
                var cf = new ChangedFlag
                {
                    FlagName = newFlag.Name,
                    OldDescription = oldFlag.Description,
                    NewDescription = newFlag.Description,
                    OldDefault = oldFlag.DefaultValue,
                    NewDefault = newFlag.DefaultValue,
                };

                if (oldFlag.Description != newFlag.Description)
                    changed = true;
                if (oldFlag.DefaultValue != newFlag.DefaultValue)
                    changed = true;

                if (changed)
                    changedFlags.Add(cf);
            }
        }

        // Critical changes: removed flags that we use
        var appFlagSet = new HashSet<string>(AllAppFlags.Select(NormalizeFlag), StringComparer.OrdinalIgnoreCase);
        var criticalChanges = new List<CriticalChange>();

        foreach (var removedName in removedNames)
        {
            if (appFlagSet.Contains(removedName))
            {
                criticalChanges.Add(new CriticalChange
                {
                    FlagName = removedName,
                    OldVersion = oldVersion,
                    NewVersion = newVersion,
                    SuggestedReplacement = FindSuggestionInNew(removedName, newHelp),
                });
            }
        }

        var report = new CliChangeReport
        {
            OldVersion = oldVersion,
            NewVersion = newVersion,
            AddedFlags = addedFlags,
            RemovedFlags = removedFlags,
            ModifiedFlags = changedFlags,
            CriticalChanges = criticalChanges,
        };

        if (report.HasCriticalChanges)
        {
            _log.Error($"CRITICAL: {criticalChanges.Count} used flags removed in {newVersion}", "CliValidator");
            foreach (var cc in criticalChanges)
            {
                var msg = cc.SuggestedReplacement != null
                    ? $"  {cc.FlagName} → use: {cc.SuggestedReplacement}"
                    : $"  {cc.FlagName} — removed without replacement";
                _log.Error(msg, "CliValidator");
            }
        }

        return Task.FromResult(report);
    }

    /// <summary>
    /// Save a help snapshot for a version (for future comparison).
    /// </summary>
    public Task SaveSnapshotAsync(LlamaHelpInfo helpInfo, string version)
    {
        try
        {
            var snapshotDir = GetSnapshotsDirectory();
            var safeVersion = version.Replace("/", "_").Replace("\\", "_");
            var path = Path.Combine(snapshotDir, $"{safeVersion}.json");

            var snapshot = CliFlagSnapshot.FromHelpInfo(helpInfo, version);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            _log.Information($"Saved CLI snapshot: {version} ({helpInfo.Flags.Count} flags)", "CliValidator");
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to save CLI snapshot for {version}: {ex.Message}", "CliValidator");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Load a previously saved help snapshot.
    /// </summary>
    public async Task<LlamaHelpInfo?> LoadSnapshotAsync(string version)
    {
        try
        {
            var snapshotDir = GetSnapshotsDirectory();
            var safeVersion = version.Replace("/", "_").Replace("\\", "_");
            var path = Path.Combine(snapshotDir, $"{safeVersion}.json");

            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path);
            var snapshot = JsonSerializer.Deserialize<CliFlagSnapshot>(json);

            if (snapshot == null)
                return null;

            _log.Information($"Loaded CLI snapshot: {version} ({snapshot.Flags.Count} flags)", "CliValidator");
            return snapshot.ToHelpInfo();
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to load CLI snapshot for {version}: {ex.Message}", "CliValidator");
            return null;
        }
    }

    /// <summary>
    /// Load the latest available snapshot.
    /// </summary>
    public async Task<LlamaHelpInfo?> LoadLatestSnapshotAsync()
    {
        try
        {
            var snapshotDir = GetSnapshotsDirectory();
            if (!Directory.Exists(snapshotDir))
                return null;

            var files = Directory.GetFiles(snapshotDir, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();

            if (files == null)
                return null;

            var json = await File.ReadAllTextAsync(files);
            var snapshot = JsonSerializer.Deserialize<CliFlagSnapshot>(json);

            if (snapshot == null)
                return null;

            _log.Information($"Loaded latest CLI snapshot: {snapshot.Version}", "CliValidator");
            return snapshot.ToHelpInfo();
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to load latest CLI snapshot: {ex.Message}", "CliValidator");
            return null;
        }
    }

    static string NormalizeFlag(string flag)
    {
        return flag.TrimStart('-').ToLowerInvariant();
    }

    /// <summary>
    /// Try to find a replacement for a removed flag by looking for similar names.
    /// </summary>
    static string? FindSuggestion(string removedFlag, LlamaHelpInfo helpInfo)
    {
        var normalized = NormalizeFlag(removedFlag);

        // Known replacements (historical changes in llama.cpp)
        var knownReplacements = new Dictionary<string, string[]>
        {
            { "draft-mtp", ["--spec-type"] },
            { "flash-attn-new", ["-fa", "--flash-attn"] },
            { "cfa", ["-fa", "--flash-attn"] },
            { "n-gpu-layers", ["-ngl", "--gpu-layers"] },
            { "model-draft", ["-md", "--spec-draft-model"] },
            { "draft-model", ["-md", "--spec-draft-model"] },
            { "max-tokens", ["-n", "--predict"] },
            { "no-mmq", [] }, // removed without replacement
            { "mirostat-tau", ["--mirostat-ent"] },
            { "mirostat-learn-rate", ["--mirostat-lr"] },
            { "mirostat-eta", ["--mirostat-lr"] },
            { "rope-frequency-base", ["--rope-freq-base"] },
            { "rope-frequency-scale", ["--rope-freq-scale"] },
            { "n-predict", ["-n", "--predict"] },
        };

        var key = normalized.Replace("-", "").Replace("_", "");
        foreach (var (oldName, replacements) in knownReplacements)
        {
            var oldKey = oldName.Replace("-", "").Replace("_", "");
            if (key == oldKey || key.Contains(oldKey) || oldKey.Contains(key))
            {
                foreach (var replacement in replacements)
                {
                    if (helpInfo.IsSupported(replacement))
                        return replacement;
                }
            }
        }

        // Fuzzy: find flags with similar base name
        var bestMatch = helpInfo.Flags.Values
            .Where(f => !AllAppFlags.Contains(f.Name))
            .OrderBy(f => LevenshteinDistance(normalized, NormalizeFlag(f.Name)))
            .FirstOrDefault(f => LevenshteinDistance(normalized, NormalizeFlag(f.Name)) <= 3);

        return bestMatch?.Name;
    }

    static string? FindSuggestionInNew(string removedFlag, LlamaHelpInfo newHelp)
    {
        return FindSuggestion(removedFlag, newHelp);
    }

    /// <summary>
    /// Simple Levenshtein distance for fuzzy matching.
    /// </summary>
    static int LevenshteinDistance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}
