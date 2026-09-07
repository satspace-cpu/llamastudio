using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.HuggingFace;

public class HuggingFaceDownloader : IHuggingFaceDownloader
{
	private HttpClient _http;

	private string? _authToken;

	private CancellationTokenSource? _downloadCts;

	private volatile bool _pauseRequested;

	private volatile bool _cancelRequested;

	private readonly ILogService _log;
	private readonly ILocalizationService _localization;

	private const string ApiBaseUrl = "https://huggingface.co/api";

	private const string DownloadBaseUrl = "https://huggingface.co";

	private const string LoginUrl = "https://huggingface.co/settings/tokens";

	public event EventHandler<double>? DownloadProgress;

	public event EventHandler<string>? StatusMessage;

	public event EventHandler<HfFileInfo>? FileDownloaded;

	public event EventHandler? AllDownloadsCompleted;

	public HuggingFaceDownloader(HttpClient http, ILogService log, ILocalizationService localization)
	{
		_log = log;
		_localization = localization;
		_http = http;
	}

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

	public string? GetAuthToken()
	{
		return _authToken;
	}

	public async Task<bool> LoginViaBrowserAsync()
	{
		try
		{
			this.StatusMessage?.Invoke(this, "Opening HuggingFace login page...");
			OpenUrl("https://huggingface.co/settings/tokens");
			this.StatusMessage?.Invoke(this, "Open the browser, create/copy your token, then paste it below.");
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	public async Task<List<HfFileInfo>> ListRepoFilesAsync(string urlOrRepoId, CancellationToken cancellationToken = default(CancellationToken))
	{
		string repoId = ParseRepoIdFromUrl(urlOrRepoId);
		if (string.IsNullOrEmpty(repoId))
		{
			this.StatusMessage?.Invoke(this, "Invalid URL or repo ID");
			return new List<HfFileInfo>();
		}
		this.StatusMessage?.Invoke(this, "Listing files in " + repoId + "...");
		_log.Information("HF ListRepoFiles: repoId=" + repoId, "HuggingFace");
		try
		{
			// GGUF repositories often keep quantizations in subdirectories. Ask HF
			// for the complete tree so all model variants are shown.
			string treeApiUrl = "https://huggingface.co/api/models/" + repoId + "/tree/main?recursive=true";
			_log.Information("HF API URL: " + treeApiUrl, "HuggingFace");
			using HttpResponseMessage response = await _http.GetAsync(treeApiUrl, cancellationToken);
			_log.Information($"HF API response: {response.StatusCode}", "HuggingFace");
			if (!response.IsSuccessStatusCode)
			{
				string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
				_log.Error($"HF API error: {response.StatusCode} {errorBody.Substring(0, Math.Min(200, errorBody.Length))}", "HuggingFace");
				if (response.StatusCode == HttpStatusCode.Unauthorized)
				{
					this.StatusMessage?.Invoke(this, "Unauthorized. Please set your HuggingFace token.");
				}
				else
				{
					this.StatusMessage?.Invoke(this, $"API error: {response.StatusCode}");
				}
				return new List<HfFileInfo>();
			}
			string jsonBody = await response.Content.ReadAsStringAsync(cancellationToken);
			_log.Information($"HF API json length: {jsonBody.Length}", "HuggingFace");
			JsonDocument jsonDocument = JsonDocument.Parse(jsonBody);
			List<JsonElement> fileEntries = new List<JsonElement>();
			JsonElement valueElement;
			if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement entry in jsonDocument.RootElement.EnumerateArray())
				{
					fileEntries.Add(entry);
				}
			}
			else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object && jsonDocument.RootElement.TryGetProperty("value", out valueElement))
			{
				foreach (JsonElement valueEntry in valueElement.EnumerateArray())
				{
					fileEntries.Add(valueEntry);
				}
			}
			if (fileEntries.Count == 0)
			{
				this.StatusMessage?.Invoke(this, "No files found in repository");
				return new List<HfFileInfo>();
			}
			List<HfFileInfo> files = new List<HfFileInfo>();
			foreach (JsonElement fileEntry in fileEntries)
			{
				string filePath = fileEntry.GetProperty("path").GetString();
				if (filePath != null && filePath.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
				{
					long size = 0L;
					if (fileEntry.TryGetProperty("size", out var sizeElement))
					{
						size = sizeElement.GetInt64();
					}
					string sha = string.Empty;
					JsonElement oidElement;
					if (fileEntry.TryGetProperty("lfs", out var lfsElement) && lfsElement.TryGetProperty("oid", out var lfsOidElement))
					{
						sha = lfsOidElement.GetString() ?? string.Empty;
					}
					else if (fileEntry.TryGetProperty("oid", out oidElement))
					{
						sha = oidElement.GetString() ?? string.Empty;
					}
					files.Add(new HfFileInfo
					{
						RepoId = repoId,
						Path = filePath,
						Size = size,
						Sha256 = sha
					});
				}
			}
			_log.Information($"HF found {files.Count} .gguf files from {fileEntries.Count} total", "HuggingFace");
			this.StatusMessage?.Invoke(this, $"Found {files.Count} model files");
			return files;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_log.Error(ex, "HF list failed", "HuggingFace");
			this.StatusMessage?.Invoke(this, "Error: " + ex.Message);
			return new List<HfFileInfo>();
		}
	}

