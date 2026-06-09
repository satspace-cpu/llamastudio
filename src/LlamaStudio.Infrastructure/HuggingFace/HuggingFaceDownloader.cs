using LlamaStudio.Core.Interfaces;
       using LlamaStudio.Core.Models;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace LlamaStudio.Infrastructure.HuggingFace;

/// <summary>
/// Downloads models from Hugging Face Hub with progress reporting.
/// Supports repo browsing, multi-file download, and browser-based auth.
/// </summary>
public class HuggingFaceDownloader : IHuggingFaceDownloader
{
    HttpClient _http;
    string? _authToken;
    CancellationTokenSource? _downloadCts;
    readonly ILogService _log;

    public event EventHandler<double>? DownloadProgress;
    public event EventHandler<string>? StatusMessage;
    public event EventHandler<HfFileInfo>? FileDownloaded;
    public event EventHandler? AllDownloadsCompleted;

    const string ApiBaseUrl = "https://huggingface.co/api";
    const string DownloadBaseUrl = "https://huggingface.co";
    const string LoginUrl = "https://huggingface.co/settings/tokens";

    public HuggingFaceDownloader(HttpClient http, ILogService log)
    {
        _log = log;
        _http = http;
        _http.Timeout = TimeSpan.FromMinutes(30);
    }

    // --- Auth ---

    public void SetAuthToken(string? token)
    {
        _authToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public string? GetAuthToken() => _authToken;

    public async Task<bool> LoginViaBrowserAsync()
    {
        try
        {
            StatusMessage?.Invoke(this, "Opening HuggingFace login page...");
            OpenUrl(LoginUrl);
            StatusMessage?.Invoke(this, "Open the browser, create/copy your token, then paste it below.");
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { /* Browser may not be available */ }
    }

    // --- Repo listing ---

    public async Task<List<HfFileInfo>> ListRepoFilesAsync(string urlOrRepoId, CancellationToken cancellationToken = default)
    {
        var repoId = ParseRepoIdFromUrl(urlOrRepoId);
        if (string.IsNullOrEmpty(repoId))
        {
            StatusMessage?.Invoke(this, "Invalid URL or repo ID");
            return new List<HfFileInfo>();
        }

        StatusMessage?.Invoke(this, $"Listing files in {repoId}...");
        _log.Information($"HF ListRepoFiles: repoId={repoId}", "HuggingFace");

        try
        {
            // Use the tree API to get all files in the repo
            var treeUrl = $"{ApiBaseUrl}/models/{repoId}/tree/main";
            _log.Information($"HF API URL: {treeUrl}", "HuggingFace");

            using var response = await _http.GetAsync(treeUrl, cancellationToken);
            _log.Information($"HF API response: {(int)response.StatusCode}", "HuggingFace");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _log.Error($"HF API error: {(int)response.StatusCode} {errorBody.Substring(0, Math.Min(200, errorBody.Length))}", "HuggingFace");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    StatusMessage?.Invoke(this, "Unauthorized. Please set your HuggingFace token.");
                }
                else
                {
                    StatusMessage?.Invoke(this, $"API error: {(int)response.StatusCode}");
                }
                return new List<HfFileInfo>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _log.Information($"HF API json length: {json.Length}", "HuggingFace");

            // Parse response: HF may return [ ... ] (direct array) or { "value": [ ... ] }
            var root = JsonDocument.Parse(json);
            var elements = new List<JsonElement>();
            if (root.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Direct array response
                foreach (var elem in root.RootElement.EnumerateArray())
                    elements.Add(elem);
            }
            else if (root.RootElement.ValueKind == JsonValueKind.Object && root.RootElement.TryGetProperty("value", out var valueProp))
            {
                // Wrapped in { "value": [...] }
                foreach (var elem in valueProp.EnumerateArray())
                    elements.Add(elem);
            }

            if (elements.Count == 0)
            {
                StatusMessage?.Invoke(this, "No files found in repository");
                return new List<HfFileInfo>();
            }

            // Filter to .gguf files only
            var result = new List<HfFileInfo>();
            foreach (var elem in elements)
            {
                var path = elem.GetProperty("path").GetString();
                if (path == null || !path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                    continue;

                long size = 0;
                if (elem.TryGetProperty("size", out var sz))
                    size = sz.GetInt64();

                string sha256 = string.Empty;
                if (elem.TryGetProperty("lfs", out var lfs) && lfs.TryGetProperty("oid", out var oid))
                    sha256 = oid.GetString() ?? string.Empty;
                else if (elem.TryGetProperty("oid", out var directOid))
                    sha256 = directOid.GetString() ?? string.Empty;

                result.Add(new HfFileInfo
                {
                    RepoId = repoId,
                    Path = path,
                    Size = size,
                    Sha256 = sha256,
                });
            }

            _log.Information($"HF found {result.Count} .gguf files from {elements.Count} total", "HuggingFace");
            StatusMessage?.Invoke(this, $"Found {result.Count} model files");
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "HF list failed", "HuggingFace");
            StatusMessage?.Invoke(this, $"Error: {ex.Message}");
            return new List<HfFileInfo>();
        }
    }

    // --- Multi-file download ---

    public async Task DownloadSelectedAsync(List<HfFileInfo> files, string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            StatusMessage?.Invoke(this, "No files selected for download");
            return;
        }

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        _downloadCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_downloadCts.Token, cancellationToken);
        var ct = linkedCts.Token;

