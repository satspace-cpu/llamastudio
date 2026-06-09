using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

/// <summary>
/// Parses llama-server --help output to detect supported CLI arguments.
/// </summary>
public interface IHelpParser
{
    /// <summary>
    /// Parse help output from llama-server executable.
    /// Returns null if executable not found or parsing fails.
    /// </summary>
    Task<LlamaHelpInfo?> ParseAsync(string serverExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific flag is supported by the current build.
    /// </summary>
    bool IsFlagSupported(LlamaHelpInfo? helpInfo, string flag);
}
