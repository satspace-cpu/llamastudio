namespace LlamaStudio.Core.Interfaces;

public interface IAppUpdater
{
    event EventHandler<AppUpdateInfo>? UpdateAvailable;
    event EventHandler<string>? StatusChanged;
    event EventHandler<double>? ProgressChanged;

    /// <summary>
    /// Checks GitHub releases for a newer version of LlamaStudio.
    /// </summary>
    Task<AppUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Downloads and prepares the latest update for installation.
    /// </summary>
    Task<string> DownloadUpdateAsync(AppUpdateInfo info, string downloadPath, CancellationToken ct = default);
}

public record AppUpdateInfo(
    string Version,
    string DownloadUrl,
    string Changelog,
    DateTime PublishedAt,
    bool IsPrerelease
);
