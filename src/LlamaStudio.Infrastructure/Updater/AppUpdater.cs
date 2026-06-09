using LlamaStudio.Core.Interfaces;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Updater;

public class AppUpdater : IAppUpdater
{
    readonly HttpClient _httpClient;
    readonly ILogService _log;

    public event EventHandler<AppUpdateInfo>? UpdateAvailable;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<double>? ProgressChanged;

    public AppUpdater(HttpClient httpClient, ILogService log)
    {
        _httpClient = httpClient;
        _log = log;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString(3) ?? "0.0.0";
    }

    public async Task<AppUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            OnStatus("Checking for updates...");
            _log.Information("Checking for LlamaStudio updates on GitHub...", "AppUpdater");

            var releases = await _httpClient.GetFromJsonAsync<List<GithubRelease>>(
                "https://api.github.com/repos/satspace-cpu/llamastudio/releases", ct);

            if (releases == null || releases.Count == 0)
            {
                _log.Warning("No releases found on GitHub", "AppUpdater");
                return null;
            }

            var currentVersion = GetCurrentVersion();
            _log.Information($"Current version: {currentVersion}", "AppUpdater");

            foreach (var release in releases)
            {
                var releaseVersion = release.TagName.TrimStart('v');
                if (IsNewerVersion(releaseVersion, currentVersion))
                {
                    var exeAsset = release.Assets?.FirstOrDefault(a => a.Name == "LlamaStudio.exe");
                    if (exeAsset == null)
                        continue;

                    var info = new AppUpdateInfo(
                        Version: releaseVersion,
                        DownloadUrl: exeAsset.BrowserDownloadUrl,
                        Changelog: release.Body ?? "",
                        PublishedAt: release.PublishedAt,
                        IsPrerelease: release.Prerelease
                    );

                    _log.Information($"Update available: {releaseVersion}", "AppUpdater");
                    UpdateAvailable?.Invoke(this, info);
                    return info;
                }
            }

            _log.Information("No updates available", "AppUpdater");
            return null;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to check for updates: {ex.Message}", "AppUpdater");
            OnStatus($"Error checking for updates: {ex.Message}");
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(AppUpdateInfo info, string downloadPath, CancellationToken ct = default)
    {
        try
        {
            OnStatus($"Downloading version {info.Version}...");
            _log.Information($"Downloading update {info.Version}...", "AppUpdater");

            var fileName = "LlamaStudio.exe";
            var fullPath = Path.Combine(downloadPath, fileName);

            using var response = await _httpClient.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, FileOptions.WriteThrough);

            var buffer = new byte[1048576];
            long downloaded = 0;
            var lastCallbackTime = DateTime.UtcNow;

            while (true)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read == 0)
                    break;

                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;

                var now = DateTime.UtcNow;
                if (downloaded - (long)(downloaded * 0.01) >= 10485760 || (now - lastCallbackTime).TotalMilliseconds >= 200)
                {
                    var progress = totalBytes > 0 ? (double)downloaded / totalBytes * 100 : 0;
                    OnProgress(progress);
                    lastCallbackTime = now;
                }
            }

            OnProgress(100);
            OnStatus($"Download complete: {fileName}");
            _log.Information($"Update downloaded to {fullPath}", "AppUpdater");

            return fullPath;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to download update: {ex.Message}", "AppUpdater");
            OnStatus($"Download failed: {ex.Message}");
            throw;
        }
    }

    static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        if (Version.TryParse(newVersion, out var newVer) && Version.TryParse(currentVersion, out var curVer))
        {
            return newVer > curVer;
        }

        return string.Compare(newVersion, currentVersion, StringComparison.Ordinal) > 0;
    }

    void OnStatus(string message) => StatusChanged?.Invoke(this, message);
    void OnProgress(double value) => ProgressChanged?.Invoke(this, value);

    class GithubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public List<GithubAsset>? Assets { get; set; }
    }

    class GithubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
