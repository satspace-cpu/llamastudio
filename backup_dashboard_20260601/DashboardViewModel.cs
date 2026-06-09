using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hermass.Core.Enums;
using Hermass.Core.Models;
using Hermass.Core.Interfaces;

namespace Hermass.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    readonly IServerManager _serverManager;
    readonly IModelScanner _modelScanner;
    readonly IProfileManager _profileManager;
    readonly ISettings _settings;
    readonly INavigationService _navigation;
    readonly ILocalizationService _loc;
    readonly ILlamaUpdater _updater;

    [ObservableProperty] ServerStatus _serverStatus = new();
    [ObservableProperty] int _totalModels;
    [ObservableProperty] int _totalProfiles;
    [ObservableProperty] string _activeVersion = "Not installed";
    [ObservableProperty] double _estimatedVramGb;
    [ObservableProperty] double _vramPercentage;
    [ObservableProperty] string _selectedModel = "No model selected";
    [ObservableProperty] bool _hasUpdate;
    [ObservableProperty] string _latestVersion = string.Empty;
    [ObservableProperty] bool _isCheckingVersion;
    [ObservableProperty] bool _isUpdating;
    [ObservableProperty] double _updateProgress;

    public string UpdateButtonLabel => IsCheckingVersion ? CheckingVersion : (HasUpdate ? $"Обновить ({LatestVersion})" : CheckUpdatesBtn);

    // Localized update button label
    public string GetUpdateButtonLabel() => IsCheckingVersion ? CheckingVersion : (HasUpdate ? $"{_loc.T("dash.update")} ({LatestVersion})" : CheckUpdatesBtn);

    public string UpdateBtnText => IsUpdating ? $"{_loc.T("dash.updating")} {UpdateProgress:F0}%" : $"{_loc.T("dash.update")} ({LatestVersion})";

    public string VersionStatusText => IsCheckingVersion ? $"⏳ {CheckingVersion}" : (HasUpdate ? $"↑ {UpdateAvailable}, {LatestVersion}" : $"✓ {UpToDate}");

    partial void OnHasUpdateChanged(bool value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); OnPropertyChanged(nameof(UpdateBtnText)); }
    partial void OnLatestVersionChanged(string value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); OnPropertyChanged(nameof(UpdateBtnText)); }
    partial void OnIsCheckingVersionChanged(bool value) { OnPropertyChanged(nameof(UpdateButtonLabel)); OnPropertyChanged(nameof(VersionStatusText)); }
    partial void OnIsUpdatingChanged(bool value) => OnPropertyChanged(nameof(UpdateBtnText));
    partial void OnUpdateProgressChanged(double value) => OnPropertyChanged(nameof(UpdateBtnText));

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

    public DashboardViewModel(
        IServerManager serverManager,
        IModelScanner modelScanner,
        IProfileManager profileManager,
        ISettings settings,
        INavigationService navigation,
        ILocalizationService loc,
        ILlamaUpdater updater)
    {
        _serverManager = serverManager;
        _modelScanner = modelScanner;
        _profileManager = profileManager;
        _settings = settings;
        _navigation = navigation;
        _loc = loc;
        _updater = updater;

        _serverManager.StatusChanged += (s, status) => ServerStatus = status;
        _loc.OnLanguageChanged += OnLanguageChanged;
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
            nameof(UpdateAvailable), nameof(CheckingVersion)
        })
            OnPropertyChanged(prop);
        OnPropertyChanged(nameof(GetUpdateButtonLabel));
    }

    partial void OnServerStatusChanged(ServerStatus value)
    {
        EstimatedVramGb = value.VramUsedGb;
        VramPercentage = value.VramUsedGb > 0
            ? Math.Min(value.VramUsedGb / 24.0 * 100, 100)
            : 0;
        SelectedModel = value.ModelName ?? "No model selected";
    }

    [RelayCommand]
    void Navigate(string page)
    {
        _navigation.Navigate(page);
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
                HasUpdate = !string.Equals(ActiveVersion, latest.TagName, StringComparison.OrdinalIgnoreCase);
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

            // Determine base directory
            string baseDir;
            if (string.IsNullOrWhiteSpace(_settings.LlamaCppDirectory))
            {
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HermassLlamaServer");
            }
            else
            {
                var currentDir = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar);
                var currentName = Path.GetFileName(currentDir);
                if ((currentName.StartsWith("b") || currentName.StartsWith("v") ||
                     currentName.StartsWith("B") || currentName.StartsWith("V")) &&
                    Directory.Exists(currentDir))
                {
                    baseDir = Path.GetDirectoryName(currentDir) ?? currentDir;
                }
                else
                {
                    baseDir = currentDir;
                }
            }

            var installPath = await _updater.DownloadAndExtractAsync(LatestVersion, baseDir);

            // Update settings
            _settings.LlamaCppDirectory = installPath;
            _settings.ActiveLlamaCppVersion = LatestVersion;
            await _settings.SaveAsync();

            ActiveVersion = LatestVersion;
            HasUpdate = false;
        }
        catch (Exception ex)
        {
            // Update failed — keep HasUpdate true so user can retry
            System.Diagnostics.Debug.WriteLine($"Update failed: {ex.Message}");
        }
        finally
        {
            IsUpdating = false;
            _updater.DownloadProgress -= (s, progress) => UpdateProgress = progress;
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

        var defaultProfile = await _profileManager.GetDefaultProfileAsync();
        SelectedModel = defaultProfile?.ModelPath != null
            ? Path.GetFileName(defaultProfile.ModelPath)
            : "No model selected";

        if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
            ActiveVersion = _settings.ActiveLlamaCppVersion;

        EstimatedVramGb = ServerStatus.VramUsedGb;
        VramPercentage = ServerStatus.VramUsedGb > 0
            ? Math.Min(ServerStatus.VramUsedGb / 24.0 * 100, 100)
            : 0;

        _ = CheckVersionAsync();
    }
}