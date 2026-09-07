using System;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaStudio.Core.Interfaces;

public interface IAppUpdater
{
	event EventHandler<AppUpdateInfo>? UpdateAvailable;

	event EventHandler<string>? StatusChanged;

	event EventHandler<double>? ProgressChanged;

	Task<AppUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default(CancellationToken));

	string GetCurrentVersion();

	Task<string> DownloadUpdateAsync(AppUpdateInfo info, string downloadPath, CancellationToken ct = default(CancellationToken));
}
