namespace LlamaStudio.Core.Models;

/// <summary>
/// A flag that was removed between versions and is actively used by our application.
/// </summary>
public class CriticalChange
{
    public string FlagName { get; set; } = string.Empty;
    public string OldVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;
    public string? SuggestedReplacement { get; set; }
}

/// <summary>
/// Report comparing CLI flags between two llama.cpp versions.
/// </summary>
public class CliChangeReport
{
    public string OldVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;

    /// <summary>
    /// Flags added in the new version (we don't use yet).
    /// </summary>
    public List<CliFlagInfo> AddedFlags { get; set; } = new();

    /// <summary>
    /// Flags removed in the new version.
    /// </summary>
    public List<string> RemovedFlags { get; set; } = new();

    /// <summary>
    /// Flags that exist in both versions but changed description or default.
    /// </summary>
    public List<ChangedFlag> ModifiedFlags { get; set; } = new();

    /// <summary>
    /// Critical: flags that were removed AND we actively use them.
    /// </summary>
    public List<CriticalChange> CriticalChanges { get; set; } = new();

    public bool HasCriticalChanges => CriticalChanges.Count > 0;
}
