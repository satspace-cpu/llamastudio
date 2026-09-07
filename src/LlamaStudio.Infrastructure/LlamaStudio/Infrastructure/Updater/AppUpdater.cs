using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Infrastructure.Updater;

public class AppUpdater : IAppUpdater
{
	private class LatestVersionInfo
	{
		[JsonPropertyName("version")]
		public string Version { get; set; } = string.Empty;

		[JsonPropertyName("downloadUrl")]
		public string DownloadUrl { get; set; } = string.Empty;

		[JsonPropertyName("changelog")]
		public string? Changelog { get; set; }

		[JsonPropertyName("publishedAt")]
		public DateTime PublishedAt { get; set; }

		[JsonPropertyName("downloadUrlLinux")]
		public string? DownloadUrlLinux { get; set; }
	}

	private readonly HttpClient _httpClient;

	private readonly ILogService _log;

	public event EventHandler<AppUpdateInfo>? UpdateAvailable;

	public event EventHandler<string>? StatusChanged;

	public event EventHandler<double>? ProgressChanged;

	public AppUpdater(HttpClient httpClient, ILogService log)
	{
		_httpClient = httpClient;
		_log = log;
		if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
		{
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "LlamaStudio-Updater");
		}
	}

	public string GetCurrentVersion()
	{
		Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
		string text = assembly.GetName().Version?.ToString(3) ?? "0.0.0";
		_log.Information($"GetCurrentVersion: {text} (from: {assembly.Location})", "AppUpdater");
		return text;
	}

	public async Task<AppUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			OnStatus("Checking for updates...");
			_log.Information("Checking for LlamaStudio updates...", "AppUpdater");
			string requestUri = "https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/latest.json";
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
			httpRequestMessage.Headers.Add("User-Agent", "LlamaStudio-Updater/1.0");
			HttpResponseMessage obj = await _httpClient.SendAsync(httpRequestMessage, ct);
			obj.EnsureSuccessStatusCode();
			LatestVersionInfo latestVersionInfo = JsonSerializer.Deserialize<LatestVersionInfo>(await obj.Content.ReadAsStringAsync(ct));
			if (latestVersionInfo == null)
			{
				_log.Warning("Failed to fetch latest version info", "AppUpdater");
				return null;
			}
			string currentVersion = GetCurrentVersion();
			_log.Information("Comparing current version: " + currentVersion + " with latest: " + latestVersionInfo.Version, "AppUpdater");
			if (IsNewerVersion(latestVersionInfo.Version, currentVersion))
			{
				AppUpdateInfo appUpdateInfo = new AppUpdateInfo(latestVersionInfo.Version, latestVersionInfo.DownloadUrl, latestVersionInfo.Changelog ?? "", latestVersionInfo.PublishedAt, IsPrerelease: false, DownloadUrlLinux: latestVersionInfo.DownloadUrlLinux ?? string.Empty);
				_log.Information("Update available: " + latestVersionInfo.Version, "AppUpdater");
				this.UpdateAvailable?.Invoke(this, appUpdateInfo);
				return appUpdateInfo;
			}
			_log.Information("No newer updates available (current: " + currentVersion + ")", "AppUpdater");
			return null;
		}
		catch (Exception ex)
		{
			_log.Error("Failed to check for updates: " + ex.Message, "AppUpdater");
			OnStatus("Error checking for updates: " + ex.Message);
			return null;
		}
	}

	public async Task<string> DownloadUpdateAsync(AppUpdateInfo info, string downloadPath, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			OnStatus("Downloading version " + info.Version + "...");
			_log.Information("Downloading update " + info.Version + "...", "AppUpdater");
			// r87: per-OS artifact — single-file EXE on Windows, tar.gz on Linux.
			bool isLinux = OperatingSystem.IsLinux();
			string fileName = isLinux ? "LlamaStudio-" + info.Version + "-linux-x64.tar.gz" : "LlamaStudio.exe";
			string fullPath = Path.Combine(downloadPath, fileName);
			using (HttpResponseMessage response = await _httpClient.GetAsync(info.DownloadUrlForCurrentOS, HttpCompletionOption.ResponseHeadersRead, ct))
			{
				response.EnsureSuccessStatusCode();
				long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
				string result;
				await using (Stream stream = await response.Content.ReadAsStreamAsync(ct))
				{
					string text;
					await using (FileStream file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, FileOptions.WriteThrough))
					{
						byte[] buffer = new byte[1048576];
						long downloaded = 0L;
						DateTime lastCallbackTime = DateTime.UtcNow;
						while (true)
						{
							int read = await stream.ReadAsync(buffer, ct);
							if (read == 0)
							{
								break;
							}
							await file.WriteAsync(buffer.AsMemory(0, read), ct);
							downloaded += read;
							DateTime utcNow = DateTime.UtcNow;
							if (downloaded - (long)((double)downloaded * 0.01) >= 10485760 || (utcNow - lastCallbackTime).TotalMilliseconds >= 200.0)
							{
								double value = ((totalBytes > 0) ? ((double)downloaded / (double)totalBytes * 100.0) : 0.0);
								OnProgress(value);
								lastCallbackTime = utcNow;
							}
						}
						OnProgress(100.0);
						OnStatus("Download complete: " + fileName);
						_log.Information("Update downloaded to " + fullPath, "AppUpdater");
						text = fullPath;
					}
					result = text;
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			_log.Error("Failed to download update: " + ex.Message, "AppUpdater");
			OnStatus("Download failed: " + ex.Message);
			throw;
		}
	}

	private static bool IsNewerVersion(string newVersion, string currentVersion)
	{
		if (Version.TryParse(newVersion, out Version result) && Version.TryParse(currentVersion, out Version result2))
		{
			return result > result2;
		}
		return string.Compare(newVersion, currentVersion, StringComparison.Ordinal) > 0;
	}

	private void OnStatus(string message)
	{
		this.StatusChanged?.Invoke(this, message);
	}

	private void OnProgress(double value)
	{
		this.ProgressChanged?.Invoke(this, value);
	}
}