	public async Task DownloadSelectedAsync(List<HfFileInfo> files, string targetDirectory, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (files == null || files.Count == 0)
		{
			this.StatusMessage?.Invoke(this, "No files selected for download");
			return;
		}
		if (!Directory.Exists(targetDirectory))
		{
			Directory.CreateDirectory(targetDirectory);
		}
		_downloadCts = new CancellationTokenSource();
		_pauseRequested = false;
		_cancelRequested = false;
		CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_downloadCts.Token, cancellationToken);
		CancellationToken ct = cancellationTokenSource.Token;
		int total = files.Count;
		int completed = 0;
		long aggregateTotalBytes = files.Where((HfFileInfo f) => f.Size > 0).Sum((HfFileInfo f) => f.Size);
		long aggregateDownloadedBase = 0L;
		DateTime lastStatsUpdate = DateTime.MinValue;
		long lastOverallBytes = 0L;
		foreach (HfFileInfo file in files)
		{
			if (ct.IsCancellationRequested)
			{
				this.StatusMessage?.Invoke(this, "Downloads cancelled");
				break;
			}
			file.IsDownloading = true;
			file.Status = "Starting...";
			file.DownloadProgress = 0.0;
			try
			{
				try
				{
					string localPath = Path.Combine(targetDirectory, file.FileName);
					file.LocalPath = localPath;
					string tempPath = localPath + ".part";
					bool needDownload = !File.Exists(localPath);
					long length = 0L;
					if (!needDownload)
					{
						length = new FileInfo(localPath).Length;
						bool isCompleted = false;
						if (file.Size > 0 && length != file.Size)
						{
							_log.Information($"Size mismatch for {file.FileName}: local={length}, remote={file.Size}, re-downloading", "HuggingFace");
						}
						else if (!string.IsNullOrEmpty(file.Sha256))
						{
							file.Status = "Verifying hash...";
							string localHash = ComputeSha256(localPath);
							if (string.Equals(localHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
							{
								isCompleted = true;
								_log.Information("SHA256 match for " + file.FileName + ", skipping", "HuggingFace");
							}
							else
							{
								_log.Information($"SHA256 mismatch for {file.FileName}: local={localHash}, remote={file.Sha256}, re-downloading", "HuggingFace");
							}
						}
						else if (file.Size > 0 && length == file.Size)
						{
							isCompleted = true;
							_log.Information($"Size match for {file.FileName}: {length} bytes, skipping", "HuggingFace");
						}
						else if (file.Size == 0L && length > 0)
						{
							isCompleted = true;
							_log.Information($"No remote info for {file.FileName}, local file exists ({length} bytes), skipping", "HuggingFace");
						}
						if (isCompleted)
						{
							file.Status = "Already downloaded";
							file.IsCompleted = true;
							file.DownloadProgress = 100.0;
							file.LocalPath = localPath;
							completed++;
							aggregateDownloadedBase += file.Size > 0 ? file.Size : length;
							UpdateOverallProgressBytes(aggregateDownloadedBase, aggregateTotalBytes, ref lastStatsUpdate, ref lastOverallBytes, total);
							continue;
						}
					}
					file.Status = File.Exists(tempPath) ? "Resuming..." : "Downloading...";
					long currentFileBytes = 0L;
					DateTime lastFileStatsUpdate = DateTime.MinValue;
					long fileStartBytes = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;
					long lastFileBytes = fileStartBytes;
					DateTime fileSpeedTimestamp = DateTime.UtcNow;
					currentFileBytes = await DownloadResumableAsync(file.DownloadUrl, tempPath, file.Size, ct, (downloadedBytes, totalBytes) =>
					{
						if (totalBytes > 0)
						{
							double progressPercent = (double)downloadedBytes / (double)totalBytes * 100.0;
							file.DownloadProgress = Math.Min(progressPercent, 99.0);
							DateTime now = DateTime.UtcNow;
							if ((now - lastFileStatsUpdate).TotalMilliseconds >= 250.0 || downloadedBytes >= totalBytes)
							{
								lastFileStatsUpdate = now;
								DateTime speedNow = DateTime.UtcNow;
								double speedSeconds = Math.Max((speedNow - fileSpeedTimestamp).TotalSeconds, 0.001);
								long speed = (long)Math.Max((downloadedBytes - lastFileBytes) / speedSeconds, 0.0);
								lastFileBytes = downloadedBytes;
								fileSpeedTimestamp = speedNow;
								long remaining = Math.Max(totalBytes - downloadedBytes, 0L);
								file.Status = _localization.T("web.hf.downloading_file") + " " + file.DownloadProgress.ToString("F0") + "% | " + FormatTransferSize(downloadedBytes) + " / " + FormatTransferSize(totalBytes) + " | " + _localization.T("web.hf.left") + " " + FormatTransferSize(remaining) + " | " + FormatTransferSpeed(speed);
							}
							UpdateOverallProgressBytes(aggregateDownloadedBase + downloadedBytes, aggregateTotalBytes, ref lastStatsUpdate, ref lastOverallBytes, total);
						}
					});
					file.DownloadProgress = 100.0;
					file.Status = "Finalizing...";
					await Task.Delay(100, ct);
					bool finalizeSucceeded = false;
					for (int j = 0; j < 20; j++)
					{
						try
						{
							if (File.Exists(localPath))
							{
								for (int k = 0; k < 5; k++)
								{
									try
									{
										File.Delete(localPath);
									}
									catch (IOException) when (k < 4)
									{
										Thread.Sleep(500);
										continue;
									}
									break;
								}
							}
							if (File.Exists(tempPath))
							{
								File.Move(tempPath, localPath);
							}
							finalizeSucceeded = true;
						}
						catch (IOException) when (j < 19)
						{
							Thread.Sleep(500);
							continue;
						}
						break;
					}
					if (!finalizeSucceeded)
					{
						try
						{
							File.Copy(tempPath, localPath, overwrite: true);
							finalizeSucceeded = true;
						}
						catch
						{
						}
					}
					if (!finalizeSucceeded)
					{
						throw new IOException("Cannot finalize file " + file.FileName + " — file locked after 20 attempts");
					}
					file.Status = "Complete";
					file.IsCompleted = true;
					file.LocalPath = localPath;
					this.FileDownloaded?.Invoke(this, file);
					completed++;
					aggregateDownloadedBase += file.Size > 0 ? file.Size : currentFileBytes;
					UpdateOverallProgressBytes(aggregateDownloadedBase, aggregateTotalBytes, ref lastStatsUpdate, ref lastOverallBytes, total);
				}
				catch (OperationCanceledException)
				{
					file.Status = _pauseRequested && !_cancelRequested ? "Paused" : "Cancelled";
					file.IsDownloading = false;
					if (_cancelRequested || !_pauseRequested)
					{
						CleanupTemp(file);
					}
				}
				catch (Exception ex)
				{
					file.Status = "Error: " + ex.Message;
					file.IsDownloading = false;
					try
					{
						string searchPattern = "_." + file.FileName + ".*.tmp";
						string[] tmpFiles = Directory.GetFiles(targetDirectory, searchPattern);
						foreach (string tmpPath in tmpFiles)
						{
							try
							{
								File.Delete(tmpPath);
							}
							catch
							{
							}
						}
					}
					catch
					{
					}
				}
			}
			finally
			{
				file.IsDownloading = false;
			}
		}
		if (completed == total)
		{
			this.StatusMessage?.Invoke(this, $"All {total} files downloaded successfully");
			this.AllDownloadsCompleted?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			this.StatusMessage?.Invoke(this, $"Downloaded {completed}/{total} files");
		}
		_downloadCts = null;
	}

	private void UpdateOverallProgress(int completed, int total)
	{
		this.DownloadProgress?.Invoke(this, (double)completed / (double)total * 100.0);
	}

	private void UpdateOverallProgressBytes(long downloaded, long totalBytes, ref DateTime lastStatsUpdate, ref long lastOverallBytes, int fileCount)
	{
		if (totalBytes <= 0)
		{
			return;
		}
		double progress = Math.Min((double)downloaded / totalBytes * 100.0, 100.0);
		DateTime now = DateTime.UtcNow;
		if ((now - lastStatsUpdate).TotalMilliseconds < 250.0 && downloaded < totalBytes)
		{
			return;
		}
		DateTime previousStatsUpdate = lastStatsUpdate;
		lastStatsUpdate = now;
		this.DownloadProgress?.Invoke(this, progress);
		double seconds = previousStatsUpdate == DateTime.MinValue ? 0.0 : Math.Max((now - previousStatsUpdate).TotalSeconds, 0.001);
		long speed = seconds > 0.0 ? (long)Math.Max((downloaded - lastOverallBytes) / seconds, 0.0) : 0L;
		lastOverallBytes = downloaded;
		this.StatusMessage?.Invoke(this, _localization.T("web.hf.downloading") + " " + fileCount + " " + _localization.T("web.hf.files_count") + ": " + FormatTransferSize(downloaded) + " / " + FormatTransferSize(totalBytes) + " | " + FormatTransferSpeed(speed));
	}

	private string FormatTransferSpeed(long bytesPerSecond)
	{
		double megabytes = (double)Math.Max(bytesPerSecond, 0L) / 1048576.0;
		double megabits = (double)Math.Max(bytesPerSecond, 0L) * 8.0 / 1000000.0;
		return megabytes.ToString("F1") + " " + _localization.T("web.hf.mbs") + " (" + megabits.ToString("F0") + " " + _localization.T("web.hf.mbits") + ")";
	}

	private string FormatTransferSize(long bytes)
	{
		if (bytes >= 1073741824L)
		{
			return $"{(double)bytes / 1073741824.0:F2} " + _localization.T("web.hf.gib");
		}
		if (bytes >= 1048576L)
		{
			return $"{(double)bytes / 1048576.0:F1} " + _localization.T("web.hf.mib");
		}
		return $"{Math.Max(bytes, 0L) / 1024.0:F0} " + _localization.T("web.hf.kib");
	}

	private static string ComputeSha256(string filePath)
	{
		using SHA256 sha256 = SHA256.Create();
		using FileStream inputStream = File.OpenRead(filePath);
		return BitConverter.ToString(sha256.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant();
	}

	private void CleanupTemp(HfFileInfo file)
	{
		try
		{
			string? directory = Path.GetDirectoryName(file.LocalPath) ?? ".";
			string searchPattern = "_." + file.FileName + ".*.tmp";
			string[] tmpFiles = Directory.GetFiles(directory, searchPattern);
			foreach (string tmpPath in tmpFiles)
			{
				try
				{
					File.Delete(tmpPath);
				}
				catch
				{
				}
			}
			string partPath = file.LocalPath + ".part";
			if (File.Exists(partPath))
			{
				File.Delete(partPath);
			}
			string legacyPath = file.LocalPath + ".download.tmp";
			if (File.Exists(legacyPath))
			{
				File.Delete(legacyPath);
			}
		}
		catch
		{
		}
	}

	public void CancelDownloads()
	{
		_cancelRequested = true;
		_pauseRequested = false;
		_downloadCts?.Cancel();
		this.StatusMessage?.Invoke(this, "Cancelling downloads and clearing partial files...");
	}

	public void PauseDownloads()
	{
		_pauseRequested = true;
		_cancelRequested = false;
		_downloadCts?.Cancel();
		this.StatusMessage?.Invoke(this, "Pausing downloads...");
	}

	public async Task<HfModelInfo?> ResolveRepoAsync(string repo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(repo))
		{
			return null;
		}
		try
		{
			this.StatusMessage?.Invoke(this, "Resolving model info...");
			var (repoId, fileName) = ParseRepoAndFile(repo);
			if (string.IsNullOrEmpty(fileName))
			{
				return null;
			}
			string requestUri = $"{"https://huggingface.co/api"}/models/{repoId}/info/{fileName}";
			using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			Dictionary<string, JsonElement> modelInfoJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await response.Content.ReadAsStringAsync(cancellationToken));
			if (modelInfoJson == null)
			{
				return null;
			}
			long size = 0L;
			string sha = string.Empty;
			if (modelInfoJson.TryGetValue("size", out var sizeElement))
			{
				size = sizeElement.GetInt64();
			}
			if (modelInfoJson.TryGetValue("oid", out var oidElement))
			{
				sha = oidElement.GetString() ?? string.Empty;
			}
			string localFilePath = GetLocalFilePath(repoId, fileName);
			return new HfModelInfo
			{
				Repo = repoId,
				FileName = fileName,
				Size = size,
				Sha256 = sha,
				LocalPath = localFilePath,
				ExistsLocally = File.Exists(localFilePath)
			};
		}
		catch (Exception ex)
		{
			this.StatusMessage?.Invoke(this, "Resolve error: " + ex.Message);
			return null;
		}
	}

