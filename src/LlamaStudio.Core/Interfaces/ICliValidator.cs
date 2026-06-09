using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

/// <summary>
/// Validates application CLI flags against llama.cpp binary and compares versions.
/// </summary>
public interface ICliValidator
{
    /// <summary>
    /// Validate all flags our app can generate against the current binary.
    /// Returns a report with removed, new, and changed flags.
    /// </summary>
    Task<ValidationReport> ValidateAllFlagsAsync(string serverExePath, string version);

    /// <summary>
    /// Compare two versions' help output to detect CLI changes.
    /// </summary>
    Task<CliChangeReport> CompareVersionsAsync(LlamaHelpInfo oldHelp, LlamaHelpInfo newHelp, string oldVersion, string newVersion);

    /// <summary>
    /// Save a help snapshot for a version (for future comparison).
    /// </summary>
    Task SaveSnapshotAsync(LlamaHelpInfo helpInfo, string version);

    /// <summary>
    /// Load a previously saved help snapshot.
    /// </summary>
    Task<LlamaHelpInfo?> LoadSnapshotAsync(string version);

    /// <summary>
    /// Load the latest available snapshot (most recently written).
    /// </summary>
    Task<LlamaHelpInfo?> LoadLatestSnapshotAsync();

    /// <summary>
    /// Get the directory where CLI snapshots are stored.
    /// </summary>
    string GetSnapshotsDirectory();
}
