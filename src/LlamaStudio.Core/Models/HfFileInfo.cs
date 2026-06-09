using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LlamaStudio.Core.Models;

/// <summary>
/// Represents a file from a HuggingFace repository with download state.
/// </summary>
public partial class HfFileInfo : ObservableObject
{
    public string RepoId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
    public long Size { get; set; }
    public string SizeDisplay => FormatSize(Size);
    public string Sha256 { get; set; } = string.Empty;
    public string DownloadUrl => $"https://huggingface.co/{RepoId}/resolve/main/{Path}";

    [ObservableProperty] bool _isSelected;
    [ObservableProperty] bool _isDownloading;
    [ObservableProperty] double _downloadProgress;
    [ObservableProperty] string _status = "Ready";
    [ObservableProperty] bool _isCompleted;
    [ObservableProperty] string _localPath = string.Empty;
    [ObservableProperty] bool _showDetails;
    [ObservableProperty] bool _isMetadataLoading;
    [ObservableProperty] ObservableCollection<KeyValuePair<string, string>> _metadata = new();

    public string ProgressText => IsDownloading ? $"{DownloadProgress:F0}%" : (IsCompleted ? "Done" : string.Empty);
    public string StatusIcon => IsCompleted ? "✅" : (IsDownloading ? "⬇️" : string.Empty);
    public string MetadataLoading => "Loading metadata...";

    static string FormatSize(long bytes)
    {
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024:N0} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):N1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):N2} GB";
    }
}
