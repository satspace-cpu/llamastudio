using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public enum MonitorStyle { Bars, Circles, Compact }

public partial class MonitoringViewModel : ObservableObject, IDisposable
{
    readonly IServerManager _serverManager;
    readonly IProfileManager _profileManager;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly IGpuMonitor _gpuMonitor;
    readonly ILocalizationService _loc;
    readonly DispatcherTimer _gpuTimer;

    [ObservableProperty] ServerStatus _serverStatus = new();
    [ObservableProperty] GpuInfo? _gpuInfo;
    double _lastTps;
    double _lastPromptTps;

    // Monitoring active state
    [ObservableProperty] bool _isMonitoringActive = true;

    // Window settings (loaded from ISettings)
    [ObservableProperty] double _windowOpacity = 0.92;
    [ObservableProperty] bool _isAlwaysOnTop = true;
    [ObservableProperty] MonitorStyle _displayStyle = MonitorStyle.Bars;

    // Metric visibility toggles (loaded from ISettings)
    [ObservableProperty] bool _showTps = true;
    [ObservableProperty] bool _showVram = true;
    [ObservableProperty] bool _showRam = true;
    [ObservableProperty] bool _showGpuTemp = true;
    [ObservableProperty] bool _showPowerDraw = true;
    [ObservableProperty] bool _showGpuCore = true;
    [ObservableProperty] bool _showFanSpeed = true;

    // Computed display values
    public string TpsText => $"{_lastTps:F1}";
    public string PromptTpsText => $"{_lastPromptTps:F1}";
    public double TpsPercent => Math.Min(_lastTps / 300.0 * 100, 100);
    public SolidColorBrush TpsBrush
    {
        get
        {
            if (_lastTps <= 50) return new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
            if (_lastTps <= 150) return new SolidColorBrush(Color.FromArgb(255, 251, 191, 36));
            return new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
        }
    }

    public string VramUsedText => GpuInfo != null
        ? $"{GpuInfo.MemoryUsedGb:F2} / {GpuInfo.MemoryTotalGb:F1} GB"
        : "\u2014";
    public double VramPercent => GpuInfo?.MemoryPercent ?? 0;

    public string RamUsedText => ServerStatus.RamUsedGb > 0
        ? $"{ServerStatus.RamUsedGb:F1} / {s_totalRamGb:F1} GB"
        : $"\u2014 / {s_totalRamGb:F1} GB";
    public double RamPercent => s_totalRamGb > 0 && ServerStatus.RamUsedGb > 0
        ? Math.Min(ServerStatus.RamUsedGb / s_totalRamGb * 100, 100) : 0;

    public string GpuTempText => GpuInfo != null ? $"{GpuInfo.TemperatureCelsius:F0}\u00B0C" : "\u2014";
    public SolidColorBrush GpuTempBrush => GetTempColor(GpuInfo?.TemperatureCelsius ?? 0);
    public double GpuTempPercent => GpuInfo != null ? Math.Min(GpuInfo.TemperatureCelsius / 95.0 * 100, 100) : 0;

    public string PowerDrawText => GpuInfo != null
        ? $"{GpuInfo.PowerDrawWatts:F0} W / {GpuInfo.PowerLimitWatts:F0} W"
        : "\u2014";
    public double PowerPercent => GpuInfo?.PowerPercent ?? 0;

    public string GpuCoreText => GpuInfo != null ? $"{GpuInfo.GpuUtilization:F0}%" : "\u2014";
    public double GpuCorePercent => GpuInfo?.GpuUtilization ?? 0;

    public string FanSpeedText => GpuInfo != null ? $"{GpuInfo.FanSpeed}%" : "\u2014";
    public double FanSpeedPercent => GpuInfo?.FanSpeed ?? 0;

    public string ModelName => ServerStatus.ModelName != null
        ? System.IO.Path.GetFileName(ServerStatus.ModelName)
        : "\u2014";

    public bool IsRunning => ServerStatus.State == ServerState.Running || ServerStatus.State == ServerState.Starting;
    public bool IsStopped => ServerStatus.State == ServerState.Stopped;

    // Style visibility (bool for IsVisible)
    public bool ShowBarsStyle => DisplayStyle == MonitorStyle.Bars;
    public bool ShowCirclesStyle => DisplayStyle == MonitorStyle.Circles;

    // Style visibility (for proper removal from visual tree)
    public bool BarsVisible => DisplayStyle == MonitorStyle.Bars;
    public bool CirclesVisible => DisplayStyle == MonitorStyle.Circles;

