namespace LlamaStudio.Core.Models;

/// <summary>
/// Result of validating application's CLI flags against a specific llama.cpp binary.
/// </summary>
public class ValidationReport
{
    public DateTime Timestamp { get; set; }
    public string ServerExePath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Flags our app generates that are NOT found in the current binary.
    /// </summary>
    public List<RemovedFlag> RemovedFlags { get; set; } = new();

    /// <summary>
    /// Flags present in the binary that we don't use yet (potentially new features).
    /// </summary>
    public List<CliFlagInfo> NewAvailableFlags { get; set; } = new();

    /// <summary>
    /// Flags we use that exist but have changed description or default value.
    /// </summary>
    public List<ChangedFlag> ChangedFlags { get; set; } = new();

    public ValidationStatus Status { get; set; }

    public bool HasIssues => RemovedFlags.Count > 0 || ChangedFlags.Count > 0;
}
