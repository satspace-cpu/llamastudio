using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

/// <summary>
/// Downloads models from Hugging Face Hub with progress reporting.
/// </summary>
public interface IHuggingFaceDownloader
{
    /// <summary>
    /// Raised during download with percentage (0-100).
    /// </summary>
    event EventHandler<double>? DownloadProgress;

    /// <summary>
    /// Raised with status messages (e.g., "Resolving...", "Downloading...", "Extracting...").
    /// </summary>
    event EventHandler<string>? StatusMessage;

    /// <summary>
    /// Raised when a single file download completes.
    /// </summary>
    event EventHandler<HfFileInfo>? FileDownloaded;

    /// <summary>
    /// Raised when all selected files are downloaded.
    /// </summary>
    event EventHandler? AllDownloadsCompleted;

    /// <summary>
    /// Set HuggingFace auth token for private repos.
    /// </summary>
    void SetAuthToken(string? token);

    /// <summary>
    /// Get current auth token.
    /// </summary>
    string? GetAuthToken();

    /// <summary>
    /// Open browser for HuggingFace login. Returns true if user logged in successfully.
    /// </summary>
    Task<bool> LoginViaBrowserAsync();

    /// <summary>
    /// Resolve a HuggingFace URL or repo ID to get list of downloadable .gguf files.
    /// </summary>
    Task<List<HfFileInfo>> ListRepoFilesAsync(string urlOrRepoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download selected files to the target directory.
    /// </summary>
    Task DownloadSelectedAsync(List<HfFileInfo> files, string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel ongoing downloads.
    /// </summary>
    void CancelDownloads();

    /// <summary>
    /// Resolve a HuggingFace repo to get model file info (legacy).
    /// </summary>
    Task<HfModelInfo?> ResolveRepoAsync(string repo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a model from HuggingFace Hub (legacy).
    /// </summary>
    Task<string> DownloadModelAsync(string repo, bool offlineMode = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the local cache directory for HuggingFace models.
    /// </summary>
    string GetCacheDirectory();

    /// <summary>
    /// Fetch GGUF metadata for a file by downloading its header.
    /// </summary>
    Task LoadFileMetadataAsync(HfFileInfo file, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a model file from HuggingFace Hub.
/// </summary>
public class HfModelInfo
{
    public string Repo { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeDisplay => FormatSize(Size);
    public string Sha256 { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public bool ExistsLocally { get; set; }

    static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 * 1024 => $"{bytes / 1024} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }
}
