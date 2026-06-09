using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly ILocalizationService _loc;
    readonly IAppUpdater _appUpdater;
    System.Timers.Timer? _autoSaveTimer;
    bool _isDisposing;

    [ObservableProperty] string _llamaCppBaseDirectory;
    [ObservableProperty] string _llamaCppDirectory;
    [ObservableProperty] string _modelsDirectory;
    [ObservableProperty] string _theme = "Dark";
    [ObservableProperty] bool _autoCheckUpdates = true;
    [ObservableProperty] bool _minimizeToTray = true;
    [ObservableProperty] bool _startMinimized;
    [ObservableProperty] bool _autoStartWithOS;
    [ObservableProperty] bool _startMonitoringWindow = true;
    [ObservableProperty] bool _keepServerOnExit;
    [ObservableProperty] string _defaultHost = "127.0.0.1";
    [ObservableProperty] int _defaultPort = 8080;
    [ObservableProperty] string _defaultGpuLayers = "all";
    [ObservableProperty] bool _flashAttention = true;
    string _selectedUpdateChannel = "Stable";
    public string SelectedUpdateChannel
    {
        get => _selectedUpdateChannel;
        set
        {
            if (SetProperty(ref _selectedUpdateChannel, value))
            {
                _settings.UpdateChannel = value switch
                {
                    "PreRelease" => Core.Enums.UpdateChannel.PreRelease,
                    "Nightly" => Core.Enums.UpdateChannel.Nightly,
                    _ => Core.Enums.UpdateChannel.Stable
                };
                ResetAutoSave();
            }
        }
    }
    public string[] UpdateChannelOptions => new[] { "Stable", "PreRelease", "Nightly" };
    [ObservableProperty] string _selectedLanguage = "en";
    [ObservableProperty] string _updateStatus = "Not checked";
    [ObservableProperty] string _appVersion = "0.0.0";
    [ObservableProperty] bool _isCheckingUpdates;
    [ObservableProperty] bool _hasUpdateAvailable;
    [ObservableProperty] string _latestVersion = "";
    [ObservableProperty] string _updateChangelog = "";
    [ObservableProperty] bool _isDownloading;
    [ObservableProperty] double _downloadProgress;

    // Monitoring settings
    [ObservableProperty] double _monitorOpacity = 0.92;
    [ObservableProperty] bool _monitorAlwaysOnTop = true;
    [ObservableProperty] bool _monitorShowTps = true;
    [ObservableProperty] bool _monitorShowVram = true;
    [ObservableProperty] bool _monitorShowRam = true;
    [ObservableProperty] bool _monitorShowGpuTemp = true;
    [ObservableProperty] bool _monitorShowPowerDraw = true;
    [ObservableProperty] bool _monitorShowGpuCore = true;
    [ObservableProperty] bool _monitorShowFanSpeed = true;

    public string MonitorOpacityPercent => $"{MonitorOpacity * 100:F0}%";

    partial void OnMonitorOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(MonitorOpacityPercent));
        _settings.MonitorOpacity = value;
        ResetAutoSave();
    }
    partial void OnMonitorAlwaysOnTopChanged(bool value) { _settings.MonitorAlwaysOnTop = value; ResetAutoSave(); }
    partial void OnMonitorShowTpsChanged(bool value) { _settings.MonitorShowTps = value; ResetAutoSave(); }
    partial void OnMonitorShowVramChanged(bool value) { _settings.MonitorShowVram = value; ResetAutoSave(); }
    partial void OnMonitorShowRamChanged(bool value) { _settings.MonitorShowRam = value; ResetAutoSave(); }
    partial void OnMonitorShowGpuTempChanged(bool value) { _settings.MonitorShowGpuTemp = value; ResetAutoSave(); }
    partial void OnMonitorShowPowerDrawChanged(bool value) { _settings.MonitorShowPowerDraw = value; ResetAutoSave(); }
    partial void OnMonitorShowGpuCoreChanged(bool value) { _settings.MonitorShowGpuCore = value; ResetAutoSave(); }
    partial void OnMonitorShowFanSpeedChanged(bool value) { _settings.MonitorShowFanSpeed = value; ResetAutoSave(); }

    public string[] LanguageOptions => new[] { "en", "ru" };
    public string[] ThemeOptions => new[] { "Dark", "Light", "System" };

    // Text wrappers for port/gpu layers (TextBox binding instead of NumericUpDown)
    [ObservableProperty] string _defaultPortText = "8080";
    [ObservableProperty] string _defaultGpuLayersText = "all";

    partial void OnDefaultPortTextChanged(string value)
    {
        if (int.TryParse(value, out var port) && port >= 1 && port <= 65535)
            DefaultPort = port;
    }

 partial void OnDefaultGpuLayersTextChanged(string value)
    {
        DefaultGpuLayers = value;
    }

    partial void OnDefaultGpuLayersChanged(string value)
    {
        _settings.DefaultGpuLayers = value;
        ResetAutoSave();
        if (DefaultGpuLayersText != value)
            DefaultGpuLayersText = value;
    }

    partial void OnDefaultPortChanged(int value)
    {
        // Immediately sync to settings model
        _settings.DefaultPort = value;
        ResetAutoSave();
        if (DefaultPortText != value.ToString())
            DefaultPortText = value.ToString();
    }

    // Translated strings
    public string Title => _loc.T("settings.title");
    public string GeneralSection => _loc.T("settings.general_section");
    public string LanguageLabel => _loc.T("settings.language_label");
    public string ThemeLabel => _loc.T("settings.theme_label");
    public string AutoCheckUpdatesLabel => _loc.T("settings.auto_check_updates");
    public string MinimizeToTrayLabel => _loc.T("settings.minimize_to_tray");
    public string StartMinimizedLabel => _loc.T("settings.start_minimized");
    public string AutoStartWithOSLabel => _loc.T("settings.auto_start_with_os");
    public string StartMonitoringWindowLabel => _loc.T("settings.start_monitoring_window");
    public string KeepServerOnExitLabel => _loc.T("settings.keep_server_on_exit");
    public string PathsSection => _loc.T("settings.paths_section");
    public string LlamaCppBaseDirLabel => _loc.T("settings.llama_cpp_base_dir_label");
    public string LlamaCppDirLabel => _loc.T("settings.llama_cpp_dir_label");
    public string ModelsDirLabel => _loc.T("settings.models_dir_label");
    public string BrowseBtn => _loc.T("settings.browse_btn");
    public string ServerDefaultsSection => _loc.T("settings.server_defaults_section");
    public string HostLabel => _loc.T("settings.host_label");
    public string PortLabel => _loc.T("settings.port_label");
    public string GpuLayersLabel => _loc.T("settings.gpu_layers_label");
    public string FlashAttentionLabel => _loc.T("settings.flash_attention");
    public string UpdateChannelLabel => _loc.T("settings.update_channel_label");
    public string SaveSettingsBtn => _loc.T("settings.save_settings_btn");
    public string DarkTheme => _loc.T("settings.dark");
    public string LightTheme => _loc.T("settings.light");
    public string SystemTheme => _loc.T("settings.system");
    public string StableChannel => _loc.T("settings.stable");
    public string PrereleaseChannel => _loc.T("settings.prerelease");
    public string NightlyChannel => _loc.T("settings.nightly");
    public string RussianLanguage => _loc.T("settings.russian");
    public string EnglishLanguage => _loc.T("settings.english");

    // App update strings
    public string AppVersionLabel => _loc.T("dash.app_version");
    public string AppUpdateStatus => HasUpdateAvailable ? $"↑ {_loc.T("dash.new_version")} {LatestVersion}" : $"✓ {_loc.T("dash.up_to_date")}";
    public string CheckUpdatesBtn => IsCheckingUpdates ? _loc.T("dash.checking_update") : _loc.T("dash.check_updates");
    public string DownloadUpdateBtn => IsDownloading ? $"{_loc.T("dash.downloading")} {DownloadProgress:F0}%" : $"{_loc.T("dash.update")} ({LatestVersion})";

    // ToolTips
    public string TtLanguage => _loc.T("tt.language");
    public string TtLlamaCppBaseDir => _loc.T("tt.llama_cpp_base_dir");
    public string TtLlamaCppDir => _loc.T("tt.llama_cpp_dir");
    public string TtModelsDir => _loc.T("tt.models_dir");
    public string TtAutoCheckUpdates => _loc.T("tt.auto_check_updates");
    public string TtMinimizeToTray => _loc.T("tt.minimize_to_tray");
    public string TtStartMinimized => _loc.T("tt.start_minimized");
    public string TtAutoStartWithOS => _loc.T("tt.auto_start_with_os");
    public string TtStartMonitoringWindow => _loc.T("tt.start_monitoring_window");
    public string TtKeepServerOnExit => _loc.T("tt.keep_server_on_exit");
    public string TtTheme => _loc.T("tt.theme");
    public string TtUpdateChannel => _loc.T("tt.update_channel");
    public string TtHost => _loc.T("tt.host");
    public string TtHostDefault => _loc.T("tt.host_default");
    public string TtPort => _loc.T("tt.port");
    public string TtPortManual => _loc.T("tt.port_manual");
    public string TtGpuLayers => _loc.T("tt.gpu_layers");
    public string TtGpuLayersDefault => _loc.T("tt.gpu_layers_default");
    public string TtFlashAttention => _loc.T("tt.flash_attention");

    public SettingsViewModel(ISettings settings, ILogService log, IDialogService dialog, ILocalizationService loc, IAppUpdater appUpdater)
    {
        _settings = settings;
        _log = log;
        _dialog = dialog;
        _loc = loc;
        _appUpdater = appUpdater;

        AppVersion = _appUpdater.GetCurrentVersion();

        _appUpdater.StatusChanged += (s, msg) => UpdateStatus = msg;
        _appUpdater.UpdateAvailable += (s, info) =>
        {
            HasUpdateAvailable = true;
            LatestVersion = info.Version;
            UpdateChangelog = info.Changelog;
        };

        LlamaCppBaseDirectory = _settings.LlamaCppBaseDirectory;
        LlamaCppDirectory = _settings.LlamaCppDirectory;
        ModelsDirectory = _settings.ModelsDirectory;
        Theme = _settings.Theme;
        AutoCheckUpdates = _settings.AutoCheckUpdates;
        MinimizeToTray = _settings.MinimizeToTray;
        StartMinimized = _settings.StartMinimized;
        AutoStartWithOS = _settings.AutoStartWithOS;
        StartMonitoringWindow = _settings.StartMonitoringWindow;
        ApplyAutoStart(_settings.AutoStartWithOS);
        KeepServerOnExit = _settings.KeepServerOnExit;
        DefaultHost = _settings.DefaultHost;
        DefaultPort = _settings.DefaultPort;
        DefaultPortText = _settings.DefaultPort.ToString();
        DefaultGpuLayers = _settings.DefaultGpuLayers ?? "all";
        DefaultGpuLayersText = _settings.DefaultGpuLayers ?? "all";
        FlashAttention = _settings.FlashAttention;
        SelectedUpdateChannel = _settings.UpdateChannel switch
            {
                Core.Enums.UpdateChannel.PreRelease => "PreRelease",
                Core.Enums.UpdateChannel.Nightly => "Nightly",
                _ => "Stable"
            };
        SelectedLanguage = _loc.Language;

        // Monitoring settings
        MonitorOpacity = _settings.MonitorOpacity;
        MonitorAlwaysOnTop = _settings.MonitorAlwaysOnTop;
        MonitorShowTps = _settings.MonitorShowTps;
        MonitorShowVram = _settings.MonitorShowVram;
        MonitorShowRam = _settings.MonitorShowRam;
        MonitorShowGpuTemp = _settings.MonitorShowGpuTemp;
        MonitorShowPowerDraw = _settings.MonitorShowPowerDraw;
        MonitorShowGpuCore = _settings.MonitorShowGpuCore;
        MonitorShowFanSpeed = _settings.MonitorShowFanSpeed;

        _loc.OnLanguageChanged += OnLanguageChanged;

        // Auto-save timer — saves settings 1 second after last change
        _autoSaveTimer = new System.Timers.Timer(1000) { AutoReset = false };
        _autoSaveTimer.Elapsed += (s, e) => _ = SaveSettingsInternalAsync();
    }

    void OnLanguageChanged(object? sender, string language)
    {
        foreach (var prop in new[]
        {
            nameof(Title), nameof(GeneralSection), nameof(LanguageLabel), nameof(ThemeLabel),
            nameof(AutoCheckUpdatesLabel), nameof(MinimizeToTrayLabel), nameof(StartMinimizedLabel), nameof(AutoStartWithOSLabel), nameof(StartMonitoringWindowLabel), nameof(KeepServerOnExitLabel), nameof(PathsSection),
            nameof(LlamaCppBaseDirLabel), nameof(LlamaCppDirLabel), nameof(ModelsDirLabel), nameof(BrowseBtn),
            nameof(ServerDefaultsSection), nameof(HostLabel), nameof(PortLabel), nameof(GpuLayersLabel),
            nameof(FlashAttentionLabel), nameof(UpdateChannelLabel),
            nameof(SaveSettingsBtn),
            nameof(DarkTheme), nameof(LightTheme), nameof(SystemTheme),
            nameof(StableChannel), nameof(PrereleaseChannel), nameof(NightlyChannel),
            nameof(RussianLanguage), nameof(EnglishLanguage),
            nameof(TtLanguage), nameof(TtLlamaCppBaseDir), nameof(TtLlamaCppDir), nameof(TtModelsDir),             nameof(TtAutoCheckUpdates),
            nameof(TtMinimizeToTray), nameof(TtStartMinimized), nameof(TtAutoStartWithOS), nameof(TtStartMonitoringWindow), nameof(TtKeepServerOnExit),
            nameof(TtTheme), nameof(TtUpdateChannel), nameof(TtHost),
            nameof(TtHostDefault), nameof(TtPort), nameof(TtPortManual), nameof(TtGpuLayers),
            nameof(TtGpuLayersDefault), nameof(TtFlashAttention)
        })
            OnPropertyChanged(prop);
    }

    partial void OnLlamaCppBaseDirectoryChanged(string value) => ResetAutoSave();
    partial void OnLlamaCppDirectoryChanged(string value) => ResetAutoSave();
    partial void OnModelsDirectoryChanged(string value) => ResetAutoSave();
    partial void OnThemeChanged(string value)
    {
        ResetAutoSave();
        ApplyTheme(value);
    }

    static void ApplyTheme(string? theme)
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;

        var variant = theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => DetectSystemThemeVariant()
        };
        app.RequestedThemeVariant = variant;

        // Delegate to App's shared ApplyThemeColors (sets ALL theme resources)
        if (app is LlamaStudio.App appInstance)
        {
            ApplyThemeColors(appInstance, variant == Avalonia.Styling.ThemeVariant.Light);
        }
    }

    static void ApplyThemeColors(LlamaStudio.App app, bool lightMode)
    {
        // Reuse the exact same logic as App.ApplyThemeColors
        if (lightMode)
        {
            app.Resources["ThemeBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F5F5F5"));
            app.Resources["ThemeSurface"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FAFAFA"));
            app.Resources["ThemeCard"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["ThemeTextPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            app.Resources["ThemeTextSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#656D76"));
            app.Resources["ThemeTextTertiary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B949E"));
            app.Resources["ThemeBorder"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D0D7DE"));
            app.Resources["ThemeBorderStrong"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#C9D1D9"));
            app.Resources["ThemeInputBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["ThemeAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["ThemeAccentHover"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1A7FCA"));
            app.Resources["CardServerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardModelsBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardProfilesBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardVersionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardSectionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardServerAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["CardServerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["CardSubText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280"));
            app.Resources["GpuMonitorBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["GpuMonitorInner"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["GpuMonitorHeader"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["ProgressTrack"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E1E4E8"));
            app.Resources["ProgressVram"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["ProgressCore"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["ProgressPower"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["ProgressFan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["ProgressRam"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["SidebarBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F0F0F0"));
            app.Resources["SidebarItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["SidebarItemSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D4D4D4"));
            app.Resources["SidebarItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#57606A"));
            app.Resources["SidebarItemTextSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            app.Resources["TabItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["TabItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#57606A"));
            app.Resources["TabItemSelectedBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["TabItemSelectedText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["TabItemHoverBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D4D4D4"));
            app.Resources["ProfileInnerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["ProfileModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["BadgeMtp"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BadgeReasoning"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BadgeFlash"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2563EB"));
            app.Resources["BadgeCache"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D97706"));
            app.Resources["BadgeBatching"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
            app.Resources["BadgeText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["ServerBannerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["ServerBannerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["ServerBannerSub"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280"));
            app.Resources["SectionTitle"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            app.Resources["SectionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#393F46"));
            app.Resources["SectionLabelLight"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#656D76"));
            app.Resources["BtnPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["BtnSuccess"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2DA44E"));
            app.Resources["BtnDanger"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CF222E"));
            app.Resources["BtnSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["BtnPurple"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B54FF"));
            app.Resources["BtnCyan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
        }
        else
        {
            app.Resources["ThemeBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeSurface"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B"));
            app.Resources["ThemeCard"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeTextPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["ThemeTextSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["ThemeTextTertiary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            app.Resources["ThemeBorder"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155"));
            app.Resources["ThemeBorderStrong"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            app.Resources["ThemeInputBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["ThemeAccentHover"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4F46E5"));
            app.Resources["CardServerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1B4B"));
            app.Resources["CardModelsBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#064E3B"));
            app.Resources["CardProfilesBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C2D12"));
            app.Resources["CardVersionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E3A5F"));
            app.Resources["CardModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#172554"));
            app.Resources["CardSectionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F1533"));
            app.Resources["CardServerAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["CardServerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#818CF8"));
            app.Resources["CardModelsLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#34D399"));
            app.Resources["CardProfilesLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FDBA74"));
            app.Resources["CardVersionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#60A5FA"));
            app.Resources["CardModelLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#60A5FA"));
            app.Resources["CardSubText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A5B4FC"));
            app.Resources["GpuMonitorBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F1533"));
            app.Resources["GpuMonitorInner"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["GpuMonitorHeader"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A78BFA"));
            app.Resources["ProgressTrack"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["ProgressVram"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["ProgressCore"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["ProgressPower"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["ProgressFan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["ProgressRam"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["SidebarBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["SidebarItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B"));
            app.Resources["SidebarItemSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#27272A"));
            app.Resources["SidebarItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["SidebarItemTextSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["TabItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155"));
            app.Resources["TabItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["TabItemSelectedBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["TabItemSelectedText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["TabItemHoverBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            app.Resources["ProfileInnerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["ProfileModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["BadgeMtp"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BadgeReasoning"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BadgeFlash"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2563EB"));
            app.Resources["BadgeCache"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D97706"));
            app.Resources["BadgeBatching"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
            app.Resources["BadgeText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["ServerBannerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1B4B"));
            app.Resources["ServerBannerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#818CF8"));
            app.Resources["ServerBannerSub"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            app.Resources["SectionTitle"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["SectionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E2E8F0"));
            app.Resources["SectionLabelLight"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            app.Resources["BtnPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4338CA"));
            app.Resources["BtnSuccess"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BtnDanger"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DC2626"));
            app.Resources["BtnSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            app.Resources["BtnPurple"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BtnCyan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
        }
    }

    static Avalonia.Styling.ThemeVariant DetectSystemThemeVariant()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value != null)
            {
                return int.TryParse(value.ToString(), out var isLight) && isLight == 1
                    ? Avalonia.Styling.ThemeVariant.Light
                    : Avalonia.Styling.ThemeVariant.Dark;
            }
        }
        catch { }
        return Avalonia.Styling.ThemeVariant.Light;
    }
    partial void OnAutoCheckUpdatesChanged(bool value) => ResetAutoSave();
    partial void OnMinimizeToTrayChanged(bool value)
    {
        _settings.MinimizeToTray = value;
        _ = _settings.SaveAsync(); // Save immediately, no timer delay
    }
    partial void OnStartMinimizedChanged(bool value)
    {
        _settings.StartMinimized = value;
        ResetAutoSave();
    }
    partial void OnAutoStartWithOSChanged(bool value)
    {
        _settings.AutoStartWithOS = value;
        ApplyAutoStart(value);
        ResetAutoSave();
    }
    partial void OnStartMonitoringWindowChanged(bool value)
    {
        _settings.StartMonitoringWindow = value;
        ResetAutoSave();
    }
    partial void OnKeepServerOnExitChanged(bool value)
    {
        _settings.KeepServerOnExit = value;
        ResetAutoSave();
    }
    partial void OnDefaultHostChanged(string value)
        {
            // Save host as-is — server binds to whatever user sets (0.0.0.0, 127.0.0.1, etc.)
            _settings.DefaultHost = value;
            ResetAutoSave();
        }
    partial void OnFlashAttentionChanged(bool value) => ResetAutoSave();
    partial void OnHasUpdateAvailableChanged(bool value) { OnPropertyChanged(nameof(AppUpdateStatus)); }
    partial void OnLatestVersionChanged(string value) { OnPropertyChanged(nameof(AppUpdateStatus)); OnPropertyChanged(nameof(DownloadUpdateBtn)); }
    partial void OnIsDownloadingChanged(bool value) { OnPropertyChanged(nameof(DownloadUpdateBtn)); }
    partial void OnDownloadProgressChanged(double value) { OnPropertyChanged(nameof(DownloadUpdateBtn)); }
    partial void OnSelectedLanguageChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _loc.ChangeLanguage(value);
            ResetAutoSave();
            _log.Information($"Language changed to: {value}", "Settings");
        }
    }

    static void ApplyAutoStart(bool enabled)
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (key == null) return;

            if (enabled)
                key.SetValue("LlamaStudio", $"\"{exePath}\"");
            else
                key.DeleteValue("LlamaStudio", false);
        }
        catch { }
    }

    void ResetAutoSave()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    async Task SaveSettingsInternalAsync()
    {
        if (_isDisposing) return;

        ApplySettingsToModel();
        await _settings.SaveAsync();
        _log.Information("Settings auto-saved", "Settings");
    }

    public void ApplySettingsToModel()
    {
        _settings.LlamaCppBaseDirectory = LlamaCppBaseDirectory;
        _settings.LlamaCppDirectory = LlamaCppDirectory;
        _settings.ModelsDirectory = ModelsDirectory;
        _settings.Theme = Theme;
        _settings.Language = SelectedLanguage;
        _settings.AutoCheckUpdates = AutoCheckUpdates;
        _settings.MinimizeToTray = MinimizeToTray;
        _settings.StartMinimized = StartMinimized;
        _settings.AutoStartWithOS = AutoStartWithOS;
        _settings.StartMonitoringWindow = StartMonitoringWindow;
        _settings.KeepServerOnExit = KeepServerOnExit;
        _settings.DefaultHost = DefaultHost;
        _settings.DefaultPort = DefaultPort;
        _settings.DefaultGpuLayers = DefaultGpuLayers;
        _settings.FlashAttention = FlashAttention;
        _settings.UpdateChannel = SelectedUpdateChannel switch
            {
                "PreRelease" => Core.Enums.UpdateChannel.PreRelease,
                "Nightly" => Core.Enums.UpdateChannel.Nightly,
                _ => Core.Enums.UpdateChannel.Stable
            };

        // Monitoring settings
        _settings.MonitorOpacity = MonitorOpacity;
        _settings.MonitorAlwaysOnTop = MonitorAlwaysOnTop;
        _settings.MonitorShowTps = MonitorShowTps;
        _settings.MonitorShowVram = MonitorShowVram;
        _settings.MonitorShowRam = MonitorShowRam;
        _settings.MonitorShowGpuTemp = MonitorShowGpuTemp;
        _settings.MonitorShowPowerDraw = MonitorShowPowerDraw;
        _settings.MonitorShowGpuCore = MonitorShowGpuCore;
        _settings.MonitorShowFanSpeed = MonitorShowFanSpeed;
    }

    public void Dispose()
    {
        _isDisposing = true;
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
        _appUpdater.StatusChanged -= (s, msg) => UpdateStatus = msg;
        _appUpdater.UpdateAvailable -= (s, info) =>
        {
            HasUpdateAvailable = true;
            LatestVersion = info.Version;
            UpdateChangelog = info.Changelog;
        };
    }

    [RelayCommand]
    async Task SaveSettings()
    {
        _autoSaveTimer?.Stop();
        ApplySettingsToModel();
        await _settings.SaveAsync();
        _log.Information("Settings saved manually", "Settings");
    }

    [RelayCommand]
    async Task SelectLlamaCppBaseDirectory()
    {
        var path = await _dialog.SelectFolderAsync("Select llama.cpp Base Directory", LlamaCppBaseDirectory);
        if (!string.IsNullOrEmpty(path))
        {
            LlamaCppBaseDirectory = path;
            _settings.LlamaCppBaseDirectory = path;
            await _settings.SaveAsync();
        }
    }

    [RelayCommand]
    async Task SelectLlamaCppDirectory()
    {
        var path = await _dialog.SelectFolderAsync("Select llama.cpp Directory", LlamaCppDirectory);
        if (!string.IsNullOrEmpty(path))
        {
            LlamaCppDirectory = path;
            _settings.LlamaCppDirectory = path;
            await _settings.SaveAsync();
        }
    }

    [RelayCommand]
    async Task SelectModelsDirectory()
    {
        var path = await _dialog.SelectFolderAsync("Select Models Directory", ModelsDirectory);
        if (!string.IsNullOrEmpty(path))
        {
            ModelsDirectory = path;
            _settings.ModelsDirectory = path;
            await _settings.SaveAsync();
        }
    }

    [RelayCommand]
    async Task CheckForUpdates()
    {
        IsCheckingUpdates = true;
        HasUpdateAvailable = false;
        LatestVersion = "";
        UpdateChangelog = "";
        UpdateStatus = "Checking for updates...";

        try
        {
            var update = await _appUpdater.CheckForUpdatesAsync();
            if (update == null)
            {
                UpdateStatus = $"No updates available (current: {AppVersion})";
                _log.Information($"No updates available for LlamaStudio {AppVersion}", "Settings");
            }
            else
            {
                // Update is already reported via event
                UpdateStatus = $"Update available: {update.Version}";
                _log.Information($"Update available: {update.Version}", "Settings");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Error: {ex.Message}";
            _log.Error($"Failed to check for updates: {ex.Message}", "Settings");
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    async Task DownloadUpdate()
    {
        if (!HasUpdateAvailable)
            return;

        IsDownloading = true;
        DownloadProgress = 0;
        UpdateStatus = "Downloading update...";

        try
        {
            // Subscribe to progress events
            _appUpdater.ProgressChanged += (s, progress) => DownloadProgress = progress;

            // Download update to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            var update = await _appUpdater.CheckForUpdatesAsync();
            if (update == null)
            {
                UpdateStatus = "No update available";
                HasUpdateAvailable = false;
                return;
            }

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
                _log.Information($"App update downloaded, restarting...", "Settings");
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Error: {ex.Message}";
            _log.Error($"App update failed: {ex.Message}", "Settings");
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
