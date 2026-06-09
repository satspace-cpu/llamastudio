using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Updater;

public class LlamaUpdater : ILlamaUpdater
{
    readonly HttpClient _httpClient;
    readonly ILogService _log;
    readonly ISettings _settings;
    readonly ICliValidator _cliValidator;
    readonly IHelpParser _helpParser;

    public event EventHandler<double>? DownloadProgress;
    public event EventHandler<string>? StatusMessage;
    public event EventHandler<CliChangeReport>? CliChangesDetected;

    static string DefaultDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");

    public LlamaUpdater(HttpClient httpClient, ILogService log, ISettings settings, ICliValidator cliValidator, IHelpParser helpParser)
    {
        _httpClient = httpClient;
        _log = log;
        _settings = settings;
        _cliValidator = cliValidator;
        _helpParser = helpParser;
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    async Task<List<LlamaCppRelease>> ILlamaUpdater.FetchReleasesAsync(bool includePrerelease, CancellationToken ct)
    {
        _log.Information("Fetching llama.cpp releases from ggml-org/llama.cpp...", "Updater");

        var releases = await _httpClient.GetFromJsonAsync<List<GithubRelease>>(
            "https://api.github.com/repos/ggml-org/llama.cpp/releases?per_page=30", ct);

        if (releases == null || releases.Count == 0)
            return new();

        var result = new List<LlamaCppRelease>();

        foreach (var rel in releases)
        {
            if (!includePrerelease && rel.Prerelease)
                continue;

            var windowsAssets = FilterWindowsAssets(rel.Assets);

            if (windowsAssets.Count == 0)
                continue;

            result.Add(new LlamaCppRelease
            {
                TagName = rel.TagName,
                Name = rel.Name,
                HtmlUrl = rel.HtmlUrl,
                PublishedAt = rel.PublishedAt,
                IsPrerelease = rel.Prerelease,
                Body = rel.Body,
                Assets = windowsAssets
            });

            if (result.Count >= 30)
                break;
        }

        _log.Information($"Found {result.Count} releases with Windows assets", "Updater");
        return result;
    }

    List<ReleaseAsset> FilterWindowsAssets(List<GithubAsset>? assets)
    {
        if (assets == null)
            return new();

        var result = new List<ReleaseAsset>();

        foreach (var a in assets)
        {
            var name = a.Name.ToLowerInvariant();

            // Skip cudart-llama-bin archives — they contain ONLY CUDA DLL, not full binaries
            if (name.StartsWith("cudart-"))
                continue;

            if (!name.Contains("-win-") || !name.EndsWith(".zip"))
                continue;

            var asset = ReleaseAsset.ParseAsset(a.Name, a.BrowserDownloadUrl, a.Size, a.UpdatedAt);
            result.Add(asset);
        }

        return result;
    }

    async Task<LlamaCppRelease?> ILlamaUpdater.GetLatestReleaseAsync(bool includePrerelease, CancellationToken ct)
    {
        var releases = await ((ILlamaUpdater)this).FetchReleasesAsync(includePrerelease, ct);
        return releases.FirstOrDefault();
    }

  /// <summary>
    /// Downloads the main archive + CUDA runtime (for CUDA builds), extracts both to one folder.
    /// </summary>
    async Task<string> DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken ct)
    {
        var buildSuffix = asset.BuildType.ToString().ToLowerInvariant().Replace(" ", "");
        var folderName = $"{version}-{buildSuffix}";
        var installDir = Path.Combine(targetDirectory, folderName);
        if (!Directory.Exists(installDir))
            Directory.CreateDirectory(installDir);

        // Collect all archives to download: main + CUDA runtime (for CUDA builds)
        var archivesToDownload = new List<(string Name, string Url, long Size)>
        {
            (asset.Name, asset.BrowserDownloadUrl, asset.Size)
        };

        // For CUDA builds, find matching CUDA runtime archive from the same release
        if (asset.BuildType == BuildType.Cuda12x || asset.BuildType == BuildType.Cuda13x)
        {
            var cudaRuntime = await FindCudaRuntimeArchiveAsync(version, asset);
            if (cudaRuntime != null)
            {
                archivesToDownload.Add((cudaRuntime.Name, cudaRuntime.BrowserDownloadUrl, cudaRuntime.Size));
            }
        }

        // Calculate total size for combined progress
        var totalSize = archivesToDownload.Sum(a => a.Size);
        object lockObj = new();

         // Download all archives to temp files
        var tempFiles = new List<string>();
        long[] fileDownloaded = new long[archivesToDownload.Count];
        try
        {
            for (int i = 0; i < archivesToDownload.Count; i++)
            {
                var (name, url, size) = archivesToDownload[i];
                var progress = archivesToDownload.Count > 1 ? $"{i + 1}/{archivesToDownload.Count}" : "";
                OnStatus($"Downloading {progress} {name} ({FormatSize(size)})...");
                _log.Information($"Downloading: {name}", "Updater");

                var tempFile = Path.Combine(Path.GetTempPath(), $"llama_studio_{Guid.NewGuid():N}.zip");
                tempFiles.Add(tempFile);

                await DownloadToFileAsync(url, tempFile, (total, downloaded) =>
                {
                    lock (lockObj)
                    {
                        fileDownloaded[i] = downloaded;
                        var totalDl = fileDownloaded.Sum();
                        var overallProgress = totalSize > 0 ? (double)totalDl / totalSize * 100 : 0;
                        OnProgress(Math.Min(overallProgress, 100));
                    }
                }, ct);
            }

            // Extract all archives to the same folder
            OnStatus("Extracting archives...");
            _log.Information("Extracting archives...", "Updater");

            foreach (var tempFile in tempFiles)
            {
                ExtractZipFlattened(tempFile, installDir);
            }

            // Validate installation
            var hasExe = File.Exists(Path.Combine(installDir, "llama-server.exe"));
            if (!hasExe)
            {
                throw new InvalidOperationException("llama-server.exe not found after extraction!");
            }

            // Log installed files
            var files = Directory.GetFiles(installDir, "*.*", SearchOption.TopDirectoryOnly);
            _log.Information($"Installed files: [{string.Join(", ", files.Select(f => Path.GetFileName(f)))}]", "Updater");

            // Save version info
            var infoPath = Path.Combine(installDir, "version_info.json");
            var info = new
            {
                Version = version,
                BuildType = asset.BuildType.ToString(),
                Description = asset.Description,
                AssetName = asset.Name,
                CudaRuntimeName = archivesToDownload.Count > 1 ? archivesToDownload[1].Name : null,
                InstalledAt = DateTime.UtcNow.ToString("o")
            };
            try
            {
                File.WriteAllText(infoPath, System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }

            OnStatus("Download complete!");
            _log.Information($"Installed llama.cpp {version} ({asset.Description}) to {installDir}", "Updater");

            // Run CLI validation against the newly installed binary
            var newExePath = Path.Combine(installDir, "llama-server.exe");
            _ = ValidateCliAfterInstallAsync(newExePath, version);

            return installDir;
        }
        finally
        {
            // Clean up temp files
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Finds the CUDA runtime archive for a given release and build type.
    /// Searches the release assets on GitHub for cudart-llama-bin-* archives matching the CUDA version.
    /// </summary>
    async Task<GithubAsset?> FindCudaRuntimeArchiveAsync(string version, ReleaseAsset mainAsset)
    {
        var mainName = mainAsset.Name.ToLowerInvariant();
        string cudaMajor;

        if (mainName.Contains("cuda-13") || mainName.Contains("cuda13"))
            cudaMajor = "13";
        else if (mainName.Contains("cuda-12"))
            cudaMajor = "12";
        else
            return null;

        try
        {
            var release = await _httpClient.GetFromJsonAsync<GithubRelease>(
                $"https://api.github.com/repos/ggml-org/llama.cpp/releases/tags/{version}",
                CancellationToken.None).ConfigureAwait(false);

            var allAssets = release?.Assets;
            if (allAssets == null || allAssets.Count == 0)
            {
                _log.Warning($"No assets found for release {version}", "Updater");
                return null;
            }

            _log.Information($"Release {version} has {allAssets.Count} assets: [{string.Join(", ", allAssets.Select(a => a.Name))}]", "Updater");

            // Priority 1: cudart-llama-bin-* archives (new format)
            var cudartLlamaBin = allAssets.FirstOrDefault(a =>
                a.Name.ToLowerInvariant().Contains("cudart-llama-bin") &&
                a.Name.ToLowerInvariant().Contains($"cuda-{cudaMajor}"));

            if (cudartLlamaBin != null)
            {
                _log.Information($"Found CUDA runtime (new format): {cudartLlamaBin.Name}", "Updater");
                return cudartLlamaBin;
            }

            // Priority 2: cudart-* archives matching CUDA version
            var cudaAsset = allAssets.FirstOrDefault(a =>
                a.Name.ToLowerInvariant().StartsWith("cudart-") &&
                a.Name.EndsWith(".zip") &&
                (a.Name.ToLowerInvariant().Contains($"-v{cudaMajor}.") ||
                 a.Name.ToLowerInvariant().Contains($"-{cudaMajor}.")));

            if (cudaAsset != null)
            {
                _log.Information($"Found CUDA runtime: {cudaAsset.Name}", "Updater");
                return cudaAsset;
            }

            // Priority 3: any cudart archive
            var anyCudart = allAssets.FirstOrDefault(a =>
                a.Name.ToLowerInvariant().StartsWith("cudart-") &&
                a.Name.EndsWith(".zip"));

            if (anyCudart != null)
            {
                _log.Warning($"No exact CUDA {cudaMajor} match, using: {anyCudart.Name}", "Updater");
                return anyCudart;
            }

            _log.Warning($"No CUDA runtime archive found for release {version}", "Updater");
            return null;
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to find CUDA runtime archive: {ex.Message}", "Updater");
            return null;
        }
    }

    async Task DownloadToFileAsync(string url, string filePath, Action<long, long> totalBytesCallback, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        // Large buffer for high-speed downloads + buffered file stream
        await using var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, FileOptions.WriteThrough);

        var buffer = new byte[1048576]; // 1 MB buffer
        long downloaded = 0;
        long lastCallback = 0;
        var lastCallbackTime = DateTime.UtcNow;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0)
                break;

            await file.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            downloaded += read;

            // Throttle callback: at least every 10 MB or 200 ms to avoid UI spam
            var now = DateTime.UtcNow;
            if (downloaded - lastCallback >= 10485760 || (now - lastCallbackTime).TotalMilliseconds >= 200)
            {
                totalBytesCallback(totalBytes, downloaded);
                lastCallback = downloaded;
                lastCallbackTime = now;
            }
        }

        // Final callback
        if (downloaded != lastCallback)
            totalBytesCallback(totalBytes, downloaded);
    }

