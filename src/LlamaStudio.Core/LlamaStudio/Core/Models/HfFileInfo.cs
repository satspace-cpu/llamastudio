using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LlamaStudio.Core.Models;

public class HfFileInfo : ObservableObject
{
	[ObservableProperty]
	private bool _isSelected;

	[ObservableProperty]
	private bool _isDownloading;

	[ObservableProperty]
	private double _downloadProgress;

	[ObservableProperty]
	private string _status = "Ready";

	[ObservableProperty]
	private bool _isCompleted;

	[ObservableProperty]
	private string _localPath = string.Empty;

	[ObservableProperty]
	private bool _showDetails;

	[ObservableProperty]
	private bool _isMetadataLoading;

	[ObservableProperty]
	private ObservableCollection<KeyValuePair<string, string>> _metadata = new ObservableCollection<KeyValuePair<string, string>>();

	public bool IsSelected
	{
		get => _isSelected;
		set => SetProperty(ref _isSelected, value);
	}

	public bool IsDownloading
	{
		get => _isDownloading;
		set => SetProperty(ref _isDownloading, value);
	}

	public double DownloadProgress
	{
		get => _downloadProgress;
		set => SetProperty(ref _downloadProgress, value);
	}

	public string Status
	{
		get => _status;
		set => SetProperty(ref _status, value);
	}

	public bool IsCompleted
	{
		get => _isCompleted;
		set => SetProperty(ref _isCompleted, value);
	}

	public string LocalPath
	{
		get => _localPath;
		set => SetProperty(ref _localPath, value);
	}

	public bool ShowDetails
	{
		get => _showDetails;
		set => SetProperty(ref _showDetails, value);
	}

	public bool IsMetadataLoading
	{
		get => _isMetadataLoading;
		set => SetProperty(ref _isMetadataLoading, value);
	}

	public ObservableCollection<KeyValuePair<string, string>> Metadata
	{
		get => _metadata;
		set => SetProperty(ref _metadata, value);
	}

	public string RepoId { get; set; } = string.Empty;

	public string Path { get; set; } = string.Empty;

	public string FileName => System.IO.Path.GetFileName(Path);

	public long Size { get; set; }

	public string SizeDisplay => FormatSize(Size);

	public string Sha256 { get; set; } = string.Empty;

	public string DownloadUrl => "https://huggingface.co/" + RepoId + "/resolve/main/" + Path;

	public string ProgressText
	{
		get
		{
			if (!IsDownloading)
			{
				if (!IsCompleted)
				{
					return string.Empty;
				}
				return "Done";
			}
			return $"{DownloadProgress:F0}%";
		}
	}

	public string StatusIcon
	{
		get
		{
			if (!IsCompleted)
			{
				if (!IsDownloading)
				{
					return string.Empty;
				}
				return "⬇️";
			}
			return "✅";
		}
	}

	public string MetadataLoading => "Loading metadata...";

	private static string FormatSize(long bytes)
	{
		if (bytes >= 1048576)
		{
			if (bytes >= 1073741824)
			{
				return $"{(double)bytes / 1073741824.0:N2} GB";
			}
			return $"{(double)bytes / 1048576.0:N1} MB";
		}
		return $"{bytes / 1024:N0} KB";
	}
}
