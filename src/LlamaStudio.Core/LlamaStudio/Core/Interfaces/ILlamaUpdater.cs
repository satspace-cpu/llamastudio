using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface ILlamaUpdater
{
	event EventHandler<double>? DownloadProgress;

	event EventHandler<string>? StatusMessage;

	event EventHandler<CliChangeReport>? CliChangesDetected;

	Task<List<LlamaCppRelease>> FetchReleasesAsync(bool includePrerelease = false, CancellationToken cancellationToken = default(CancellationToken));

	Task<List<LlamaCppRelease>> FetchReleasesFromRepoAsync(string ownerRepo, CancellationToken cancellationToken = default(CancellationToken));

	Task<LlamaCppRelease?> GetLatestReleaseAsync(bool includePrerelease = false, CancellationToken cancellationToken = default(CancellationToken));

	Task<string> DownloadAndExtractAsync(string version, string targetDirectory, CancellationToken cancellationToken = default(CancellationToken));

	Task<string> DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken cancellationToken = default(CancellationToken), bool ncclEnabled = false);

	Task<List<string>> GetInstalledVersionsAsync(string installDirectory);

	bool IsVersionInstalledAsync(string installDirectory, string version);

	Task UninstallVersionAsync(string installDirectory, string version);
}
