using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Models;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Views;
using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    readonly IServerManager _serverManager;
    readonly IModelScanner _modelScanner;
    readonly IProfileManager _profileManager;
    readonly ISettings _settings;
    readonly INavigationService _navigation;
    readonly ILocalizationService _loc;
    readonly ILlamaUpdater _updater;
    readonly IAppUpdater _appUpdater;
    readonly IGpuMonitor _gpuMonitor;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly DispatcherTimer _gpuTimer;
    readonly MonitoringViewModel? _monitoringVm;

    [ObservableProperty] ServerStatus _serverStatus = new();
    double _lastTps;
    double _lastPromptTps;
    [ObservableProperty] int _totalModels;
    [ObservableProperty] int _totalProfiles;
    [ObservableProperty] string _activeVersion = "Not installed";
    [ObservableProperty] double _estimatedVramGb;
    [ObservableProperty] double _vramPercentage;
    [ObservableProperty] string _selectedModel = "No model selected";

    // GPU Monitor — real data from nvidia-smi
    [ObservableProperty] GpuInfo? _gpuInfo;
    [ObservableProperty] bool _gpuAvailable;

    public string GpuName => GpuInfo?.Name ?? "N/A";
    public string GpuMemoryInfo => GpuInfo != null
        ? $"{GpuInfo.MemoryTotalGb:F1} GB · Driver {GpuInfo.DriverVersion}"
        : "N/A";
    public string GpuTempText => GpuInfo != null ? $"{GpuInfo.TemperatureCelsius:F0}°C" : "N/A";
    public double GpuTempPercent => GpuInfo != null ? Math.Min(GpuInfo.TemperatureCelsius / 90.0 * 100, 100) : 0;
    public string GpuMemTempText => GpuInfo != null && GpuInfo.MemoryTemperatureCelsius > 0 ? $"{GpuInfo.MemoryTemperatureCelsius:F0}°C" : "N/A";
    public double GpuMemTempPercent => GpuInfo != null && GpuInfo.MemoryTemperatureCelsius > 0 ? Math.Min(GpuInfo.MemoryTemperatureCelsius / 90.0 * 100, 100) : 0;
    public bool HasGpuMemTemp => GpuInfo?.MemoryTemperatureCelsius > 0;

    public SolidColorBrush GpuTempBrush
    {
        get
        {
            double temp = GpuInfo?.TemperatureCelsius ?? 0;
            byte r, g, b;
            if (temp <= 55)
            {
                r = 16; g = 185; b = 129; // Green
            }
            else if (temp <= 70)
            {
                double t = (temp - 55) / 15.0;
                r = (byte)(16 + (251 - 16) * t);
                g = (byte)(185 + (191 - 185) * t);
                b = (byte)(129 + (36 - 129) * t);
            }
            else
            {
                double t = Math.Min((temp - 70) / 20.0, 1.0);
                r = (byte)(251 + (239 - 251) * t);
                g = (byte)(191 + (68 - 191) * t);
                b = (byte)(36 + (68 - 36) * t);
            }
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
    }

    public SolidColorBrush GpuMemTempBrush
    {
        get
        {
            double temp = GpuInfo?.MemoryTemperatureCelsius ?? 0;
            if (temp <= 0) return new SolidColorBrush(Color.FromArgb(0, 148, 163, 184));
            byte r, g, b;
            if (temp <= 55)
            {
                r = 16; g = 185; b = 129;
            }
            else if (temp <= 70)
            {
                double t = (temp - 55) / 15.0;
                r = (byte)(16 + (251 - 16) * t);
                g = (byte)(185 + (191 - 185) * t);
                b = (byte)(129 + (36 - 129) * t);
            }
            else
            {
                double t = Math.Min((temp - 70) / 20.0, 1.0);
                r = (byte)(251 + (239 - 251) * t);
                g = (byte)(191 + (68 - 191) * t);
                b = (byte)(36 + (68 - 36) * t);
            }
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
    }

    public string GpuVramGbText => GpuInfo != null
        ? $"{GpuInfo.MemoryUsedGb:F3} / {GpuInfo.MemoryTotalGb:F3} GB"
        : "N/A";
    public string GpuVramText => GpuInfo != null ? $"{GpuInfo.MemoryPercent:F0}%" : "N/A";
    public string GpuCoreText => GpuInfo != null ? $"{GpuInfo.GpuUtilization:F0}%" : "N/A";
    public string GpuPowerWattsText => GpuInfo != null
        ? $"{GpuInfo.PowerDrawWatts:F0} W / {GpuInfo.PowerLimitWatts:F0} W"
        : "N/A";
    public string GpuPowerText => GpuInfo != null ? $"{GpuInfo.PowerPercent:F0}%" : "N/A";
    public string GpuFanText => GpuInfo != null ? $"{GpuInfo.FanSpeed}%" : "N/A";
    public string GpuClockText => GpuInfo != null ? $"{GpuInfo.ClockMhz} MHz" : "N/A";
    public string GpuThroughputText => _lastTps > 0 ? $"{_lastTps:F1}" : "—";
    public string GpuPromptThroughputText => $"{_lastPromptTps:F1}";
  static readonly double s_totalRamGb = GetTotalPhysicalMemory() / (1024.0 * 1024.0 * 1024.0);
    public string RamUsedText => ServerStatus.RamUsedGb > 0
        ? $"{ServerStatus.RamUsedGb:F1} / {s_totalRamGb:F1} GB"
        : $"— / {s_totalRamGb:F1} GB";
    public string RamPercentText => s_totalRamGb > 0 && ServerStatus.RamUsedGb > 0 ? $"{ServerStatus.RamUsedGb / s_totalRamGb * 100:F0}%" : "—";
    public double RamUsedPercent => s_totalRamGb > 0 && ServerStatus.RamUsedGb > 0 ? Math.Min(ServerStatus.RamUsedGb / s_totalRamGb * 100, 100) : 0;

    static long GetTotalPhysicalMemory()
    {
        try
        {
            // MEMORYSTATUSEX: dwLength(4) + MemoryLoad(4) + TotalPhys(8) = offset 8 for TotalPhys
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(64);
            try
            {
                System.Runtime.InteropServices.Marshal.WriteInt32(ptr, 64); // dwLength
                if (GlobalMemoryStatusEx(ptr))
                    return System.Runtime.InteropServices.Marshal.ReadInt64(ptr, 8); // ullTotalPhys
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr); }
        }
        catch { }

        // Fallback: assume 64GB
        return 64L * 1024 * 1024 * 1024;
    }

    [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(System.IntPtr lpBuffer);
    public string RamUsageLabel => _loc.T("dash.ram_usage");
    public double GpuVramUsage => GpuInfo?.MemoryPercent ?? 0;
    public double GpuCoreUsage => GpuInfo?.GpuUtilization ?? 0;
    public double GpuPowerUsage => GpuInfo?.PowerPercent ?? 0;
    public double GpuFanSpeed => GpuInfo?.FanSpeed ?? 0;
    public string ActiveModelName => SelectedModel;

    [ObservableProperty] bool _hasUpdate;
    [ObservableProperty] string _latestVersion = string.Empty;
    [ObservableProperty] bool _isCheckingVersion;
    [ObservableProperty] bool _isUpdating;
    [ObservableProperty] double _updateProgress;
    [ObservableProperty] bool _showVersionList;
    [ObservableProperty] ObservableCollection<string> _installedVersions = new();

    // App update properties
    [ObservableProperty] string _appVersion = "0.0.0";
    [ObservableProperty] bool _hasAppUpdate;
    [ObservableProperty] bool _isUpdatingApp;
    [ObservableProperty] double _appUpdateProgress;
    [ObservableProperty] string _appLatestVersion = "";

    public string UpdateButtonLabel => IsCheckingVersion ? CheckingVersion : (HasUpdate ? string.Format(_loc.T("dash.update_version"), LatestVersion) : CheckUpdatesBtn);

    // Localized update button label
    public string GetUpdateButtonLabel() => IsCheckingVersion ? CheckingVersion : (HasUpdate ? $"{_loc.T("dash.update")} ({LatestVersion})" : CheckUpdatesBtn);

    public string UpdateBtnText => IsUpdating ? $"{_loc.T("dash.updating")} {UpdateProgress:F0}%" : $"{_loc.T("dash.update")} ({LatestVersion})";

    public string VersionStatusText => IsCheckingVersion ? $"⏳ {CheckingVersion}" : (HasUpdate ? $"↑ {UpdateAvailable}, {LatestVersion}" : $"✓ {UpToDate}");

    partial void OnHasUpdateChanged(bool value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); OnPropertyChanged(nameof(UpdateBtnText)); }
    partial void OnHasAppUpdateChanged(bool value) { OnPropertyChanged(nameof(AppUpdateStatus)); OnPropertyChanged(nameof(AppUpdateBtnText)); }
    partial void OnAppLatestVersionChanged(string value) { OnPropertyChanged(nameof(AppUpdateStatus)); OnPropertyChanged(nameof(AppUpdateBtnText)); }
    partial void OnIsUpdatingAppChanged(bool value) { OnPropertyChanged(nameof(AppUpdateBtnText)); }
    partial void OnAppUpdateProgressChanged(double value) { OnPropertyChanged(nameof(AppUpdateBtnText)); }
    partial void OnLatestVersionChanged(string value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); OnPropertyChanged(nameof(UpdateBtnText)); }
    partial void OnIsCheckingVersionChanged(bool value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); }
    partial void OnIsUpdatingChanged(bool value) => OnPropertyChanged(nameof(UpdateBtnText));
    partial void OnUpdateProgressChanged(double value) => OnPropertyChanged(nameof(UpdateBtnText));
    partial void OnActiveVersionChanged(string value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); OnPropertyChanged(nameof(UpdateBtnText)); }

    public string SwitchVersionBtn => _loc.T("llama_releases.switch_version");

    [RelayCommand]
    void ToggleVersionList()
    {
        ShowVersionList = !ShowVersionList;
    }

    [RelayCommand]
    async Task SwitchVersionAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return;

        var baseDir = GetBaseLlamaDir();
        var newPath = Path.Combine(baseDir, version);

        if (!Directory.Exists(newPath) || !File.Exists(Path.Combine(newPath, "llama-server.exe")))
        {
            _log.Warning($"Version {version} not found or incomplete", "Dashboard");
            return;
        }

        // Check if server is running BEFORE changing settings
        bool wasRunning = ServerStatus.State == ServerState.Running || ServerStatus.State == ServerState.Starting;

        IDisposable? loadingWin = null;
        if (wasRunning)
        {
            loadingWin = _dialog.ShowLoading(
                string.Format(_loc.T("dash.switching_version"), version),
                _loc.T("dash.restart_server"));
        }

        try
        {
            // Stop current server if running
            if (wasRunning)
            {
                _log.Information($"Stopping server to switch version to {version}", "Dashboard");
                await _serverManager.StopAsync();

                // Wait for server to actually stop (up to 15 seconds)
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    var status = await _serverManager.GetStatusAsync();
                    if (status.State == ServerState.Stopped)
                        break;
                }
            }

            // Apply version change
            ActiveVersion = version;
            SelectedVersion = version;
            _settings.LlamaCppDirectory = newPath;
            _settings.ActiveLlamaCppVersion = version;
            await _settings.SaveAsync();
            _log.Information($"Switched to version {version} -> {newPath}", "Dashboard");

            // Restart server if it was running
            if (wasRunning)
            {
                var profile = await GetActiveProfileAsync();
                if (profile != null && !string.IsNullOrWhiteSpace(profile.ModelPath))
                {
                    _log.Information($"Restarting server with profile: {profile.Name}", "Dashboard");
                    await _serverManager.StartAsync(profile);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to switch version to {version}: {ex.Message}", "Dashboard");
        }
        finally
        {
            loadingWin?.Dispose();
        }
    }

    // Selected version for ComboBox binding
    [ObservableProperty] string? _selectedVersion;
    partial void OnSelectedVersionChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != ActiveVersion)
        {
            _ = SwitchVersionAsync(value);
        }
    }

    static string GetBaseLlamaDir()
    {
        // Try to get base directory from settings
        var roamingDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(roamingDir, "LlamaStudio");
        return Directory.Exists(baseDir) ? baseDir : string.Empty;
    }

    async Task RefreshInstalledVersionsAsync()
    {
        var baseDir = GetBaseLlamaDir();
        if (!Directory.Exists(baseDir)) return;

        try
        {
            var versions = Directory.GetDirectories(baseDir)
                .Select(d => Path.GetFileName(d))
                .Where(n => n.StartsWith("b") || n.StartsWith("v"))
                .OrderByDescending(n => n)
                .ToList();

            InstalledVersions.Clear();
            foreach (var v in versions)
                InstalledVersions.Add(v);

            // Set selected version to match current active version
            // Try exact match first, then match by LlamaCppDirectory path
            if (!string.IsNullOrWhiteSpace(ActiveVersion))
            {
                SelectedVersion = InstalledVersions.FirstOrDefault(v => v == ActiveVersion);
                if (SelectedVersion == null && !string.IsNullOrEmpty(_settings.LlamaCppDirectory))
                {
                    var activeDir = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar);
                    var activeName = Path.GetFileName(activeDir);
                    SelectedVersion = InstalledVersions.FirstOrDefault(v => v == activeName);
                    if (SelectedVersion != null)
                    {
                        // Update ActiveVersion to the full name for consistency
                        ActiveVersion = SelectedVersion;
                        _settings.ActiveLlamaCppVersion = SelectedVersion;
                        await _settings.SaveAsync();
                    }
                }
            }
        }
        catch { }
    }

    // Translated strings
    public string Title => _loc.T("dash.title");
    public string ServerLabel => _loc.T("dash.server_label");
    public string ModelsCard => _loc.T("main.models");
    public string Indexed => _loc.T("dash.indexed");
    public string ProfilesCard => _loc.T("main.profiles");
    public string Saved => _loc.T("dash.saved");
    public string LlamaCppCard => _loc.T("dash.llama_cpp");
    public string ActiveBadge => _loc.T("dash.active");
    public string VramUsage => _loc.T("dash.vram_usage");
    public string GpuMemory => _loc.T("dash.gpu_memory");
    public string ActiveModelLabel => _loc.T("dash.active_model");
    public string QuickActions => _loc.T("dash.quick_actions");
    public string ScanModelsBtn => _loc.T("dash.scan_models");
    public string DiscoverGguf => _loc.T("dash.discover_gguf");
    public string CreateProfileBtn => _loc.T("dash.create_profile");
    public string LaunchConfig => _loc.T("dash.launch_config");
    public string CheckUpdatesBtn => _loc.T("dash.check_updates");
    public string LlamaVersionLabel => _loc.T("dash.llama_version");
    public string UpToDate => _loc.T("dash.up_to_date");
    public string UpdateAvailable => _loc.T("dash.update_available");
    public string CheckingVersion => _loc.T("dash.checking_version");
    public string GpuMonitorTitle => _loc.T("dash.gpu_monitor");
    public string GpuLive => _loc.T("dash.live");
    public string GpuVramLabel => _loc.T("dash.vram_label");
    public string GpuCoreLabel => _loc.T("dash.gpu_core");
    public string GpuPowerLabel => _loc.T("dash.power_draw");
    public string GpuFanLabel => _loc.T("dash.fan_speed");
    public string GpuClockLabel => _loc.T("dash.clock");
    public string GpuModelLabel => _loc.T("dash.model");
    public string GpuThroughputLabel => _loc.T("dash.throughput");
    public string GpuPromptThroughputLabel => _loc.T("dash.prompt_throughput");
    public string GpuTempLabel => _loc.T("dash.gpu_temperature");
    public string GpuMemTempLabel => _loc.T("dash.mem_temperature");
    public bool ServerStopped => ServerStatus.State == Core.Enums.ServerState.Stopped;
    public bool ServerRunning => ServerStatus.State == Core.Enums.ServerState.Running || ServerStatus.State == Core.Enums.ServerState.Starting;
    public string StartServerBtn => _loc.T("server.start_btn");
    public string StopServerBtn => _loc.T("server.stop_btn");
    public string MonitoringBtn => _loc.T("main.monitoring");

    // App update strings
    public string AppVersionLabel => _loc.T("dash.app_version");
    public string AppUpdateBtnText => IsUpdatingApp ? $"{_loc.T("dash.downloading")} {AppUpdateProgress:F0}%" : $"{_loc.T("dash.update")} (v{AppLatestVersion})";
    public string AppUpdateStatus => HasAppUpdate ? $"↑ {_loc.T("dash.new_version")} {AppLatestVersion}" : $"✓ {_loc.T("dash.up_to_date")}";

    // Profile selection
    [ObservableProperty] System.Collections.ObjectModel.ObservableCollection<Core.Models.ServerProfile> _allProfiles = new();
    [ObservableProperty] Core.Models.ServerProfile? _selectedProfile;
    [ObservableProperty] Core.Models.ServerProfile? _currentProfile;

    public string ProfileName => CurrentProfile?.Name ?? _loc.T("dash.no_profile");
    public bool IsDefaultProfile => CurrentProfile?.IsDefault == true;
    public bool CanSetDefault => SelectedProfile != null && !IsDefaultProfile;
    public string ProfileMainModel => CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.ModelPath)
        ? Path.GetFileName(CurrentProfile.ModelPath) : "—";
    public bool HasMmproj => CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.MmprojPath);
    public string MmprojName => HasMmproj ? Path.GetFileName(CurrentProfile!.MmprojPath!) : "—";
    public bool HasDraft => CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.DraftModelPath);
    public string DraftName => HasDraft ? Path.GetFileName(CurrentProfile!.DraftModelPath!) : "—";
    public string ProfileContextText => CurrentProfile != null && CurrentProfile.ContextSize > 0
        ? $"{CurrentProfile.ContextSize:N0}" : "—";
    public string ProfileTempText => CurrentProfile?.Temperature.ToString("F2") ?? "—";
    public bool ProfileHasMtp => CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.SpecType);
    public bool ProfileHasReasoning => CurrentProfile?.Reasoning == true;
    public string ProfileGpuLayersText => CurrentProfile?.GpuLayers ?? "all";
    public bool ProfileFlashAttention => CurrentProfile?.FlashAttention == true;
    public bool ProfileCachePrompt => CurrentProfile?.CachePrompt == true;
    public bool ProfileContBatching => CurrentProfile?.ContBatching == true;
    public int ProfileThreads => CurrentProfile?.Threads > 0 ? CurrentProfile.Threads : 0;
    public string ProfileThreadsText => ProfileThreads > 0 ? ProfileThreads.ToString() : "—";

    public string ProfileTitle => _loc.T("dash.profile");
    public string ProfileMainLabel => _loc.T("dash.main_model");
    public string ProfileMmprojLabel => _loc.T("dash.mmproj");
    public string ProfileDraftLabel => _loc.T("dash.draft_model");
    public string ProfileContextLabel => _loc.T("dash.context");
    public string ProfileTempLabel => _loc.T("dash.temperature_label");
    public string ProfileGpuLayersLabel => _loc.T("dash.gpu_layers");
    public string ProfileThreadsLabel => _loc.T("dash.threads");
    public string ProfileMtpBadge => _loc.T("dash.mtp");
    public string ProfileReasoningBadge => _loc.T("dash.reasoning");
    public string ProfileFlashAttnBadge => _loc.T("dash.flash_attn");
    public string ProfileCacheBadge => _loc.T("dash.cache_prompt");
    public string ProfileBatchingBadge => _loc.T("dash.cont_batching");
    public string SelectProfileLabel => _loc.T("dash.select_profile");
    public string DefaultBadge => _loc.T("dash.default");
    public string SetDefaultBtn => _loc.T("dash.set_default");
    public string SwitchProfileBtn => _loc.T("dash.switch_profile_btn");
    public string ActiveProfileLabel => _loc.T("dash.active_profile_label");
    public string ActiveProfileName => CurrentProfile?.Name ?? "—";

    void RaiseProfileProps()
    {
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(ProfileMainModel));
        OnPropertyChanged(nameof(HasMmproj));
        OnPropertyChanged(nameof(MmprojName));
        OnPropertyChanged(nameof(HasDraft));
        OnPropertyChanged(nameof(DraftName));
        OnPropertyChanged(nameof(ProfileContextText));
        OnPropertyChanged(nameof(ProfileTempText));
        OnPropertyChanged(nameof(ProfileHasMtp));
        OnPropertyChanged(nameof(ProfileHasReasoning));
        OnPropertyChanged(nameof(ProfileGpuLayersText));
        OnPropertyChanged(nameof(ProfileFlashAttention));
        OnPropertyChanged(nameof(ProfileCachePrompt));
        OnPropertyChanged(nameof(ProfileContBatching));
        OnPropertyChanged(nameof(ProfileThreads));
        OnPropertyChanged(nameof(ProfileThreadsText));
        OnPropertyChanged(nameof(IsDefaultProfile));
        OnPropertyChanged(nameof(CanSetDefault));
        OnPropertyChanged(nameof(ActiveProfileName));
    }

    partial void OnSelectedProfileChanged(Core.Models.ServerProfile? value)
    {
        CurrentProfile = value;
        if (value != null)
        {
            _ = SaveSelectedProfileIdAsync(value.Id.ToString());
            _profileManager.NotifyProfileChanged(value.Id.ToString());
        }
    }

    async Task SaveSelectedProfileIdAsync(string profileId)
    {
        try
        {
            _settings.LastSelectedProfileId = profileId;
            await _settings.SaveAsync();
        }
        catch
        {
            // silently fail — settings save is best-effort
        }
    }
    partial void OnCurrentProfileChanged(Core.Models.ServerProfile? value) => RaiseProfileProps();

    [RelayCommand]
    async Task SetDefaultProfileAsync()
    {
        if (SelectedProfile != null)
        {
            await _profileManager.SetDefaultProfileAsync(SelectedProfile.Id);
            var profiles = await _profileManager.GetAllProfilesAsync();
            AllProfiles.Clear();
            foreach (var p in profiles)
                AllProfiles.Add(p);
            var def = await _profileManager.GetDefaultProfileAsync();
            SelectedProfile = def;
        }
    }

    [RelayCommand]
    async Task SwitchProfileAsync()
    {
        if (SelectedProfile == null)
            return;

        // Apply host/port from settings
        if (!string.IsNullOrWhiteSpace(_settings.DefaultHost))
            SelectedProfile.Host = _settings.DefaultHost;
        if (_settings.DefaultPort > 0)
            SelectedProfile.Port = _settings.DefaultPort;

        // Stop current server if running
        if (ServerStatus.State == Core.Enums.ServerState.Running || ServerStatus.State == Core.Enums.ServerState.Starting)
        {
            await _serverManager.StopAsync();
        }

        // Start with selected profile
        await _serverManager.StartAsync(SelectedProfile);
    }

    public DashboardViewModel(
        IServerManager serverManager,
        IModelScanner modelScanner,
        IProfileManager profileManager,
        ISettings settings,
        INavigationService navigation,
        ILocalizationService loc,
        ILlamaUpdater updater,
        IAppUpdater appUpdater,
        IGpuMonitor gpuMonitor,
        ILogService log,
        IDialogService dialog,
        MonitoringViewModel? monitoringVm = null)
    {
        _serverManager = serverManager;
        _modelScanner = modelScanner;
        _profileManager = profileManager;
        _settings = settings;
        _navigation = navigation;
        _loc = loc;
        _updater = updater;
        _appUpdater = appUpdater;
        _gpuMonitor = gpuMonitor;
        _log = log;
        _dialog = dialog;
        _monitoringVm = monitoringVm;

        GpuAvailable = gpuMonitor.IsAvailable;

        // Init app version
        AppVersion = _appUpdater.GetCurrentVersion();

        _gpuTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _gpuTimer.Tick += (_, _) => _ = RefreshGpuAsync();
        _gpuTimer.Start();

        _serverManager.StatusChanged += (s, status) => ServerStatus = status;
        _loc.OnLanguageChanged += OnLanguageChanged;

        // Auto-check for app updates on startup (non-blocking, fire-and-forget)
        if (_settings.AutoCheckUpdates)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000); // Delay to not block startup
                try
                {
                    var update = await _appUpdater.CheckForUpdatesAsync();
                    if (update != null)
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            HasAppUpdate = true;
                            AppLatestVersion = update.Version;
                        });
                        _log.Information($"App update available: {update.Version}", "Dashboard");
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning($"App update check skipped: {ex.Message}", "Dashboard");
                }
            });
        }
    }

    public void Dispose()
    {
        _gpuTimer.Stop();
    }

    void OnLanguageChanged(object? sender, string language)
    {
        foreach (var prop in new[]
        {
            nameof(Title), nameof(ServerLabel), nameof(ModelsCard), nameof(Indexed),
            nameof(ProfilesCard), nameof(Saved), nameof(LlamaCppCard), nameof(ActiveBadge),
            nameof(VramUsage), nameof(GpuMemory), nameof(ActiveModelLabel), nameof(QuickActions),
            nameof(ScanModelsBtn), nameof(DiscoverGguf), nameof(CreateProfileBtn), nameof(LaunchConfig),
            nameof(CheckUpdatesBtn), nameof(LlamaVersionLabel), nameof(UpToDate),
            nameof(UpdateAvailable), nameof(CheckingVersion),
            nameof(GpuMonitorTitle), nameof(GpuLive), nameof(GpuVramLabel), nameof(GpuCoreLabel),
                nameof(GpuPowerLabel), nameof(GpuFanLabel), nameof(GpuTempLabel), nameof(GpuMemTempLabel), nameof(GpuClockLabel), nameof(GpuModelLabel),
            nameof(ServerStopped), nameof(ServerRunning), nameof(StartServerBtn), nameof(StopServerBtn),
            nameof(GpuThroughputText), nameof(GpuThroughputLabel),
            nameof(GpuPromptThroughputText), nameof(GpuPromptThroughputLabel),
            nameof(ProfileName), nameof(ProfileTitle), nameof(ProfileMainLabel), nameof(ProfileMmprojLabel),
            nameof(ProfileDraftLabel), nameof(ProfileContextLabel), nameof(ProfileTempLabel),
            nameof(ProfileGpuLayersLabel), nameof(ProfileThreadsLabel), nameof(ProfileMtpBadge),
            nameof(ProfileReasoningBadge), nameof(ProfileFlashAttnBadge), nameof(ProfileCacheBadge),
            nameof(ProfileBatchingBadge), nameof(SelectProfileLabel), nameof(DefaultBadge), nameof(SetDefaultBtn),
       nameof(SwitchProfileBtn), nameof(ActiveProfileLabel), nameof(ActiveProfileName),
            nameof(SwitchVersionBtn)
        })
            OnPropertyChanged(prop);
        OnPropertyChanged(nameof(GetUpdateButtonLabel));
    }

    partial void OnServerStatusChanged(ServerStatus value)
    {
      // Always sync — ServerManager now persists last TPS when idle
        _lastTps = value.TokensPerSecond;
        _lastPromptTps = value.PromptTokensPerSecond;

        EstimatedVramGb = value.VramUsedGb;
        VramPercentage = value.VramUsedGb > 0
            ? Math.Min(value.VramUsedGb / 24.0 * 100, 100)
            : 0;
        SelectedModel = value.ModelName ?? "No model selected";
        OnPropertyChanged(nameof(ServerStopped));
        OnPropertyChanged(nameof(ServerRunning));
        OnPropertyChanged(nameof(GpuThroughputText));
        OnPropertyChanged(nameof(GpuPromptThroughputText));
        OnPropertyChanged(nameof(RamUsedText));
        OnPropertyChanged(nameof(RamPercentText));
        OnPropertyChanged(nameof(RamUsedPercent));
    }

    [RelayCommand]
    void Navigate(string page)
    {
        _navigation.Navigate(page);
    }

    [RelayCommand]
    async Task StartServerAsync()
    {
        // Use active profile, not default!
        var profile = await GetActiveProfileAsync();
        if (profile != null)
        {
            if (!string.IsNullOrWhiteSpace(_settings.DefaultHost))
                profile.Host = _settings.DefaultHost;
            if (_settings.DefaultPort > 0)
                profile.Port = _settings.DefaultPort;

            await _serverManager.StartAsync(profile);
        }
    }

    async Task<Core.Models.ServerProfile?> GetActiveProfileAsync()
    {
        Core.Models.ServerProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
        profile ??= await _profileManager.GetDefaultProfileAsync();
        return profile;
    }

    [RelayCommand]
    async Task StopServerAsync()
    {
        await _serverManager.StopAsync();
    }

    [RelayCommand]
    async Task CheckVersionAsync()
    {
        IsCheckingVersion = true;
        try
        {
            var latest = await _updater.GetLatestReleaseAsync(false);
            if (latest != null)
            {
                LatestVersion = latest.TagName;
                // Compare only the version tag, ignoring build suffix (e.g., b9544 vs b9544-cuda12x)
                var currentTag = ActiveVersion?.Split('-').FirstOrDefault() ?? string.Empty;
                HasUpdate = !string.Equals(currentTag, latest.TagName, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // silently fail — network may be unavailable
        }
        finally
        {
            IsCheckingVersion = false;
        }

        // Always refresh installed versions list (even if update check failed)
        await RefreshInstalledVersionsAsync();
    }

    [RelayCommand]
    void OpenMonitoring()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (lifetime == null) return;

            // Refresh settings before showing
            _monitoringVm?.RefreshFromSettings();

            var existing = lifetime.Windows.FirstOrDefault(w => w is Views.MonitoringWindow);
            if (existing != null)
            {
                existing.Show();
                existing.Activate();
            }
            else
            {
                var win = new Views.MonitoringWindow(_monitoringVm!);
                win.Show(lifetime.MainWindow);
            }
        }
        catch (Exception ex)
        {
         }
    }

    [RelayCommand]
    async Task UpdateLlamaCppAsync()
    {
        if (string.IsNullOrWhiteSpace(LatestVersion))
            return;

        IsUpdating = true;
        UpdateProgress = 0;

        try
        {
            // Subscribe to progress events
            _updater.DownloadProgress += (s, progress) => UpdateProgress = progress;

           // Determine base directory — always use LlamaStudio
            string baseDir = GetBaseLlamaDir();
            if (!Directory.Exists(baseDir))
            {
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
                Directory.CreateDirectory(baseDir);
            }

            var installPath = await _updater.DownloadAndExtractAsync(LatestVersion, baseDir);

            // Store the full version name (e.g., "b9550-cuda12x"), not just the tag
            var installedVersionName = Path.GetFileName(installPath.TrimEnd(Path.DirectorySeparatorChar));
            _settings.LlamaCppDirectory = installPath;
            _settings.ActiveLlamaCppVersion = installedVersionName;
            await _settings.SaveAsync();

            ActiveVersion = installedVersionName;
            SelectedVersion = installedVersionName;
            HasUpdate = false;

            // Refresh installed versions list
            await RefreshInstalledVersionsAsync();

            // Ask user if they want to restart the server with the new version
            var serverRunning = ServerStatus.State == ServerState.Running;
            if (serverRunning)
            {
                var restart = await _dialog.ShowConfirmationAsync(
                    string.Format(_loc.T("dash.update_restart_server"), installedVersionName),
                    _loc.T("dash.update_installed"));
                if (restart)
                {
                    await RestartServerAfterUpdateAsync();
                }
            }
            else
            {
                await _dialog.ShowSuccessAsync(
                    string.Format(_loc.T("dash.update_installed_msg"), installedVersionName),
                    _loc.T("dash.update_installed"));
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            IsUpdating = false;
            _updater.DownloadProgress -= (s, progress) => UpdateProgress = progress;
        }
    }

    async Task RestartServerAfterUpdateAsync()
    {
        try
        {
            // Stop the server
            await _serverManager.StopAsync();
            
            // Wait for server to fully stop
            await Task.Delay(1000);

            // Get the current profile and restart
            var profile = CurrentProfile ?? SelectedProfile;
            if (profile != null)
            {
                await _serverManager.StartAsync(profile);
                
                // Update status
                ServerStatus = await _serverManager.GetStatusAsync();
                
                await _dialog.ShowSuccessAsync(
                    _loc.T("dash.server_restarted"),
                    _loc.T("dash.success"));
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(
                $"{_loc.T("dash.restart_failed")}: {ex.Message}",
                _loc.T("dash.error"));
        }
    }

    async Task CheckAppUpdatesAsync()
    {
        try
        {
            var update = await _appUpdater.CheckForUpdatesAsync();
            if (update != null)
            {
                HasAppUpdate = true;
                AppLatestVersion = update.Version;
                _log.Information($"App update available: {update.Version}", "Dashboard");
            }
        }
        catch { }
    }

    [RelayCommand]
    async Task UpdateAppAsync()
    {
        try
        {
            IsUpdatingApp = true;
            AppUpdateProgress = 0;

            // Subscribe to progress events
            _appUpdater.ProgressChanged += (s, progress) => AppUpdateProgress = progress;

            // Check for updates
            var update = await _appUpdater.CheckForUpdatesAsync();
            if (update == null)
            {
                IsUpdatingApp = false;
                return;
            }

            AppLatestVersion = update.Version;
            HasAppUpdate = true;

            // Download update to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            var downloadedPath = await _appUpdater.DownloadUpdateAsync(update, tempDir);

            // Prepare auto-restart: create a batch file to replace and restart
            var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(currentExe))
            {
                // Create a restart script
                var restartScript = Path.Combine(tempDir, "restart.bat");
                File.WriteAllText(restartScript, $@"
@echo off
timeout /t 2 /nobreak >nul
copy /y ""{downloadedPath}"" ""{currentExe}"" >nul
start """" ""{currentExe}""
del ""{restartScript}""
rmdir /q /s ""{tempDir}""
");

                // Start the restart script
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = restartScript,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                });

                // Exit current app
                _log.Information($"App update downloaded, restarting...", "Dashboard");
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"App update failed: {ex.Message}", "Dashboard");
        }
        finally
        {
            IsUpdatingApp = false;
        }
    }

    public async Task RefreshAsync()
    {
        ServerStatus = await _serverManager.GetStatusAsync();

        if (!string.IsNullOrWhiteSpace(_settings.ModelsDirectory) &&
            Directory.Exists(_settings.ModelsDirectory))
        {
            var models = await _modelScanner.ScanDirectoryAsync(_settings.ModelsDirectory);
            TotalModels = models.Count;
        }

        var profiles = await _profileManager.GetAllProfilesAsync();
        TotalProfiles = profiles.Count;

        AllProfiles.Clear();
        foreach (var p in profiles)
            AllProfiles.Add(p);

        // Restore last selected profile, fallback to default — выбираем из коллекции (по ссылке!)
        ServerProfile? initialProfile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
        {
            initialProfile = AllProfiles.FirstOrDefault(p => p.Id == _settings.LastSelectedProfileId);
        }
        if (initialProfile == null)
        {
            initialProfile = AllProfiles.FirstOrDefault(p => p.IsDefault) ?? AllProfiles.FirstOrDefault();
        }

        // Only trigger selection if profile actually changed — avoids unnecessary server restarts on navigation
        if (SelectedProfile?.Id != initialProfile?.Id)
        {
            SelectedProfile = initialProfile;
        }
        CurrentProfile = initialProfile;
        SelectedModel = initialProfile?.ModelPath != null
            ? Path.GetFileName(initialProfile.ModelPath)
            : "No model selected";

        if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
            ActiveVersion = _settings.ActiveLlamaCppVersion;

        // Refresh installed versions immediately
        await RefreshInstalledVersionsAsync();

        EstimatedVramGb = ServerStatus.VramUsedGb;
        VramPercentage = ServerStatus.VramUsedGb > 0
            ? Math.Min(ServerStatus.VramUsedGb / 24.0 * 100, 100)
            : 0;

        RaiseProfileProps();

        _ = CheckVersionAsync();
    }

    async Task RefreshGpuAsync()
    {
        try
        {
            var info = await _gpuMonitor.GetGpuInfoAsync();
            if (info != null)
            {
                GpuInfo = info;
                OnPropertyChanged(nameof(GpuName));
                OnPropertyChanged(nameof(GpuMemoryInfo));
                OnPropertyChanged(nameof(GpuTempText));
                OnPropertyChanged(nameof(GpuVramGbText));
                OnPropertyChanged(nameof(GpuVramText));
                OnPropertyChanged(nameof(GpuCoreText));
                OnPropertyChanged(nameof(GpuPowerWattsText));
                OnPropertyChanged(nameof(GpuPowerText));
                OnPropertyChanged(nameof(GpuFanText));
                OnPropertyChanged(nameof(GpuClockText));
                OnPropertyChanged(nameof(GpuTempPercent));
                OnPropertyChanged(nameof(GpuTempBrush));
                OnPropertyChanged(nameof(GpuMemTempText));
                OnPropertyChanged(nameof(GpuMemTempPercent));
                OnPropertyChanged(nameof(GpuMemTempBrush));
                OnPropertyChanged(nameof(HasGpuMemTemp));
                OnPropertyChanged(nameof(GpuVramUsage));
                OnPropertyChanged(nameof(GpuCoreUsage));
                OnPropertyChanged(nameof(GpuPowerUsage));
                OnPropertyChanged(nameof(GpuFanSpeed));
            }
        }
        catch
        {
            // silently fail — nvidia-smi may be temporarily unavailable
        }
    }
}