	public async Task<string> DownloadModelAsync(string repo, bool offlineMode = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(repo))
		{
			throw new ArgumentException("Repo cannot be empty", "repo");
		}
		(string, string) parsed = ParseRepoAndFile(repo);
		string repoId = parsed.Item1;
		string fileName = parsed.Item2;
		string localPath = GetLocalFilePath(repoId, fileName ?? Path.GetFileName(repo));
		if (offlineMode && File.Exists(localPath))
		{
			this.StatusMessage?.Invoke(this, "Using cached model: " + localPath);
			return localPath;
		}
		if (offlineMode)
		{
			throw new FileNotFoundException("Model not found in cache: " + localPath);
		}
		_downloadCts ??= new CancellationTokenSource();
		using CancellationTokenSource modelDownloadCts = CancellationTokenSource.CreateLinkedTokenSource(_downloadCts.Token, cancellationToken);
		CancellationToken downloadToken = modelDownloadCts.Token;
		string expectedHash = null;
		try
		{
			string requestUri = $"{"https://huggingface.co/api"}/models/{repoId}/info/{fileName ?? Path.GetFileName(repo)}";
			using HttpResponseMessage infoResponse = await _http.GetAsync(requestUri, downloadToken);
			JsonElement oidElement = default(JsonElement);
			if (infoResponse.IsSuccessStatusCode && (JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await infoResponse.Content.ReadAsStringAsync(downloadToken))?.TryGetValue("oid", out oidElement) ?? false))
			{
				expectedHash = oidElement.GetString();
			}
		}
		catch
		{
			_log.Debug("Could not resolve SHA-256 from HF API, will skip hash verification", "HuggingFace");
		}
		string modelDownloadUrl = $"{"https://huggingface.co"}/{repoId}/resolve/main/{fileName ?? Path.GetFileName(repo)}";
		this.StatusMessage?.Invoke(this, "Downloading from HuggingFace...");
		this.DownloadProgress?.Invoke(this, 0.0);
		string tempPath = localPath + ".part";
		try
		{
			await DownloadResumableAsync(modelDownloadUrl, tempPath, 0L, downloadToken, (downloadedBytes, totalBytes) =>
			{
				if (totalBytes > 0)
				{
					double progressPercent = (double)downloadedBytes / (double)totalBytes * 100.0;
					this.DownloadProgress?.Invoke(this, Math.Min(progressPercent, 99.0));
				}
			});
			this.DownloadProgress?.Invoke(this, 100.0);
			this.StatusMessage?.Invoke(this, "Download complete");
			if (!string.IsNullOrEmpty(expectedHash))
			{
				this.StatusMessage?.Invoke(this, "Verifying SHA-256 hash...");
				string actualHash = ComputeSha256(tempPath);
				if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
				{
					string message = $"SHA-256 hash mismatch for downloaded model: expected {expectedHash}, got {actualHash}. File may be corrupted or tampered with.";
					_log.Error(message, "HuggingFace");
					File.Delete(tempPath);
					throw new SecurityException(message);
				}
				_log.Information("SHA-256 hash verified for " + fileName, "HuggingFace");
			}
			if (File.Exists(localPath))
			{
				File.Delete(localPath);
			}
			File.Move(tempPath, localPath);
			return localPath;
		}
		catch
		{
			// Preserve the .part file on cancellation or a connection failure.
			throw;
		}
	}

	private async Task<long> DownloadResumableAsync(string url, string tempPath, long expectedSize, CancellationToken cancellationToken, Action<long, long> progress)
	{
		long offset = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
		if (offset > 0)
		{
			request.Headers.Range = new RangeHeaderValue(offset, null);
		}
		using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		if (offset > 0 && response.StatusCode != HttpStatusCode.PartialContent)
		{
			try { File.Delete(tempPath); } catch { }
			return await DownloadResumableAsync(url, tempPath, expectedSize, cancellationToken, progress);
		}
		response.EnsureSuccessStatusCode();
		long total = expectedSize > 0 ? expectedSize : (response.Content.Headers.ContentLength ?? 0L) + offset;
		using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using FileStream fileStream = new FileStream(tempPath, offset > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 81920, useAsync: true);
		byte[] buffer = new byte[65536];
		long downloaded = offset;
		progress(downloaded, total);
		while (true)
		{
			int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
			if (bytesRead <= 0)
			{
				break;
			}
			await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
			downloaded += bytesRead;
			progress(downloaded, total);
		}
		await fileStream.FlushAsync(cancellationToken);
		return downloaded;
	}

	public string GetCacheDirectory()
	{
		string cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "huggingface", "hub");
		if (!Directory.Exists(cacheDirectory))
		{
			Directory.CreateDirectory(cacheDirectory);
		}
		return cacheDirectory;
	}

	private static string ParseRepoIdFromUrl(string urlOrRepoId)
	{
		if (string.IsNullOrEmpty(urlOrRepoId))
		{
			return string.Empty;
		}
		if (urlOrRepoId.Contains("://"))
		{
			try
			{
				string[] segments = (from s in new Uri(urlOrRepoId).Segments
					where s.Length > 1 && !string.IsNullOrEmpty(s.Trim())
					select s.TrimEnd('/')).ToArray();
				if (segments.Length >= 2)
				{
					return segments[0] + "/" + segments[1];
				}
			}
			catch
			{
			}
		}
		if (urlOrRepoId.Contains('/'))
		{
			string[] parts = urlOrRepoId.Split('/');
			if (parts.Length >= 2)
			{
				if (parts.Last().EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
				{
					return string.Join("/", parts.Take(parts.Length - 1));
				}
				return parts[0] + "/" + parts[1];
			}
		}
		return urlOrRepoId.Trim();
	}

	private static (string repoId, string? fileName) ParseRepoAndFile(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return (repoId: string.Empty, fileName: null);
		}
		string[] parts = input.Split('/');
		if (parts.Length == 1)
		{
			return (repoId: input, fileName: null);
		}
		if (parts.Length >= 2)
		{
			string lastPart = parts[^1];
			if (lastPart.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
			{
				return (repoId: string.Join("/", parts.Take(parts.Length - 1)), fileName: lastPart);
			}
			return (repoId: input, fileName: null);
		}
		return (repoId: input, fileName: null);
	}

	private string GetLocalFilePath(string repoId, string fileName)
	{
		string repoDirectory = Path.Combine(GetCacheDirectory(), SanitizePath(repoId));
		Directory.CreateDirectory(repoDirectory);
		string localPath = Path.Combine(repoDirectory, fileName);
		string? directoryName = Path.GetDirectoryName(localPath);
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		return localPath;
	}

	private static string SanitizePath(string path)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			path = path.Replace(oldChar, '_');
		}
		return path;
	}

	public async Task LoadFileMetadataAsync(HfFileInfo file, CancellationToken cancellationToken = default(CancellationToken))
	{
		file.IsMetadataLoading = true;
		file.Metadata.Clear();
		try
		{
			string requestUri = "https://huggingface.co/api/models/" + file.RepoId;
			using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken);
			response.EnsureSuccessStatusCode();
			JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(cancellationToken));
			if (jsonElement.TryGetProperty("gguf", out var ggufElement) && ggufElement.ValueKind == JsonValueKind.Object)
			{
				foreach (JsonProperty metadataProperty in ggufElement.EnumerateObject())
				{
					string key = FormatMetadataKey(metadataProperty.Name);
					string metadataValue = metadataProperty.Value.ToString();
					file.Metadata.Add(new KeyValuePair<string, string>(key, metadataValue));
				}
			}
			if (jsonElement.TryGetProperty("cardData", out var cardDataElement) && cardDataElement.ValueKind == JsonValueKind.Object)
			{
				if (cardDataElement.TryGetProperty("base_model", out var baseModelElement) && baseModelElement.ValueKind == JsonValueKind.Array)
				{
					List<string> baseModels = new List<string>();
					foreach (JsonElement baseModelEntry in baseModelElement.EnumerateArray())
					{
						baseModels.Add(baseModelEntry.ToString());
					}
					file.Metadata.Add(new KeyValuePair<string, string>("Base Model(s)", string.Join(", ", baseModels)));
				}
				if (cardDataElement.TryGetProperty("license", out var licenseElement))
				{
					file.Metadata.Add(new KeyValuePair<string, string>("License", licenseElement.ToString()));
				}
			}
			if (jsonElement.TryGetProperty("downloads", out var downloadsElement))
			{
				file.Metadata.Add(new KeyValuePair<string, string>("Downloads", downloadsElement.GetInt64().ToString("N0")));
			}
			if (jsonElement.TryGetProperty("likes", out var likesElement))
			{
				file.Metadata.Add(new KeyValuePair<string, string>("Likes", likesElement.GetInt64().ToString("N0")));
			}
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

	private static string FormatMetadataKey(string name)
	{
		return name switch
		{
			"architecture" => "Architecture", 
			"context_length" => "Context Length", 
			"totalFileSize" => "Total File Size", 
			"total" => "Total Parameters", 
			"quantize_imatrix_file" => "Imatrix File", 
			_ => Regex.Replace(name, "_", " "), 
		};
	}

	public async Task<List<HfSearchResult>> SearchModelsAsync(string query, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return new List<HfSearchResult>();
		}
		string requestUri = "https://huggingface.co/api/models?search=" + Uri.EscapeDataString(query) + "&sort=downloads&direction=-1&limit=20";
		try
		{
			using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken);
			response.EnsureSuccessStatusCode();
			JsonDocument jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
			List<HfSearchResult> results = new List<HfSearchResult>();
			foreach (JsonElement modelEntry in jsonDocument.RootElement.EnumerateArray())
			{
				string modelId = modelEntry.GetProperty("id").GetString();
				if (string.IsNullOrEmpty(modelId))
				{
					continue;
				}
				int likes = 0;
				if (modelEntry.TryGetProperty("likes", out var likesElement))
				{
					likes = likesElement.GetInt32();
				}
				int downloads = 0;
				if (modelEntry.TryGetProperty("downloads", out var downloadsElement))
				{
					downloads = downloadsElement.GetInt32();
				}
				string pipelineTag = string.Empty;
				if (modelEntry.TryGetProperty("pipeline_tag", out var pipelineTagElement))
				{
					pipelineTag = pipelineTagElement.GetString() ?? string.Empty;
				}
				string lastModified = string.Empty;
				if (modelEntry.TryGetProperty("lastModified", out var lastModifiedElement))
				{
					lastModified = lastModifiedElement.GetString() ?? string.Empty;
				}
				List<string> tags = new List<string>();
				if (modelEntry.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement tagEntry in tagsElement.EnumerateArray())
					{
						tags.Add(tagEntry.ToString());
					}
				}
				string[] idParts = modelId.Split('/');
				string author = ((idParts.Length != 0) ? idParts[0] : string.Empty);
				string modelName = ((idParts.Length > 1) ? idParts[1] : modelId);
				results.Add(new HfSearchResult
				{
					Id = modelId,
					Author = author,
					ModelName = modelName,
					Likes = likes,
					Downloads = downloads,
					PipelineTag = pipelineTag,
					LastModified = lastModified,
					Tags = tags
				});
			}
			return results;
		}
		catch (Exception exception)
		{
			_log.Error(exception, "HF search failed", "HuggingFace");
			return new List<HfSearchResult>();
		}
	}
}
