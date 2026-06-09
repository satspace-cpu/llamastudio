using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using Avalonia.Platform;

namespace LlamaStudio.ViewModels;

public partial class LlamaReleasesViewModel : ObservableObject
{
    readonly ILlamaUpdater _updater;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly ILocalizationService _loc;
    readonly IServerManager _serverManager;
    readonly IProfileManager _profileManager;

    [ObservableProperty] ObservableCollection<ReleaseBuildCard> _releaseBuildCards = new();
    [ObservableProperty] ObservableCollection<InstalledVersionItem> _installedVersionItems = new();
    [ObservableProperty] bool _isCheckingUpdates;
    [ObservableProperty] string _updateStatus = "Not checked";
    [ObservableProperty] double _downloadProgress;
    [ObservableProperty] string _downloadStatus = string.Empty;
    [ObservableProperty] string _activeVersion = "";

    public string Title => _loc.T("llama_releases.title");
    public string ReleasesSection => _loc.T("llama_releases.releases_section");
    public string CheckForUpdatesBtn => _loc.T("llama_releases.check_updates");
    public string CheckingStatus => _loc.T("llama_releases.checking");
    public string InstalledVersionsTitle => _loc.T("llama_releases.installed_versions");
    public string RefreshBtn => _loc.T("llama_releases.refresh");
    public string InstallBtn => _loc.T("llama_releases.install");
    public string InstallingBtn => _loc.T("llama_releases.installing");
    public string InstalledBtn => _loc.T("llama_releases.installed");
    public string UseBtn => _loc.T("llama_releases.use");
    public string RemoveBtn => _loc.T("llama_releases.remove");
    public string ActiveBadge => _loc.T("llama_releases.active");
    public string TtInstallBuild => _loc.T("tt.install_build");

    public LlamaReleasesViewModel(ILlamaUpdater updater, ISettings settings, ILogService log, IDialogService dialog, ILocalizationService loc, IServerManager serverManager, IProfileManager profileManager)
    {
        _updater = updater;
        _settings = settings;
        _log = log;
        _dialog = dialog;
        _loc = loc;
        _serverManager = serverManager;
        _profileManager = profileManager;

        _updater.DownloadProgress += (_, progress) =>
        {
            Dispatcher.UIThread.Post(() => DownloadProgress = progress);
        };
        _updater.StatusMessage += (_, msg) =>
        {
            Dispatcher.UIThread.Post(() => DownloadStatus = msg);
        };

        ActiveVersion = _settings.ActiveLlamaCppVersion;

        _ = RefreshInstalledVersions();
        _ = CheckForUpdates(); // Auto-check on page load

        _loc.OnLanguageChanged += (_, _) =>
        {
            foreach (var prop in new[]
            {
                nameof(Title), nameof(ReleasesSection), nameof(CheckForUpdatesBtn), nameof(CheckingStatus),
                nameof(InstalledVersionsTitle), nameof(RefreshBtn), nameof(InstallBtn), nameof(InstallingBtn),
                nameof(InstalledBtn), nameof(UseBtn), nameof(RemoveBtn), nameof(ActiveBadge), nameof(TtInstallBuild)
            })
                OnPropertyChanged(prop);
        };
    }

