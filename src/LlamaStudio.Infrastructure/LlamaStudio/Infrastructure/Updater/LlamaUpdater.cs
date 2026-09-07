using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Updater;

public class LlamaUpdater : ILlamaUpdater
{
	private class GithubRelease
	{
		[JsonPropertyName("tag_name")]
		public string TagName { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("html_url")]
		public string HtmlUrl { get; set; } = string.Empty;

		[JsonPropertyName("published_at")]
		public DateTime PublishedAt { get; set; }

		[JsonPropertyName("prerelease")]
		public bool Prerelease { get; set; }

		[JsonPropertyName("body")]
		public string Body { get; set; } = string.Empty;

		[JsonPropertyName("assets")]
		public List<GithubAsset>? Assets { get; set; }
	}

	private class GithubAsset
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("browser_download_url")]
		public string BrowserDownloadUrl { get; set; } = string.Empty;

		[JsonPropertyName("size")]
		public long Size { get; set; }

		[JsonPropertyName("updated_at")]
		public DateTime UpdatedAt { get; set; }
	}

	private readonly HttpClient _httpClient;

	private readonly ILogService _log;

	private readonly ISettings _settings;

	private readonly ICliValidator _cliValidator;

	private readonly IHelpParser _helpParser;

	private readonly IVersionCache _versionCache;

	private static string DefaultDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");

	public event EventHandler<double>? DownloadProgress;

	public event EventHandler<string>? StatusMessage;

	public event EventHandler<CliChangeReport>? CliChangesDetected;

	public LlamaUpdater(HttpClient httpClient, ILogService log, ISettings settings, ICliValidator cliValidator, IHelpParser helpParser, IVersionCache versionCache)
	{
		_httpClient = httpClient;
		_log = log;
		_settings = settings;
		_cliValidator = cliValidator;
		_helpParser = helpParser;
		_versionCache = versionCache;
	}

	async Task<List<LlamaCppRelease>> ILlamaUpdater.FetchReleasesAsync(bool includePrerelease, CancellationToken ct)
	{
		return await FetchReleasesFromRepo("ggml-org/llama.cpp", includePrerelease, ct);
	}

	async Task<List<LlamaCppRelease>> ILlamaUpdater.FetchReleasesFromRepoAsync(string ownerRepo, CancellationToken ct)
	{
		return await FetchReleasesFromRepo(ownerRepo, includePrerelease: true, ct);
	}

	private async Task<List<LlamaCppRelease>> FetchReleasesFromRepo(string ownerRepo, bool includePrerelease, CancellationToken ct)
	{
		_log.Information("Fetching releases from " + ownerRepo + "...", "Updater");
		string[] repoParts = ownerRepo.Split('/');
		if (repoParts.Length != 2)
		{
			_log.Error("Invalid repo format: " + ownerRepo, "Updater");
			return new List<LlamaCppRelease>();
		}
		List<GithubRelease> releases = await _httpClient.GetFromJsonAsync<List<GithubRelease>>($"https://api.github.com/repos/{Uri.EscapeDataString(repoParts[0])}/{Uri.EscapeDataString(repoParts[1])}/releases?per_page=30", ct);
		if (releases == null || releases.Count == 0)
		{
			return new List<LlamaCppRelease>();
		}
		List<LlamaCppRelease> supportedReleases = new List<LlamaCppRelease>();
		foreach (GithubRelease release in releases)
		{
			if (!includePrerelease && release.Prerelease)
			{
				continue;
			}
			List<ReleaseAsset> platformAssets = FilterPlatformAssets(release.Assets);
			// Official releases ship an Ubuntu CPU archive, but no ready-made CUDA archive.
			// Expose a first-class build card so Linux users can update the same release
			// through LlamaStudio, compiling it locally for the installed NVIDIA GPU.
			// (r88: restored — r84 removed these by accident along with the cafe cards;
			//  the CUDA 12/13 cards are the standard Linux CUDA path, not a third-party build.
			//  Cafe / local third-party build cards stay removed.)
			if (OperatingSystem.IsLinux() && string.Equals(ownerRepo, "ggml-org/llama.cpp", StringComparison.OrdinalIgnoreCase))
			{
				platformAssets.InsertRange(0, CreateLinuxCudaBuildAssets(release.TagName, release.PublishedAt));
			}
			if (platformAssets.Count != 0)
			{
				supportedReleases.Add(new LlamaCppRelease
				{
					TagName = release.TagName,
					Name = release.Name,
					HtmlUrl = release.HtmlUrl,
					PublishedAt = release.PublishedAt,
					IsPrerelease = release.Prerelease,
					Body = release.Body,
					Assets = platformAssets
				});
				if (supportedReleases.Count >= 30)
				{
				break;
				}
			}
		}
		_log.Information($"Found {supportedReleases.Count} releases with supported platform assets from {ownerRepo}", "Updater");
		return supportedReleases;
	}

	private static List<ReleaseAsset> CreateLinuxCudaBuildAssets(string version, DateTime publishedAt)
	{
		return new List<ReleaseAsset>
		{
			new ReleaseAsset
			{
				Name = LinuxCudaLlamaBuilder.LocalCuda12AssetPrefix + version,
				BuildType = BuildType.Cuda12x,
				CudaVersion = "CUDA 12",
				UpdatedAt = publishedAt,
				DescriptionOverride = "Build CUDA 12 + RPC for NVIDIA GPUs detected on this computer (requires CUDA 12 toolkit)",
				SizeDisplayOverride = "local build"
			},
			new ReleaseAsset
			{
				Name = LinuxCudaLlamaBuilder.LocalCuda13AssetPrefix + version,
				BuildType = BuildType.Cuda13x,
				CudaVersion = "CUDA 13",
				UpdatedAt = publishedAt,
				DescriptionOverride = "Build CUDA 13 + RPC for NVIDIA GPUs detected on this computer",
				SizeDisplayOverride = "local build"
			},
			new ReleaseAsset
			{
				Name = LinuxCudaLlamaBuilder.UniversalCuda12AssetPrefix + version,
				BuildType = BuildType.Cuda12x,
				CudaVersion = "CUDA 12",
				UpdatedAt = publishedAt,
				DescriptionOverride = "Build universal CUDA 12 + RPC (all NVIDIA architectures; requires CUDA 12 toolkit)",
				SizeDisplayOverride = "local build"
			},
			new ReleaseAsset
			{
				Name = LinuxCudaLlamaBuilder.UniversalCuda13AssetPrefix + version,
				BuildType = BuildType.Cuda13x,
				CudaVersion = "CUDA 13",
				UpdatedAt = publishedAt,
				DescriptionOverride = "Build universal CUDA 13 + RPC (all NVIDIA architectures)",
				SizeDisplayOverride = "local build"
			}
		};
	}

	private List<ReleaseAsset> FilterPlatformAssets(List<GithubAsset>? assets)
	{
		if (assets == null)
		{
			return new List<ReleaseAsset>();
		}
		List<ReleaseAsset> supportedAssets = new List<ReleaseAsset>();
		foreach (GithubAsset asset in assets)
		{
			string assetNameLower = asset.Name.ToLowerInvariant();
			bool isWindowsAsset = assetNameLower.Contains("-win-") && assetNameLower.EndsWith(".zip");
			bool isLinuxAsset = (assetNameLower.Contains("-ubuntu-") || assetNameLower.Contains("-linux-")) && assetNameLower.Contains("-x64") && (assetNameLower.EndsWith(".tar.gz") || assetNameLower.EndsWith(".tgz"));
			bool isSupported = OperatingSystem.IsWindows() ? isWindowsAsset : (OperatingSystem.IsLinux() && isLinuxAsset);
			if (!assetNameLower.StartsWith("cudart-") && isSupported)
			{
				ReleaseAsset releaseAsset = ReleaseAsset.ParseAsset(asset.Name, asset.BrowserDownloadUrl, asset.Size, asset.UpdatedAt);
				supportedAssets.Add(releaseAsset);
			}
		}
		return supportedAssets;
	}

	async Task<LlamaCppRelease?> ILlamaUpdater.GetLatestReleaseAsync(bool includePrerelease, CancellationToken ct)
	{
		return (await ((ILlamaUpdater)this).FetchReleasesAsync(includePrerelease, ct)).FirstOrDefault();
	}

	private async Task<string> DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken ct, bool ncclEnabled)
	{
		if (OperatingSystem.IsLinux() && LinuxCudaLlamaBuilder.IsBuildAsset(asset.Name))
		{
		bool isCafe = asset.Name.StartsWith(LinuxCudaLlamaBuilder.CafeAssetPrefix, StringComparison.Ordinal);
		return await LinuxCudaLlamaBuilder.BuildAsync(
		version,
		targetDirectory,
		asset.Name,
		OnStatus,
		_log,
		ct,
		ncclEnabled,
		isCafe ? LinuxCudaLlamaBuilder.CafeRepoUrl : null,
		isCafe ? LinuxCudaLlamaBuilder.CafeBranch : null,
		isCafe ? LinuxCudaLlamaBuilder.CafeCudaArch : null,
		isCafe ? LinuxCudaLlamaBuilder.CafeFolderName : null).ConfigureAwait(false);
		}
		ct.ThrowIfCancellationRequested();
		string buildTypeString = asset.BuildType.ToString().ToLowerInvariant().Replace(" ", "");
		string path = version + "-" + buildTypeString;
		string installDir = Path.Combine(targetDirectory, path);
		// Keep the staging directory on the selected volume. Directory.Move cannot
		// move a directory from the system temp drive to a user-selected drive.
		Directory.CreateDirectory(targetDirectory);
		string tempExtractDir = Path.Combine(targetDirectory, $".llama_studio_extract_{Guid.NewGuid():N}");
		List<(string Name, string Url, long Size)> archivesToDownload = new List<(string, string, long)> { (asset.Name, asset.BrowserDownloadUrl, asset.Size) };
		if (asset.BuildType == BuildType.Cuda12x || asset.BuildType == BuildType.Cuda13x)
		{
			GithubAsset githubAsset = await FindCudaRuntimeArchiveAsync(version, asset);
			if (githubAsset != null)
			{
				archivesToDownload.Add((githubAsset.Name, githubAsset.BrowserDownloadUrl, githubAsset.Size));
			}
		}
		long totalSize = archivesToDownload.Sum<(string, string, long)>(((string Name, string Url, long Size) a) => a.Size);
		object progressLock = new object();
		List<string> tempFiles = new List<string>();
		long[] downloadedPerArchive = new long[archivesToDownload.Count];
		try
		{
			int i;
			for (i = 0; i < archivesToDownload.Count; i++)
			{
				(string Name, string Url, long Size) archive = archivesToDownload[i];
				string archiveName = archive.Name;
				string archiveUrl = archive.Url;
				long archiveSize = archive.Size;
				string progressLabel = ((archivesToDownload.Count > 1) ? $"{i + 1}/{archivesToDownload.Count}" : "");
				OnStatus($"Downloading {progressLabel} {archiveName} ({FormatSize(archiveSize)})...");
				_log.Information("Downloading: " + archiveName, "Updater");
			string archiveExtension = archiveName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ? ".tar.gz" : Path.GetExtension(archiveName);
			string tempArchivePath = Path.Combine(Path.GetTempPath(), $"llama_studio_{Guid.NewGuid():N}{archiveExtension}");
				tempFiles.Add(tempArchivePath);
				await DownloadToFileAsync(archiveUrl, tempArchivePath, delegate(long total, long downloaded)
				{
					lock (progressLock)
					{
						downloadedPerArchive[i] = downloaded;
						long totalDownloaded = downloadedPerArchive.Sum();
						double progress = ((totalSize > 0) ? ((double)totalDownloaded / (double)totalSize * 100.0) : 0.0);
						OnProgress(Math.Min(progress, 100.0));
					}
				}, ct);
			}
			OnStatus("Extracting archives...");
			_log.Information("Extracting archives...", "Updater");
			Directory.CreateDirectory(tempExtractDir);
			int archiveIndex = 0;
			foreach (string tempFile in tempFiles)
			{
				ct.ThrowIfCancellationRequested();
				archiveIndex++;
				OnStatus($"Extracting archive {archiveIndex}/{tempFiles.Count}...");
				await Task.Run(() => ExtractArchiveFlattened(tempFile, tempExtractDir, ct), ct).ConfigureAwait(false);
			}
			string serverExecutable = GetServerExecutableName();
			if (!File.Exists(Path.Combine(tempExtractDir, serverExecutable)))
			{
				throw new InvalidOperationException(serverExecutable + " not found after extraction!");
			}
			if (OperatingSystem.IsLinux())
			{
				File.SetUnixFileMode(Path.Combine(tempExtractDir, serverExecutable), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
			}
			string[] files = Directory.GetFiles(tempExtractDir, "*.*", SearchOption.TopDirectoryOnly);
			_log.Information("Installed files: [" + string.Join(", ", files.Select((string f) => Path.GetFileName(f))) + "]", "Updater");
			ct.ThrowIfCancellationRequested();
			if (Directory.Exists(installDir))
			{
				Directory.Delete(installDir, recursive: true);
			}
			Directory.Move(tempExtractDir, installDir);
			OnStatus("Download complete!");
			_log.Information($"Installed llama.cpp {version} ({asset.Description}) to {installDir}", "Updater");
			string newExePath = Path.Combine(installDir, serverExecutable);
			ValidateCliAfterInstallAsync(newExePath, version);
			_versionCache.ScanAsync(Path.GetDirectoryName(installDir));
			return installDir;
		}
		finally
		{
			foreach (string tempFilePath in tempFiles)
			{
				if (File.Exists(tempFilePath))
				{
					File.Delete(tempFilePath);
				}
			}
			if (Directory.Exists(tempExtractDir))
			{
				try
				{
					Directory.Delete(tempExtractDir, recursive: true);
				}
				catch
				{
				}
			}
		}
	}

	private static string GetServerExecutableName()
	{
		return OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server";
	}

	private void ExtractArchiveFlattened(string archivePath, string targetDir, CancellationToken ct)
	{
		if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
		{
			ExtractTarGzFlattened(archivePath, targetDir, ct);
			return;
		}
		ExtractZipFlattened(archivePath, targetDir, ct);
	}

	private static void ExtractTarGzFlattened(string archivePath, string targetDir, CancellationToken ct)
	{
		using FileStream stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress);
		using TarReader reader = new TarReader(gzip, leaveOpen: false);
		TarEntry? entry;
		while ((entry = reader.GetNextEntry()) != null)
		{
			ct.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(entry.Name) || entry.DataStream == null)
			{
				continue;
			}
			string normalized = entry.Name.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
			if (normalized.Contains(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Unsafe path in tar archive: " + entry.Name);
			}
			string output = Path.Combine(targetDir, Path.GetFileName(normalized));
			using FileStream outputStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None);
			entry.DataStream.CopyTo(outputStream);
		}
	}

	private async Task<GithubAsset?> FindCudaRuntimeArchiveAsync(string version, ReleaseAsset mainAsset)
	{
		string assetNameLower = mainAsset.Name.ToLowerInvariant();
		string cudaMajor;
		if (assetNameLower.Contains("cuda-13") || assetNameLower.Contains("cuda13"))
		{
			cudaMajor = "13";
		}
		else
		{
			if (!assetNameLower.Contains("cuda-12"))
			{
				return null;
			}
			cudaMajor = "12";
		}
		try
		{
			List<GithubAsset> assets = (await _httpClient.GetFromJsonAsync<GithubRelease>("https://api.github.com/repos/ggml-org/llama.cpp/releases/tags/" + version, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false))?.Assets;
			if (assets == null || assets.Count == 0)
			{
				_log.Warning("No assets found for release " + version, "Updater");
				return null;
			}
			_log.Information($"Release {version} has {assets.Count} assets: [{string.Join(", ", assets.Select((GithubAsset a) => a.Name))}]", "Updater");
			GithubAsset githubAsset = assets.FirstOrDefault((GithubAsset a) => a.Name.ToLowerInvariant().Contains("cudart-llama-bin") && a.Name.ToLowerInvariant().Contains("cuda-" + cudaMajor));
			if (githubAsset != null)
			{
				_log.Information("Found CUDA runtime (new format): " + githubAsset.Name, "Updater");
				return githubAsset;
			}
			GithubAsset versionedRuntimeAsset = assets.FirstOrDefault((GithubAsset a) => a.Name.ToLowerInvariant().StartsWith("cudart-") && a.Name.EndsWith(".zip") && (a.Name.ToLowerInvariant().Contains("-v" + cudaMajor + ".") || a.Name.ToLowerInvariant().Contains("-" + cudaMajor + ".")));
			if (versionedRuntimeAsset != null)
			{
				_log.Information("Found CUDA runtime: " + versionedRuntimeAsset.Name, "Updater");
				return versionedRuntimeAsset;
			}
			GithubAsset fallbackRuntimeAsset = assets.FirstOrDefault((GithubAsset a) => a.Name.ToLowerInvariant().StartsWith("cudart-") && a.Name.EndsWith(".zip"));
			if (fallbackRuntimeAsset != null)
			{
				_log.Warning("No exact CUDA " + cudaMajor + " match, using: " + fallbackRuntimeAsset.Name, "Updater");
				return fallbackRuntimeAsset;
			}
			_log.Warning("No CUDA runtime archive found for release " + version, "Updater");
			return null;
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to find CUDA runtime archive: " + ex.Message, "Updater");
			return null;
		}
	}

	private async Task DownloadToFileAsync(string url, string filePath, Action<long, long> totalBytesCallback, CancellationToken ct)
	{
		using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
		await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		await using FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, FileOptions.WriteThrough);
		byte[] buffer = new byte[1048576];
		long downloaded = 0L;
		long lastCallback = 0L;
		DateTime lastCallbackTime = DateTime.UtcNow;
		while (true)
		{
			int read = await stream.ReadAsync(buffer, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (read == 0)
			{
				break;
			}
			await file.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(continueOnCapturedContext: false);
			downloaded += read;
			DateTime utcNow = DateTime.UtcNow;
			if (downloaded - lastCallback >= 10485760 || (utcNow - lastCallbackTime).TotalMilliseconds >= 200.0)
			{
				totalBytesCallback(totalBytes, downloaded);
				lastCallback = downloaded;
				lastCallbackTime = utcNow;
			}
		}
		if (downloaded != lastCallback)
		{
			totalBytesCallback(totalBytes, downloaded);
		}
	}

	private void ExtractZipFlattened(string archivePath, string targetDir, CancellationToken ct)
	{
		using FileStream stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
		string serverEntryPath = null;
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			ct.ThrowIfCancellationRequested();
			if (entry.Name.Equals("llama-server.exe", StringComparison.OrdinalIgnoreCase))
			{
				serverEntryPath = entry.FullName;
				break;
			}
		}
		if (serverEntryPath != null)
		{
			string directoryName = Path.GetDirectoryName(serverEntryPath);
			{
				foreach (ZipArchiveEntry entry in zipArchive.Entries)
				{
					ct.ThrowIfCancellationRequested();
					if (!string.IsNullOrEmpty(entry.FullName))
					{
						string destPath;
						if (!string.IsNullOrEmpty(directoryName) && entry.FullName.StartsWith(directoryName + "/", StringComparison.Ordinal))
						{
							string path = entry.FullName.Substring(directoryName.Length + 1);
							destPath = Path.Combine(targetDir, path);
						}
						else
						{
							destPath = Path.Combine(targetDir, entry.Name);
						}
						string destDir = Path.GetDirectoryName(destPath);
						if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
						{
							Directory.CreateDirectory(destDir);
						}
						if (!string.IsNullOrEmpty(entry.Name))
						{
							entry.ExtractToFile(destPath, overwrite: true);
						}
					}
				}
				return;
			}
		}
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			ct.ThrowIfCancellationRequested();
			if (!string.IsNullOrEmpty(entry.Name))
			{
				string extractPath = Path.Combine(targetDir, entry.Name);
				string extractDir = Path.GetDirectoryName(extractPath);
				if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
				{
					Directory.CreateDirectory(extractDir);
				}
				entry.ExtractToFile(extractPath, overwrite: true);
			}
		}
	}

	private async Task Download7zExtractorAsync(string targetPath)
	{
		using HttpResponseMessage response = await _httpClient.GetAsync("https://www.7-zip.org/a/7zr.exe");
		response.EnsureSuccessStatusCode();
		using Stream stream = await response.Content.ReadAsStreamAsync();
		using FileStream file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
		await stream.CopyToAsync(file);
	}

	private void Extract7z(string archivePath, string targetDir)
	{
		string extractorPath = Path.Combine(Path.GetTempPath(), "llama_studio_7z.exe");
		if (!File.Exists(extractorPath))
		{
			Download7zExtractorAsync(extractorPath).Wait();
		}
		using Process process = Process.Start(new ProcessStartInfo
		{
			FileName = extractorPath,
			Arguments = $"x \"{archivePath}\" -o\"{targetDir}\" -y",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		});
		process?.WaitForExit();
	}

	Task<List<string>> ILlamaUpdater.GetInstalledVersionsAsync(string installDirectory)
	{
		return Task.FromResult((from v in _versionCache.GetAll()
			select Path.GetFileName(v.Directory)).ToList());
	}

	bool ILlamaUpdater.IsVersionInstalledAsync(string installDirectory, string version)
	{
		return _versionCache.IsVersionInstalled(version);
	}

	Task ILlamaUpdater.UninstallVersionAsync(string installDirectory, string version)
	{
		string path = Path.Combine(installDirectory, version);
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
			_log.Information("Uninstalled version " + version, "Updater");
		}
		return Task.CompletedTask;
	}

	private async Task ValidateCliAfterInstallAsync(string newExePath, string newVersion)
	{
		try
		{
			_log.Information("Running post-install CLI validation for " + newVersion + "...", "Updater");
			LlamaHelpInfo newHelp = await _helpParser.ParseAsync(newExePath);
			if (newHelp == null)
			{
				_log.Warning("Post-install validation: failed to parse --help for new version", "Updater");
				return;
			}
			await _cliValidator.SaveSnapshotAsync(newHelp, newVersion);
			LlamaHelpInfo llamaHelpInfo = await _cliValidator.LoadLatestSnapshotAsync();
			if (llamaHelpInfo != null)
			{
				List<string> previousSnapshots = (from f in Directory.GetFiles(_cliValidator.GetSnapshotsDirectory(), "*.json")
					where !f.EndsWith(newVersion.Replace("/", "_").Replace("\\", "_") + ".json")
					orderby File.GetLastWriteTime(f) descending
					select f).ToList();
				string oldVersion = "unknown";
				if (previousSnapshots.Count > 0)
				{
					oldVersion = Path.GetFileNameWithoutExtension(previousSnapshots[0]).Replace("_", "/");
				}
				CliChangeReport cliChangeReport = await _cliValidator.CompareVersionsAsync(llamaHelpInfo, newHelp, oldVersion, newVersion);
				if (cliChangeReport.HasCriticalChanges || cliChangeReport.RemovedFlags.Count > 0 || cliChangeReport.AddedFlags.Count > 0)
				{
					this.CliChangesDetected?.Invoke(this, cliChangeReport);
					_log.Information($"CLI changes detected: {cliChangeReport.CriticalChanges.Count} critical, {cliChangeReport.RemovedFlags.Count} removed, {cliChangeReport.AddedFlags.Count} added", "Updater");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Warning("Post-install CLI validation failed: " + ex.Message, "Updater");
		}
	}

	private void OnProgress(double progress)
	{
		this.DownloadProgress?.Invoke(this, progress);
	}

	private void OnStatus(string message)
	{
		this.StatusMessage?.Invoke(this, message);
	}

	async Task<string> ILlamaUpdater.DownloadAndExtractAsync(string version, string targetDirectory, CancellationToken ct)
	{
		ReleaseAsset releaseAsset = ((await ((ILlamaUpdater)this).FetchReleasesAsync(includePrerelease: true, ct)).FirstOrDefault((LlamaCppRelease r) => r.TagName == version) ?? throw new InvalidOperationException("Release '" + version + "' not found")).Assets.OrderByDescending((ReleaseAsset a) => (a.BuildType != BuildType.Cuda12x && a.BuildType != BuildType.Cuda13x) ? ((a.BuildType == BuildType.Vulkan) ? 1 : 0) : 2).FirstOrDefault();
		if (releaseAsset == null)
		{
			throw new InvalidOperationException("No Windows binary found for this release");
		}
		return await DownloadAndExtractAsync(version, targetDirectory, releaseAsset, ct, ncclEnabled: false);
	}

	async Task<string> ILlamaUpdater.DownloadAndExtractAsync(string version, string targetDirectory, ReleaseAsset asset, CancellationToken ct, bool ncclEnabled)
	{
		return await DownloadAndExtractAsync(version, targetDirectory, asset, ct, ncclEnabled);
	}

	private static string FormatSize(long bytes)
	{
		if (bytes < 1073741824)
		{
			if (bytes < 1048576)
			{
				if (bytes < 1024)
				{
					return $"{bytes} B";
				}
				return $"{(double)bytes / 1024.0:F1} KB";
			}
			return $"{(double)bytes / 1048576.0:F1} MB";
		}
		return $"{(double)bytes / 1073741824.0:F1} GB";
	}
}