    // Localization strings
    public string Title => _loc.T("mon.title");
    public string ServerControlsLabel => _loc.T("mon.server_controls");
    public string ModelLabel => _loc.T("mon.model_name");
    public string StartBtn => _loc.T("mon.start");
    public string StopBtn => _loc.T("mon.stop");
    public string RestartBtn => _loc.T("mon.restart");
    public string TpsLabel => _loc.T("mon.tps");
    public string TpsPromptLabel => _loc.T("mon.prompt_tps");
    public string GenLabel => _loc.T("mon.gen_label");
    public string VramLabel => _loc.T("mon.vram");
    public string RamLabel => _loc.T("mon.ram");
    public string GpuTempLabel => _loc.T("mon.gpu_temp");
    public string PowerLabel => _loc.T("mon.power");
    public string GpuCoreLabel => _loc.T("mon.gpu_core");
    public string FanLabel => _loc.T("mon.fan");
    public string SettingsTitle => _loc.T("mon.settings_title");
    public string OpacityLabel => _loc.T("mon.opacity");
    public string AlwaysOnTopLabel => _loc.T("mon.always_on_top");
    public string VisibleMetricsLabel => _loc.T("mon.visible_metrics");
    public string StyleTitle => _loc.T("mon.style_title");
    public string StyleBars => _loc.T("mon.style_bars");
    public string StyleCircles => _loc.T("mon.style_circles");
    public string OpenFloatingBtn => _loc.T("mon.open_floating");
    public string MonitoringToggleText => IsMonitoringActive ? _loc.T("mon.monitoring_on") : _loc.T("mon.monitoring_off");
    public SolidColorBrush MonitoringIndicatorBrush => IsMonitoringActive
        ? new SolidColorBrush(Color.FromArgb(255, 16, 185, 129))
        : new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));

    static readonly double s_totalRamGb = GetTotalPhysicalMemory() / (1024.0 * 1024.0 * 1024.0);

    static long GetTotalPhysicalMemory()
    {
        try
        {
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(64);
            try
            {
                System.Runtime.InteropServices.Marshal.WriteInt32(ptr, 64);
                if (GlobalMemoryStatusEx(ptr))
                    return System.Runtime.InteropServices.Marshal.ReadInt64(ptr, 8);
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr); }
        }
        catch { }
        return 64L * 1024 * 1024 * 1024;
    }

    [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(System.IntPtr lpBuffer);

    public MonitoringViewModel(
        IServerManager serverManager,
        IProfileManager profileManager,
        ISettings settings,
        ILogService log,
        IGpuMonitor gpuMonitor,
        ILocalizationService loc)
    {
        _serverManager = serverManager;
        _profileManager = profileManager;
        _settings = settings;
        _log = log;
        _gpuMonitor = gpuMonitor;
        _loc = loc;

        ApplySettings();

        _gpuTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _gpuTimer.Tick += (_, _) => _ = RefreshGpuAsync();
        _gpuTimer.Start();

        serverManager.StatusChanged += (_, status) => ServerStatus = status;
        loc.OnLanguageChanged += OnLanguageChanged;
    }

    void ApplySettings()
    {
        WindowOpacity = _settings.MonitorOpacity;
        IsAlwaysOnTop = _settings.MonitorAlwaysOnTop;
        ShowTps = _settings.MonitorShowTps;
        ShowVram = _settings.MonitorShowVram;
        ShowRam = _settings.MonitorShowRam;
        ShowGpuTemp = _settings.MonitorShowGpuTemp;
        ShowPowerDraw = _settings.MonitorShowPowerDraw;
        ShowGpuCore = _settings.MonitorShowGpuCore;
        ShowFanSpeed = _settings.MonitorShowFanSpeed;
    }

    public void RefreshFromSettings()
    {
        ApplySettings();
    }

    partial void OnDisplayStyleChanged(MonitorStyle value)
    {
        OnPropertyChanged(nameof(ShowBarsStyle));
        OnPropertyChanged(nameof(ShowCirclesStyle));
        OnPropertyChanged(nameof(BarsVisible));
        OnPropertyChanged(nameof(CirclesVisible));
    }

    partial void OnIsMonitoringActiveChanged(bool value)
    {
        if (value)
            _gpuTimer.Start();
        else
            _gpuTimer.Stop();
        OnPropertyChanged(nameof(MonitoringToggleText));
    }

    public void Dispose()
    {
        _gpuTimer.Stop();
    }

    partial void OnServerStatusChanged(ServerStatus value)
    {
        _lastTps = value.TokensPerSecond;
        _lastPromptTps = value.PromptTokensPerSecond;
        OnPropertyChanged(nameof(TpsText));
        OnPropertyChanged(nameof(PromptTpsText));
        OnPropertyChanged(nameof(TpsPercent));
        OnPropertyChanged(nameof(TpsBrush));
        OnPropertyChanged(nameof(RamUsedText));
        OnPropertyChanged(nameof(RamPercent));
        OnPropertyChanged(nameof(ModelName));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsStopped));
    }

    async Task RefreshGpuAsync()
    {
        try
        {
            var info = await _gpuMonitor.GetGpuInfoAsync();
            if (info != null)
            {
                GpuInfo = info;
                OnPropertyChanged(nameof(VramUsedText));
                OnPropertyChanged(nameof(VramPercent));
                OnPropertyChanged(nameof(GpuTempText));
                OnPropertyChanged(nameof(GpuTempBrush));
                OnPropertyChanged(nameof(GpuTempPercent));
                OnPropertyChanged(nameof(PowerDrawText));
                OnPropertyChanged(nameof(PowerPercent));
                OnPropertyChanged(nameof(GpuCoreText));
                OnPropertyChanged(nameof(GpuCorePercent));
                OnPropertyChanged(nameof(FanSpeedText));
                OnPropertyChanged(nameof(FanSpeedPercent));
            }
        }
        catch { }
    }

    [RelayCommand]
    async Task StartServerAsync()
    {
        var profile = await GetActiveProfileAsync();
        if (profile != null)
        {
            if (string.IsNullOrWhiteSpace(profile.Host) && !string.IsNullOrWhiteSpace(_settings.DefaultHost))
                profile.Host = _settings.DefaultHost;
            if (profile.Port <= 0 && _settings.DefaultPort > 0)
                profile.Port = _settings.DefaultPort;
            await _serverManager.StartAsync(profile);
        }
    }

    [RelayCommand]
    async Task StopServerAsync()
    {
        await _serverManager.StopAsync();
    }

    [RelayCommand]
    async Task RestartServerAsync()
    {
        await _serverManager.StopAsync();
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);
            var status = await _serverManager.GetStatusAsync();
            if (status.State == ServerState.Stopped) break;
        }

        var profile = await GetActiveProfileAsync();
        if (profile != null && !string.IsNullOrWhiteSpace(profile.ModelPath))
        {
            await _serverManager.StartAsync(profile);
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
    void ToggleMonitoring()
    {
        IsMonitoringActive = !IsMonitoringActive;
    }

    [RelayCommand]
    void SetStyle(string? styleName)
    {
        DisplayStyle = styleName switch
        {
            "Bars" => MonitorStyle.Bars,
            "Circles" => MonitorStyle.Circles,
            "Compact" => MonitorStyle.Compact,
            _ => DisplayStyle
        };
    }

    [RelayCommand]
    void OpenFloatingWindow()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (lifetime == null) return;

            var existing = lifetime.Windows.FirstOrDefault(w => w is Views.MonitoringWindow);
            if (existing != null)
            {
                existing.Show();
                existing.Activate();
            }
            else
            {
                var win = new Views.MonitoringWindow(this);
                // Don't set owner so it survives when MainWindow hides to tray
                win.Show();
            }
        }
        catch { }
    }

    static SolidColorBrush GetTempColor(double temp)
    {
        byte r, g, b;
        if (temp <= 55)
            { r = 16; g = 185; b = 129; }
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

    void OnLanguageChanged(object? sender, string language)
    {
        foreach (var prop in new[]
        {
            nameof(Title), nameof(ServerControlsLabel), nameof(ModelLabel),
            nameof(StartBtn), nameof(StopBtn), nameof(RestartBtn),
            nameof(TpsLabel), nameof(TpsPromptLabel), nameof(GenLabel), nameof(VramLabel), nameof(RamLabel),
            nameof(GpuTempLabel), nameof(PowerLabel), nameof(GpuCoreLabel), nameof(FanLabel),
            nameof(SettingsTitle), nameof(OpacityLabel), nameof(AlwaysOnTopLabel),
            nameof(VisibleMetricsLabel), nameof(StyleTitle),
                      nameof(StyleBars), nameof(StyleCircles),
                      nameof(OpenFloatingBtn), nameof(MonitoringToggleText), nameof(MonitoringIndicatorBrush)
        })
            OnPropertyChanged(prop);
    }
}