        int total = files.Count;
        int completed = 0;

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested)
            {
                StatusMessage?.Invoke(this, "Downloads cancelled");
                break;
            }

            file.IsDownloading = true;
            file.Status = "Starting...";
            file.DownloadProgress = 0;

            try
            {
                var localPath = Path.Combine(targetDirectory, file.FileName);

                // Clean up legacy temp files (.download.tmp) before processing
                var legacyTemp = localPath + ".download.tmp";
                if (File.Exists(legacyTemp))
                {
                    try { File.Delete(legacyTemp); } catch { }
                }

                // Also clean up any GUID-based temp files from previous failed downloads
                var dirName = Path.GetDirectoryName(localPath) ?? ".";
                var tempPattern = $"_.{file.FileName}.*.tmp";
                foreach (var existingTmp in Directory.GetFiles(dirName, tempPattern))
                {
                    try { File.Delete(existingTmp); } catch { }
                }

                // Skip if already exists and matches expected hash/size
                if (File.Exists(localPath))
                {
                    var localSize = new FileInfo(localPath).Length;
                    bool skip = false;

                    // Fast path: size mismatch — definitely need to re-download
                    if (file.Size > 0 && localSize != file.Size)
                    {
                        _log.Information($"Size mismatch for {file.FileName}: local={localSize}, remote={file.Size}, re-downloading", "HuggingFace");
                    }
                    else if (!string.IsNullOrEmpty(file.Sha256))
                    {
                        // Verify by SHA256 hash (most reliable)
                        file.Status = "Verifying hash...";
                        var localHash = ComputeSha256(localPath);
                        if (string.Equals(localHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            skip = true;
                            _log.Information($"SHA256 match for {file.FileName}, skipping", "HuggingFace");
                        }
                        else
                        {
                            _log.Information($"SHA256 mismatch for {file.FileName}: local={localHash}, remote={file.Sha256}, re-downloading", "HuggingFace");
                        }
                    }
                    else if (file.Size > 0 && localSize == file.Size)
                    {
                        // Fallback: verify by size when hash not available
                        skip = true;
                        _log.Information($"Size match for {file.FileName}: {localSize} bytes, skipping", "HuggingFace");
                    }
                    else if (file.Size == 0 && localSize > 0)
                    {
                        // No remote info — assume file is good if it exists and is non-empty
                        skip = true;
                        _log.Information($"No remote info for {file.FileName}, local file exists ({localSize} bytes), skipping", "HuggingFace");
                    }

                    if (skip)
                    {
                        file.Status = "Already downloaded";
                        file.IsCompleted = true;
                        file.DownloadProgress = 100;
                        file.LocalPath = localPath;
                        completed++;
                        UpdateOverallProgress(completed, total);
                        continue;
                    }
                }

                file.Status = "Downloading...";
                var tempPath = Path.Combine(targetDirectory, $"_.{file.FileName}.{Guid.NewGuid():N}.tmp");

                using var response = await _http.GetAsync(file.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                long downloadedBytes = 0;

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[65536];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    downloadedBytes += bytesRead;

                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        var progress = (double)downloadedBytes / totalBytes.Value * 100;
                        file.DownloadProgress = Math.Min(progress, 99);
                        file.Status = $"Downloading... {file.DownloadProgress:F0}%";
                    }
                }

                file.DownloadProgress = 100;
                file.Status = "Finalizing...";

                // Ensure all data is flushed and file handle is fully released
                await fileStream.FlushAsync(ct);
                fileStream.Close();

                // Small delay to let OS release file handle
                await Task.Delay(100, ct);

                // Move temp to final — extended retry on file lock
                bool moved = false;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    try
                    {
                        if (File.Exists(localPath))
                        {
                            // Try to delete existing file with retry
                            for (int delAttempt = 0; delAttempt < 5; delAttempt++)
                            {
                                try
                                {
                                    File.Delete(localPath);
                                    break;
                                }
                                catch (IOException) when (delAttempt < 4)
                                {
                                    Thread.Sleep(500);
                                }
                            }
                        }
                        if (File.Exists(tempPath))
                            File.Move(tempPath, localPath);
                        moved = true;
                        break;
                    }
                    catch (IOException) when (attempt < 19)
                    {
                        Thread.Sleep(500);
                    }
                }

                if (!moved)
                {
                    // Last resort: try to copy instead of move
                    try
                    {
                        File.Copy(tempPath, localPath, true);
                        moved = true;
                    }
                    catch { }
                }

                if (!moved)
                    throw new IOException($"Cannot finalize file {file.FileName} — file locked after 20 attempts");

                file.Status = "Complete";
                file.IsCompleted = true;
                file.LocalPath = localPath;

                FileDownloaded?.Invoke(this, file);
                completed++;
                UpdateOverallProgress(completed, total);
            }
            catch (OperationCanceledException)
            {
                file.Status = "Cancelled";
                file.IsDownloading = false;
                CleanupTemp(file);
            }
            catch (Exception ex)
            {
                file.Status = $"Error: {ex.Message}";
                file.IsDownloading = false;
                // Clean up temp files on error
                try
                {
                    var tempPattern = $"_.{file.FileName}.*.tmp";
                    foreach (var tmp in Directory.GetFiles(targetDirectory, tempPattern))
                    {
                        try { File.Delete(tmp); } catch { }
                    }
                }
                catch { }
            }
            finally
            {
                file.IsDownloading = false;
            }
        }

        if (completed == total)
        {
            StatusMessage?.Invoke(this, $"All {total} files downloaded successfully");
            AllDownloadsCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            StatusMessage?.Invoke(this, $"Downloaded {completed}/{total} files");
        }

        _downloadCts = null;
    }

    void UpdateOverallProgress(int completed, int total)
    {
        DownloadProgress?.Invoke(this, (double)completed / total * 100);
    }

    static string ComputeSha256(string filePath)
    {
        using var algorithm = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = algorithm.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    void CleanupTemp(HfFileInfo file)
    {
        try
        {
            // Clean up any temp files matching the pattern
            var dir = Path.GetDirectoryName(file.LocalPath) ?? ".";
            var pattern = $"_.{file.FileName}.*.tmp";
            foreach (var tmp in Directory.GetFiles(dir, pattern))
            {
                try { File.Delete(tmp); } catch { }
            }
            // Also clean up legacy temp
            var legacyTemp = file.LocalPath + ".download.tmp";
            if (File.Exists(legacyTemp))
                File.Delete(legacyTemp);
        }
        catch { /* Ignore cleanup errors */ }
    }

    public void CancelDownloads()
    {
        _downloadCts?.Cancel();
        StatusMessage?.Invoke(this, "Cancelling downloads...");
    }

    // --- Legacy methods ---

    public async Task<HfModelInfo?> ResolveRepoAsync(string repo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repo))
            return null;

        try
        {
            StatusMessage?.Invoke(this, "Resolving model info...");
            var (repoId, fileName) = ParseRepoAndFile(repo);

            if (string.IsNullOrEmpty(fileName))
                return null;

            var apiUrl = $"{ApiBaseUrl}/models/{repoId}/info/{fileName}";
            using var response = await _http.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (data == null) return null;

            long size = 0;
            string sha256 = string.Empty;

            if (data.TryGetValue("size", out var sz))
                size = sz.GetInt64();

            if (data.TryGetValue("oid", out var oid))
                sha256 = oid.GetString() ?? string.Empty;

            var localPath = GetLocalFilePath(repoId, fileName);
            return new HfModelInfo
            {
                Repo = repoId,
                FileName = fileName,
                Size = size,
                Sha256 = sha256,
                LocalPath = localPath,
                ExistsLocally = File.Exists(localPath),
            };
        }
        catch (Exception ex)
        {
            StatusMessage?.Invoke(this, $"Resolve error: {ex.Message}");
            return null;
        }
    }

    public async Task<string> DownloadModelAsync(string repo, bool offlineMode = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentException("Repo cannot be empty", nameof(repo));

        var (repoId, fileName) = ParseRepoAndFile(repo);
        var localPath = GetLocalFilePath(repoId, fileName ?? Path.GetFileName(repo));

        if (offlineMode && File.Exists(localPath))
        {
            StatusMessage?.Invoke(this, $"Using cached model: {localPath}");
            return localPath;
        }

        if (offlineMode)
            throw new FileNotFoundException($"Model not found in cache: {localPath}");

        var downloadUrl = $"{DownloadBaseUrl}/{repoId}/resolve/main/{fileName ?? Path.GetFileName(repo)}";
        StatusMessage?.Invoke(this, $"Downloading from HuggingFace...");
        DownloadProgress?.Invoke(this, 0);

        var tempPath = localPath + ".download.tmp";

        try
        {
            using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            long downloadedBytes = 0;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[65536];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    var progress = (double)downloadedBytes / totalBytes.Value * 100;
                    DownloadProgress?.Invoke(this, Math.Min(progress, 99));
                }
            }

            DownloadProgress?.Invoke(this, 100);
            StatusMessage?.Invoke(this, "Download complete");

            if (File.Exists(localPath))
                File.Delete(localPath);
            File.Move(tempPath, localPath);

            return localPath;
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    public string GetCacheDirectory()
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "huggingface", "hub");

        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);

        return cacheDir;
    }

    // --- Parsing helpers ---

    static string ParseRepoIdFromUrl(string urlOrRepoId)
    {
        if (string.IsNullOrEmpty(urlOrRepoId))
            return string.Empty;

        // Handle full URLs like https://huggingface.co/unsloth/Qwen3.6-27B-MTP-GGUF
        if (urlOrRepoId.Contains("://"))
        {
            try
            {
                var uri = new Uri(urlOrRepoId);
                // Uri.Segments returns ["/", "user/", "repo/"] — skip leading "/" segment
                var segments = uri.Segments
                    .Where(s => s.Length > 1 && !string.IsNullOrEmpty(s.Trim()))
                    .Select(s => s.TrimEnd('/'))
                    .ToArray();

                // huggingface.co/user/repo or huggingface.co/user/repo/tree/main
                if (segments.Length >= 2)
                {
                    return $"{segments[0]}/{segments[1]}";
                }
            }
            catch { /* Invalid URL, try as-is */ }
        }

        // Handle repo ID directly: user/repo
        if (urlOrRepoId.Contains('/'))
        {
            var parts = urlOrRepoId.Split('/');
            if (parts.Length >= 2)
            {
                // If last part looks like a filename, exclude it
                if (parts.Last().EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                    return string.Join("/", parts.Take(parts.Length - 1));
                return $"{parts[0]}/{parts[1]}";
            }
        }

        return urlOrRepoId.Trim();
    }

    static (string repoId, string? fileName) ParseRepoAndFile(string input)
    {
        if (string.IsNullOrEmpty(input))
            return (string.Empty, null);

        var parts = input.Split('/');

        if (parts.Length == 1)
            return (input, null);

        if (parts.Length >= 2)
        {
            var lastPart = parts[parts.Length - 1];
            if (lastPart.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                return (string.Join("/", parts.Take(parts.Length - 1)), lastPart);
            return (input, null);
        }

        return (input, null);
    }

    string GetLocalFilePath(string repoId, string fileName)
    {
        var cacheDir = GetCacheDirectory();
        var repoDir = Path.Combine(cacheDir, SanitizePath(repoId));
        Directory.CreateDirectory(repoDir);
        return Path.Combine(repoDir, fileName);
    }

    static string SanitizePath(string path)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            path = path.Replace(c, '_');
        return path;
    }

    // --- GGUF Metadata ---

    public async Task LoadFileMetadataAsync(HfFileInfo file, CancellationToken cancellationToken = default)
    {
        file.IsMetadataLoading = true;
        file.Metadata.Clear();

        try
        {
            // Fetch model info from HF API — contains GGUF metadata in the "gguf" field
            var apiUrl = $"{ApiBaseUrl}/models/{file.RepoId}";
            using var response = await _http.GetAsync(apiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelData = JsonSerializer.Deserialize<JsonElement>(json);

            // Extract GGUF metadata from API response
            if (modelData.TryGetProperty("gguf", out var gguf) && gguf.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in gguf.EnumerateObject())
                {
                    var key = FormatMetadataKey(prop.Name);
                    var value = prop.Value.ToString();
                    file.Metadata.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            // Add model-level info
            if (modelData.TryGetProperty("cardData", out var cardData) && cardData.ValueKind == JsonValueKind.Object)
            {
                if (cardData.TryGetProperty("base_model", out var baseModels) && baseModels.ValueKind == JsonValueKind.Array)
                {
                    var bases = new List<string>();
                    foreach (var bm in baseModels.EnumerateArray())
                        bases.Add(bm.ToString());
                    file.Metadata.Add(new KeyValuePair<string, string>("Base Model(s)", string.Join(", ", bases)));
                }
                if (cardData.TryGetProperty("license", out var license))
                    file.Metadata.Add(new KeyValuePair<string, string>("License", license.ToString()));
            }

            // Add downloads/likes
            if (modelData.TryGetProperty("downloads", out var downloads))
                file.Metadata.Add(new KeyValuePair<string, string>("Downloads", downloads.GetInt64().ToString("N0")));
            if (modelData.TryGetProperty("likes", out var likes))
                file.Metadata.Add(new KeyValuePair<string, string>("Likes", likes.GetInt64().ToString("N0")));

            // Add file size
            file.Metadata.Add(new KeyValuePair<string, string>("File Size", file.SizeDisplay));
        }
        catch (Exception ex)
        {
            file.Metadata.Add(new KeyValuePair<string, string>("Error", ex.Message));
            _log.Error(ex, "Failed to load model metadata", "HuggingFace");
        }
        finally
        {
            file.IsMetadataLoading = false;
        }
    }

    static string FormatMetadataKey(string name)
    {
        return name switch
        {
            "architecture" => "Architecture",
            "context_length" => "Context Length",
            "totalFileSize" => "Total File Size",
            "total" => "Total Parameters",
            "quantize_imatrix_file" => "Imatrix File",
            _ => System.Text.RegularExpressions.Regex.Replace(name, "_", " ")
        };
    }
}


