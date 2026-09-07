using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IHuggingFaceDownloader
{
	event EventHandler<double>? DownloadProgress;

	event EventHandler<string>? StatusMessage;

	event EventHandler<HfFileInfo>? FileDownloaded;

	event EventHandler? AllDownloadsCompleted;

	void SetAuthToken(string? token);

	string? GetAuthToken();

	Task<bool> LoginViaBrowserAsync();

	Task<List<HfFileInfo>> ListRepoFilesAsync(string urlOrRepoId, CancellationToken cancellationToken = default(CancellationToken));

	Task DownloadSelectedAsync(List<HfFileInfo> files, string targetDirectory, CancellationToken cancellationToken = default(CancellationToken));

	void PauseDownloads();

	void CancelDownloads();

	Task<HfModelInfo?> ResolveRepoAsync(string repo, CancellationToken cancellationToken = default(CancellationToken));

	Task<string> DownloadModelAsync(string repo, bool offlineMode = false, CancellationToken cancellationToken = default(CancellationToken));

	string GetCacheDirectory();

	Task LoadFileMetadataAsync(HfFileInfo file, CancellationToken cancellationToken = default(CancellationToken));

	Task<List<HfSearchResult>> SearchModelsAsync(string query, CancellationToken cancellationToken = default(CancellationToken));
}
