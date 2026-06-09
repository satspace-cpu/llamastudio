using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface ILlamaUpdater
{
    event EventHandler<double>? DownloadProgress;
    event EventHandler<string>? StatusMessage;

    /// <summary>
    /// Raised after installing a new version when CLI changes are detected.
    /// </summary>
    event EventHandler<CliChangeReport>? CliChangesDetected;

    Task<List<LlamaCppRelease>> FetchReleasesAsync(bool includePrerelease = false, CancellationToken cancellationToken = default);
    Task<LlamaCppRelease?> GetLatestReleaseAsync(bool includePrerelease = false, CancellationToken cancellationToken = default);
    Task<string> DownloadAndExtractAsync(string version, string targetDirectory, CancellationToken cancellationToken = default);
    Task<string> DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken cancellationToken = default);
    Task<List<string>> GetInstalledVersionsAsync(string installDirectory);
    bool IsVersionInstalledAsync(string installDirectory, string version);
    Task UninstallVersionAsync(string installDirectory, string version);
}