    void ExtractZipFlattened(string archivePath, string targetDir)
    {
        using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

        // Find llama-server.exe to determine base directory
        string? serverEntry = null;
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals("llama-server.exe", StringComparison.OrdinalIgnoreCase))
            {
                serverEntry = entry.FullName;
                break;
            }
        }

        if (serverEntry != null)
        {
            // Extract main archive files (exe + DLLs from the same directory)
            var baseDir = Path.GetDirectoryName(serverEntry);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.FullName))
                    continue;

                string targetPath;
                if (!string.IsNullOrEmpty(baseDir) && entry.FullName.StartsWith(baseDir + "/", StringComparison.Ordinal))
                {
                    var relative = entry.FullName.Substring(baseDir.Length + 1);
                    targetPath = Path.Combine(targetDir, relative);
                }
                else
                {
                    targetPath = Path.Combine(targetDir, entry.Name);
                }

                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!string.IsNullOrEmpty(entry.Name))
                    entry.ExtractToFile(targetPath, true);
            }
        }
        else
        {
            // For CUDA runtime archives (no server.exe), extract all DLLs to root
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var targetPath = Path.Combine(targetDir, entry.Name);
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                entry.ExtractToFile(targetPath, true);
            }
        }
    }

    async Task Download7zExtractorAsync(string targetPath)
    {
        using var response = await _httpClient.GetAsync("https://www.7-zip.org/a/7zr.exe");
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(file);
    }

    void Extract7z(string archivePath, string targetDir)
    {
        var tempExe = Path.Combine(Path.GetTempPath(), "llama_studio_7z.exe");

        if (!File.Exists(tempExe))
        {
            var downloadTask = Download7zExtractorAsync(tempExe);
            downloadTask.Wait();
        }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempExe,
            Arguments = $"x \"{archivePath}\" -o\"{targetDir}\" -y",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        proc?.WaitForExit();
    }

    Task<List<string>> ILlamaUpdater.GetInstalledVersionsAsync(string installDirectory)
    {
        if (!Directory.Exists(installDirectory))
            return Task.FromResult(new List<string>());

        var dirs = Directory.GetDirectories(installDirectory)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n.StartsWith("b") || n.StartsWith("v") || n.StartsWith("B") || n.StartsWith("V"))
            .OrderByDescending(n => n)
            .ToList();

        return Task.FromResult(dirs!);
    }

    bool ILlamaUpdater.IsVersionInstalledAsync(string installDirectory, string version)
    {
        var path = Path.Combine(installDirectory, version);
        return Directory.Exists(path) && File.Exists(Path.Combine(path, "llama-server.exe"));
    }

    Task ILlamaUpdater.UninstallVersionAsync(string installDirectory, string version)
    {
        var path = Path.Combine(installDirectory, version);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            _log.Information($"Uninstalled version {version}", "Updater");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// After installing a new version: validate CLI flags and compare with previous snapshot.
    /// </summary>
    async Task ValidateCliAfterInstallAsync(string newExePath, string newVersion)
    {
        try
        {
            _log.Information($"Running post-install CLI validation for {newVersion}...", "Updater");

            // Parse help for the new binary
            var newHelp = await _helpParser.ParseAsync(newExePath);
            if (newHelp == null)
            {
                _log.Warning("Post-install validation: failed to parse --help for new version", "Updater");
                return;
            }

            // Save snapshot for the new version
            await _cliValidator.SaveSnapshotAsync(newHelp, newVersion);

            // Try to load previous snapshot for comparison
            var oldHelp = await _cliValidator.LoadLatestSnapshotAsync();

            if (oldHelp != null)
            {
                // Find the old version from the snapshot filename
                var snapshotsDir = _cliValidator.GetSnapshotsDirectory();
                var snapshotFiles = Directory.GetFiles(snapshotsDir, "*.json")
                    .Where(f => !f.EndsWith($"{newVersion.Replace("/", "_").Replace("\\", "_")}.json"))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                string oldVersion = "unknown";
                if (snapshotFiles.Count > 0)
                {
                    var name = Path.GetFileNameWithoutExtension(snapshotFiles[0]);
                    oldVersion = name.Replace("_", "/");
                }

                var changeReport = await _cliValidator.CompareVersionsAsync(oldHelp, newHelp, oldVersion, newVersion);

                if (changeReport.HasCriticalChanges || changeReport.RemovedFlags.Count > 0 || changeReport.AddedFlags.Count > 0)
                {
                    CliChangesDetected?.Invoke(this, changeReport);
                    _log.Information($"CLI changes detected: {changeReport.CriticalChanges.Count} critical, " +
                        $"{changeReport.RemovedFlags.Count} removed, {changeReport.AddedFlags.Count} added", "Updater");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning($"Post-install CLI validation failed: {ex.Message}", "Updater");
        }
    }

    void OnProgress(double value) => DownloadProgress?.Invoke(this, value);
    void OnStatus(string message) => StatusMessage?.Invoke(this, message);

    // Explicit interface implementations
    async Task<string> ILlamaUpdater.DownloadAndExtractAsync(string version, string targetDirectory, CancellationToken ct)
    {
        var releases = await ((ILlamaUpdater)this).FetchReleasesAsync(true, ct);
        var target = releases.FirstOrDefault(r => r.TagName == version);

        if (target == null)
            throw new InvalidOperationException($"Release '{version}' not found");

        var preferred = target.Assets
            .OrderByDescending(a => a.BuildType == BuildType.Cuda12x || a.BuildType == BuildType.Cuda13x ? 2 :
                                   a.BuildType == BuildType.Vulkan ? 1 : 0)
            .FirstOrDefault();

        if (preferred == null)
            throw new InvalidOperationException("No Windows binary found for this release");

        return await DownloadAndExtractAsync(version, targetDirectory, preferred, ct);
    }

    async Task<string> ILlamaUpdater.DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken ct)
    {
        return await DownloadAndExtractAsync(version, targetDirectory, asset, ct);
    }

    static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        if (bytes >= 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        if (bytes >= 1024L)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    class GithubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

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

        [System.Text.Json.Serialization.JsonPropertyName("size")]
        public long Size { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