    [RelayCommand]
    async Task CheckForUpdates()
    {
      IsCheckingUpdates = true;
            UpdateStatus = _loc.T("llama_releases.checking");
            ReleaseBuildCards.Clear();

        try
        {
            var releases = await _updater.FetchReleasesAsync(false);
            var baseDir = GetBaseLlamaDir();

            foreach (var r in releases)
            {
                foreach (var asset in r.Assets)
                {
                    var isInstalled = IsAssetInstalled(baseDir, r.TagName, asset.Name);

                    ReleaseBuildCards.Add(new ReleaseBuildCard
                    {
                        ReleaseTag = r.TagName,
                        ReleaseDate = r.PublishedAt,
                        IsPrerelease = r.IsPrerelease,
                        Asset = asset,
                        BuildType = asset.BuildType,
                        BuildDescription = asset.Description,
                        SizeDisplay = asset.SizeDisplay,
                        AssetName = asset.Name,
                        InstallBtnText = isInstalled ? InstalledBtn : InstallBtn,
                        IsInstalled = isInstalled
                    });
                }
            }

            UpdateStatus = releases.Count > 0
                ? $"{_loc.T("llama_releases.latest")}: {releases[0].TagName}"
                : _loc.T("llama_releases.no_releases");
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Error: {ex.Message}";
            _log.Error(ex, "Update check failed", "LlamaReleases");
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    static bool IsAssetInstalled(string baseDir, string version, string assetName)
    {
        if (!Directory.Exists(baseDir)) return false;

        var matchingDirs = Directory.GetDirectories(baseDir)
            .Where(d => Path.GetFileName(d)!.StartsWith(version + "-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var dir in matchingDirs)
        {
            var infoPath = Path.Combine(dir, "version_info.json");
            if (File.Exists(infoPath))
            {
                try
                {
                    var json = File.ReadAllText(infoPath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("AssetName", out var an))
                    {
                        if (an.GetString() == assetName) return true;
                    }
                }
                catch { }
            }
        }
        return false;
    }

    [RelayCommand]
    async Task InstallBuild(ReleaseBuildCard card)
    {
        if (card.IsInstalled)
            return;

        card.InstallBtnText = InstallingBtn;
        DownloadStatus = $"Downloading {card.ReleaseTag} ({card.BuildDescription})...";
        DownloadProgress = 0;

    // Prevent Windows from throttling CPU when window is not in focus
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;

            try
        {
            var baseDir = GetBaseLlamaDir();
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            var installPath = await _updater.DownloadAndExtractAsync(card.ReleaseTag, baseDir, card.Asset);

            // Store the full version name (e.g., "b9550-cuda12x"), not just the tag
            var installedVersionName = Path.GetFileName(installPath.TrimEnd(Path.DirectorySeparatorChar));
            ActiveVersion = installedVersionName;
            _settings.LlamaCppDirectory = installPath;
            _settings.ActiveLlamaCppVersion = installedVersionName;
            await _settings.SaveAsync();

            DownloadStatus = $"Installed: {card.ReleaseTag} ({card.BuildDescription})";
            DownloadProgress = 100;
            card.InstallBtnText = InstalledBtn;
            card.IsInstalled = true;
            _log.Information($"Installed: {card.ReleaseTag} -> {installPath}", "LlamaReleases");

            await RefreshInstalledVersions();
            await RefreshReleaseCardsAsync();
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Error: {ex.Message}";
            card.InstallBtnText = InstallBtn;
            _log.Error(ex, "Download failed", "LlamaReleases");
        }
        finally
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
        }
    }

    [RelayCommand]
    async Task RefreshInstalledVersions()
    {
        var baseDir = GetBaseLlamaDir();
        _log.Information($"RefreshInstalledVersions scanning: {baseDir}", "LlamaReleases");
        if (!Directory.Exists(baseDir)) 
        {
            _log.Warning($"Base dir not found: {baseDir}", "LlamaReleases");
            return;
        }

        try
        {
            var versions = await _updater.GetInstalledVersionsAsync(baseDir);
            _log.Information($"Found {versions.Count} versions: [{string.Join(", ", versions)}]", "LlamaReleases");
            
            InstalledVersionItems.Clear();
            foreach (var v in versions)
            {
                var (buildType, description) = ReadVersionInfo(baseDir, v);
                if (buildType == BuildType.Unknown && string.IsNullOrEmpty(description))
                {
                    (buildType, description) = InferBuildType(baseDir, v);
                }
                // Match by exact name, or by LlamaCppDirectory path
                bool isActive = v == ActiveVersion;
                if (!isActive && !string.IsNullOrEmpty(_settings.LlamaCppDirectory))
                {
                    var activeDir = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar);
                    var activeName = Path.GetFileName(activeDir);
                    isActive = activeName == v;
                }
                InstalledVersionItems.Add(new InstalledVersionItem(v, isActive, buildType, description));
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to list installed versions", "LlamaReleases");
        }
    }

    async Task RefreshReleaseCardsAsync()
    {
        var baseDir = GetBaseLlamaDir();
        foreach (var card in ReleaseBuildCards.ToList())
        {
            var isInstalled = IsAssetInstalled(baseDir, card.ReleaseTag, card.AssetName);
            card.IsInstalled = isInstalled;
            card.InstallBtnText = isInstalled ? InstalledBtn : InstallBtn;
            OnPropertyChanged(nameof(ReleaseBuildCards));
        }
    }

    static (BuildType buildType, string description) InferBuildType(string baseDir, string version)
    {
        var versionDir = Path.Combine(baseDir, version);
        if (!Directory.Exists(versionDir))
            return (BuildType.Unknown, string.Empty);

        // Try to find any .json file that might contain version info
        var jsonFiles = Directory.GetFiles(versionDir, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(jsonFile);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var bt = BuildType.Unknown;
                var desc = string.Empty;
                if (doc.RootElement.TryGetProperty("BuildType", out var btProp) && Enum.TryParse(btProp.GetString(), true, out bt))
                    ;
                if (doc.RootElement.TryGetProperty("Description", out var descProp))
                    desc = descProp.GetString() ?? string.Empty;
                if (bt != BuildType.Unknown || !string.IsNullOrEmpty(desc))
                    return (bt, desc);
            }
            catch { }
        }

        // Fallback: check if llama-server.exe exists
        if (File.Exists(Path.Combine(versionDir, "llama-server.exe")))
            return (BuildType.Unknown, "Installed");

        return (BuildType.Unknown, string.Empty);
    }

    [RelayCommand]
    async Task SwitchVersion(InstalledVersionItem item)
    {
        if (item == null)
            return;

        var baseDir = GetBaseLlamaDir();
        var newPath = Path.Combine(baseDir, item.Version);

        if (!Directory.Exists(newPath) || !File.Exists(Path.Combine(newPath, "llama-server.exe")))
        {
            _log.Warning($"Version {item.Version} not found or incomplete", "LlamaReleases");
            return;
        }

        // Check if server is running
        var status = await _serverManager.GetStatusAsync();
        bool wasRunning = status.State == ServerState.Running || status.State == ServerState.Starting;

        IDisposable? loadingWin = null;
        if (wasRunning)
        {
            loadingWin = _dialog.ShowLoading(
                string.Format(_loc.T("llama_releases.restart_server"), item.Description),
                "Загрузка");
        }

        try
        {
            if (wasRunning)
            {
                _log.Information($"Stopping server before switching to {item.Version}", "LlamaReleases");
                await _serverManager.StopAsync();

                // Wait for server to actually stop (up to 15 seconds)
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    status = await _serverManager.GetStatusAsync();
                    if (status.State == ServerState.Stopped)
                        break;
                }
            }

            ActiveVersion = item.Version;
            _settings.LlamaCppDirectory = newPath;
            _settings.ActiveLlamaCppVersion = item.Version;
            await _settings.SaveAsync();

            _log.Information($"Switched to version {item.Version} -> {newPath}", "LlamaReleases");

            // Log CUDA DLL status for CUDA builds
            if (item.BuildType == BuildType.Cuda12x || item.BuildType == BuildType.Cuda13x)
            {
                var dlls = Directory.GetFiles(newPath, "*.dll", SearchOption.TopDirectoryOnly);
                _log.Information($"DLLs in {newPath}: [{string.Join(", ", dlls.Select(d => Path.GetFileName(d)))}]", "LlamaReleases");
                var cudartDlls = dlls.Where(d => d.Contains("cudart64")).ToList();
                if (!cudartDlls.Any())
                {
                    var ggmlCuda = dlls.FirstOrDefault(d => d.Contains("ggml-cuda"));
                    var sizeStr = ggmlCuda != null ? $"{Math.Round(new FileInfo(ggmlCuda).Length / 1024.0 / 1024.0, 1)} MB" : "N/A";
                    _log.Warning($"CUDA runtime DLL (cudart64_*.dll) NOT found in {newPath}! ggml-cuda.dll size: {sizeStr}. CUDA may not work — install with internet for automatic cudart download.", "LlamaReleases");
                }
            }

            // Refresh list to update UI immediately
            await RefreshInstalledVersions();

            // Restart server if it was running
            if (wasRunning)
            {
                var profile = await GetActiveProfileAsync();
                if (profile != null && !string.IsNullOrWhiteSpace(profile.ModelPath))
                {
                    _log.Information($"Restarting server with profile: {profile.Name}", "LlamaReleases");
                    await _serverManager.StartAsync(profile);
                    _log.Information($"Server restarted with version {item.Version}", "LlamaReleases");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to switch version to {item.Version}: {ex.Message}", "LlamaReleases");
        }
        finally
        {
            loadingWin?.Dispose();
        }
    }

    async Task<ServerProfile?> GetActiveProfileAsync()
    {
        ServerProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
        profile ??= await _profileManager.GetDefaultProfileAsync();
        return profile;
    }

    [RelayCommand]
    async Task UninstallVersion(InstalledVersionItem item)
    {
        if (item.IsActive)
        {
            await _dialog.ShowErrorAsync(_loc.T("llama_releases.cannot_delete_active"));
            return;
        }

        var baseDir = GetBaseLlamaDir();
        var path = Path.Combine(baseDir, item.Version);
        _log.Information($"Uninstalling {item.Version} from {path}", "LlamaReleases");
        
        try
        {
            await _updater.UninstallVersionAsync(baseDir, item.Version);
            
            if (Directory.Exists(path))
            {
                _log.Error($"Directory still exists after uninstall: {path}", "LlamaReleases");
                await _dialog.ShowErrorAsync(string.Format(_loc.T("llama_releases.delete_failed_folder"), item.Version));
                return;
            }
            
            InstalledVersionItems.Remove(item);
            _log.Information($"Successfully uninstalled {item.Version}", "LlamaReleases");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to uninstall {item.Version}", "LlamaReleases");
            await _dialog.ShowErrorAsync(string.Format(_loc.T("llama_releases.delete_failed_error"), item.Version, ex.Message));
        }
    }

    static (BuildType buildType, string description) ReadVersionInfo(string baseDir, string version)
    {
        var infoPath = Path.Combine(baseDir, version, "version_info.json");
        if (!File.Exists(infoPath))
            return (BuildType.Unknown, string.Empty);

        try
        {
            var json = File.ReadAllText(infoPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var buildType = BuildType.Unknown;
            var description = string.Empty;

            if (doc.RootElement.TryGetProperty("BuildType", out var bt) && Enum.TryParse(bt.GetString(), true, out buildType))
                ; // parsed
            if (doc.RootElement.TryGetProperty("Description", out var desc))
                description = desc.GetString() ?? string.Empty;

            return (buildType, description);
        }
        catch
        {
            return (BuildType.Unknown, string.Empty);
        }
    }

    string GetBaseLlamaDir()
    {
        // 1. Use user-defined base directory if set
        if (!string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory) && Directory.Exists(_settings.LlamaCppBaseDirectory))
        {
            _log.Information($"GetBaseLlamaDir: using base dir {_settings.LlamaCppBaseDirectory}", "LlamaReleases");
            return _settings.LlamaCppBaseDirectory;
        }

        // 2. Default to AppData
        var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
        if (Directory.Exists(defaultDir))
        {
            _log.Information($"GetBaseLlamaDir: using default dir {defaultDir}", "LlamaReleases");
            return defaultDir;
        }

        // 3. Fallback to settings dir
        if (!string.IsNullOrWhiteSpace(_settings.LlamaCppDirectory))
        {
            var currentDir = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var currentName = Path.GetFileName(currentDir);
            if ((currentName.StartsWith("b") || currentName.StartsWith("v") ||
                 currentName.StartsWith("B") || currentName.StartsWith("V")) && Directory.Exists(currentDir))
            {
                var parentDir = Path.GetDirectoryName(currentDir) ?? currentDir;
                _log.Information($"GetBaseLlamaDir: using settings dir {parentDir}", "LlamaReleases");
                return parentDir;
            }
            _log.Information($"GetBaseLlamaDir: using settings dir {currentDir}", "LlamaReleases");
            return currentDir;
        }

        _log.Information($"GetBaseLlamaDir: using default dir {defaultDir}", "LlamaReleases");
        return defaultDir;
    }
}

public class ReleaseBuildCard
{
    public string ReleaseTag { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsPrerelease { get; set; }
    public ReleaseAsset Asset { get; set; } = null!;
    public BuildType BuildType { get; set; }
    public string BuildDescription { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string InstallBtnText { get; set; } = "Install";
    public bool IsInstalled { get; set; }
}

public class InstalledVersionItem
{
    public string Version { get; }
    public bool IsActive { get; set; }
    public BuildType BuildType { get; }
    public string Description { get; }

    public InstalledVersionItem(string version, bool isActive, BuildType buildType = BuildType.Unknown, string description = "")
    {
        Version = version;
        IsActive = isActive;
        BuildType = buildType;
        Description = description;
    }
}
