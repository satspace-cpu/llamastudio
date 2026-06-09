using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Views.Pages;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Reflection;

namespace LlamaStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    readonly IServerManager _serverManager;
    readonly IProfileManager _profileManager;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly ILocalizationService _loc;
    readonly IDialogService _dialog;
    readonly DashboardViewModel _dashboard;
    readonly ModelsViewModel _models;
    readonly ServerViewModel _server;
    readonly ProfilesViewModel _profiles;
    readonly LogsViewModel _logs;
    readonly SettingsViewModel _settingsVm;
    readonly ApiTestViewModel _apiTest;
    readonly ChatViewModel _chat;
    readonly MonitoringViewModel _monitoringVm;
    readonly LlamaReleasesViewModel _llamaReleases;
    readonly SupportViewModel _support;

    [ObservableProperty] string _selectedPage = "Dashboard";
    [ObservableProperty] Control? _currentPage;
    [ObservableProperty] bool _canStartServer = true;
    [ObservableProperty] bool _canStopServer = false;
    [ObservableProperty] bool _canRestartServer = false;

    // Dynamic button labels based on server state
    public string StartBtnText => CanStartServer ? _loc.T("main.start_server") : _loc.T("main.server_running");
    public string StopBtnText => CanStopServer ? _loc.T("main.stop_server") : _loc.T("main.server_stopped");

    // Translated strings for MainWindow
    public string NavDashboard => _loc.T("main.dashboard");
    public string NavModels => _loc.T("main.models");
    public string NavServer => _loc.T("main.server");
      // public string NavProfiles => _loc.T("main.profiles");
    public string NavLogs => _loc.T("main.logs");
    public string NavApiTest => _loc.T("main.apitest");
    public string NavChat => _loc.T("main.chat");
    public string NavMonitoring => _loc.T("main.monitoring");
    public string NavLlamaReleases => _loc.T("main.llama_releases");
    public string NavSettings => _loc.T("main.settings");
    public string NavSupport => _loc.T("main.support");
    public string BtnStartServer => _loc.T("main.start_server");
    public string BtnStopServer => _loc.T("main.stop_server");
    public string BtnRestartServer => _loc.T("main.restart_server");
    public string Subtitle => _loc.T("main.subtitle");
    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public MainViewModel(
        IServerManager serverManager,
        IProfileManager profileManager,
        ISettings settings,
        ILogService log,
        ILocalizationService loc,
        IDialogService dialog,
        DashboardViewModel dashboard,
        ModelsViewModel models,
        ServerViewModel server,
        ProfilesViewModel profiles,
        LogsViewModel logs,
        SettingsViewModel settingsVm,
        ApiTestViewModel apiTest,
        ChatViewModel chat,
        MonitoringViewModel monitoringVm,
        LlamaReleasesViewModel llamaReleases,
        SupportViewModel support)
    {
        _serverManager = serverManager;
        _profileManager = profileManager;
        _settings = settings;
        _log = log;
        _loc = loc;
        _dialog = dialog;
        _dashboard = dashboard;
        _models = models;
        _server = server;
        _profiles = profiles;
        _logs = logs;
        _settingsVm = settingsVm;
        _apiTest = apiTest;
        _chat = chat;
        _monitoringVm = monitoringVm;
        _llamaReleases = llamaReleases;
        _support = support;

        _serverManager.StatusChanged += OnServerStatusChanged;
        _serverManager.LogReceived += OnServerLogReceived;
        _loc.OnLanguageChanged += OnLanguageChanged;
        NavigateTo("Dashboard");
        _ = InitializeAsync();
    }

    // Collect recent server error logs for display
    List<string> _recentErrors = new();
    object _errorLock = new();

    void OnServerLogReceived(object? sender, LogEntry entry)
    {
        if (entry.Level == Core.Enums.LogLevel.Error || entry.Level == Core.Enums.LogLevel.Fatal)
        {
            lock (_errorLock)
            {
                _recentErrors.Add(entry.Message);
                if (_recentErrors.Count > 20)
                    _recentErrors.RemoveAt(0);
            }
        }
    }

    string GetRecentError()
    {
        lock (_errorLock)
        {
            return _recentErrors.Count > 0 ? _recentErrors[^1] : _loc.T("server.unknown_error");
        }
    }

    void ClearRecentErrors()
    {
        lock (_errorLock)
        {
            _recentErrors.Clear();
        }
    }

    void OnLanguageChanged(object? sender, string language)
    {
        OnPropertyChanged(nameof(NavDashboard));
        OnPropertyChanged(nameof(NavModels));
        OnPropertyChanged(nameof(NavServer));
        // OnPropertyChanged(nameof(NavProfiles));
        OnPropertyChanged(nameof(NavLogs));
        OnPropertyChanged(nameof(NavApiTest));
        OnPropertyChanged(nameof(NavChat));
        OnPropertyChanged(nameof(NavMonitoring));
        OnPropertyChanged(nameof(NavLlamaReleases));
        OnPropertyChanged(nameof(NavSettings));
        OnPropertyChanged(nameof(NavSupport));
        OnPropertyChanged(nameof(BtnStartServer));
        OnPropertyChanged(nameof(BtnStopServer));
        OnPropertyChanged(nameof(BtnRestartServer));
        OnPropertyChanged(nameof(Subtitle));
    }

    async Task InitializeAsync()
    {
        try
        {
            await _profiles.LoadProfilesAsync();

            await _dashboard.RefreshAsync();

            if (!string.IsNullOrWhiteSpace(_settings.ModelsDirectory) &&
                Directory.Exists(_settings.ModelsDirectory))
            {
                await _models.ScanModels();
            }

            _log.Information("Application initialized", "Main");

            // Check actual server status on startup
            await RefreshServerStatus();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Initialization failed", "Main");
        }
    }

    public void NavigateTo(string page)
    {
        try
        {
            _log.Information($"NavigateTo called: page='{page}', SelectedPage='{SelectedPage}', CurrentPage={CurrentPage?.GetType().Name}", "Main");

            if (SelectedPage == page && CurrentPage != null)
            {
                _log.Information($"NavigateTo skipped: already on page '{page}'", "Main");
                return;
            }

            SelectedPage = page;
            _log.Information($"SelectedPage set to '{page}'", "Main");

            if (page == "Dashboard")
                _ = _dashboard.RefreshAsync();

            Control ctrl = page switch
            {
                "Dashboard" => new DashboardPage(_dashboard),
                "Models" => new ModelsPage(_models),
                "Server" => new ServerPage(_server),
                // "Profiles" => new ProfilesPage(_profiles),
                "Logs" => new LogsPage(_logs),
                "ApiTest" => new ApiTestPage(_apiTest),
                "Chat" => new ChatPage(_chat),
                "Monitoring" => new MonitoringPage(_monitoringVm),
                "LlamaReleases" => new LlamaReleasesPage(_llamaReleases),
                "Settings" => new SettingsPage(_settingsVm),
                "Support" => new SupportPage(),
                _ => new DashboardPage(_dashboard)
            };
            _log.Information($"Created page control: {ctrl.GetType().Name}", "Main");
            CurrentPage = ctrl;
            _log.Information($"CurrentPage set to {ctrl.GetType().Name}", "Main");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"NavigateTo({page}) failed", "Main");
        }
    }

    [RelayCommand]
    void Navigate(string page)
    {
        try
        {
            NavigateTo(page);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"NavigateCommand({page}) failed", "Main");
        }
    }

    [RelayCommand]
    async Task StartServer()
    {
        try
        {
            // Check if server is already running (KeepServerOnExit scenario)
            var host = _settings.DefaultHost ?? "127.0.0.1";
            var port = _settings.DefaultPort > 0 ? _settings.DefaultPort : 8080;
            var health = await _serverManager.HealthCheckAsync(host, port);
            if (health.State == Core.Enums.ServerState.Running)
            {
                CanStartServer = false;
                CanStopServer = true;
                CanRestartServer = true;
                _log.Information($"Server already running at {host}:{port}", "UI");
                await _dialog.ShowInfoAsync(_loc.T("dialog.server_already_running"));
                return;
            }

            var profile = await GetActiveProfileAsync();
            if (profile == null)
            {
                _log.Error("No profile found. Create a profile first.", "UI");
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.ModelPath))
            {
                _log.Error("Profile has no model selected.", "UI");
                return;
            }

            // Apply host/port from settings if they differ from defaults
            if (!string.IsNullOrWhiteSpace(_settings.DefaultHost))
                profile.Host = _settings.DefaultHost;
            if (_settings.DefaultPort > 0)
                profile.Port = _settings.DefaultPort;

            ClearRecentErrors();
            await _serverManager.StartAsync(profile);

            // Wait up to 15 seconds for server to start, verify with health check
            bool started = false;
            var p = profile.Port > 0 ? profile.Port : port;
            var h = profile.Host ?? host;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                // Check both internal state and actual health
                var status = await _serverManager.GetStatusAsync();
                if (status.State == Core.Enums.ServerState.Running)
                {
                    started = true;
                    break;
                }
                if (status.State == Core.Enums.ServerState.Error)
                    break;
                // Also try health check as fallback
                var hc = await _serverManager.HealthCheckAsync(h, p);
                if (hc.State == Core.Enums.ServerState.Running)
                {
                    started = true;
                    break;
                }
            }

            if (!started)
            {
                var error = GetRecentError();
                _log.Warning($"Server failed to start: {error}", "UI");
                await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_start_failed"), error));
            }
            else
            {
                _log.Information("Server started successfully", "UI");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to start server", "UI");
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_start_error"), ex.Message));
        }
    }

    [RelayCommand]
    async Task StopServer()
    {
        try
        {
            await _serverManager.StopAsync();

            // Wait up to 5 seconds for server to stop
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                var status = await _serverManager.GetStatusAsync();
                if (status.State == Core.Enums.ServerState.Stopped)
                    break;
            }

            _log.Information("Server stopped", "UI");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to stop server", "UI");
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_stop_error"), ex.Message));
        }
    }

    [RelayCommand]
    async Task RestartServer()
    {
        try
        {
            // Stop first
            await _serverManager.StopAsync();
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                var status = await _serverManager.GetStatusAsync();
                if (status.State == Core.Enums.ServerState.Stopped)
                    break;
            }

            ClearRecentErrors();
            var profile = await GetActiveProfileAsync();
            if (profile != null && !string.IsNullOrWhiteSpace(profile.ModelPath))
            {
                await _serverManager.StartAsync(profile);

                // Wait for server to start
                bool started = false;
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    var status = await _serverManager.GetStatusAsync();
                    if (status.State == Core.Enums.ServerState.Running)
                    {
                        started = true;
                        break;
                    }
                    if (status.State == Core.Enums.ServerState.Error)
                        break;
                }

                if (!started)
                {
                    var error = GetRecentError();
                    _log.Warning($"Server failed to restart: {error}", "UI");
                    await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_restart_failed"), error));
                }
                else
                {
                    _log.Information("Server restarted successfully", "UI");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to restart server", "UI");
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_restart_error"), ex.Message));
        }
    }

    async Task RefreshDashboardAfterRestart()
    {
        try
        {
            await _dashboard.RefreshAsync();
        }
        catch { }
    }

    async Task RefreshServerStatus()
    {
        try
        {
            // Always do a real health check to detect externally running server (KeepServerOnExit)
            // Health check always on 127.0.0.1 — DefaultHost may be 0.0.0.0 which is bind address, not connectable
            var host = "127.0.0.1";
            var port = _settings.DefaultPort > 0 ? _settings.DefaultPort : 8080;

            // Also try the last known host/port from internal status
            var internalStatus = await _serverManager.GetStatusAsync();
            if (internalStatus.State == Core.Enums.ServerState.Running)
            {
                host = internalStatus.Host ?? host;
                port = internalStatus.Port > 0 ? internalStatus.Port : port;
            }

            var health = await _serverManager.HealthCheckAsync(host, port);

           if (health.State == Core.Enums.ServerState.Running)
            {
                // External server detected — auto-restart with active profile for log capture
                var profile = await GetActiveProfileAsync();
                if (profile != null)
                {
                    if (!string.IsNullOrWhiteSpace(_settings.DefaultHost))
                        profile.Host = _settings.DefaultHost;
                    if (_settings.DefaultPort > 0)
                        profile.Port = _settings.DefaultPort;

                    // Show main window so loading dialog is visible (even if started in tray)
                    bool wasHidden = false;
                    try
                    {
                        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                        if (desktop?.MainWindow != null && !desktop.MainWindow.IsVisible)
                        {
                            desktop.MainWindow.Show();
                            desktop.MainWindow.Activate();
                            wasHidden = true;
                        }
                    }
                    catch { }

                    var loadingWin = _dialog.ShowLoading(
                        string.Format(_loc.T("server.restart_profile"), profile.Name),
                        "Загрузка");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _serverManager.StopAsync();
                            await Task.Delay(1500);
                            await _serverManager.StartAsync(profile);
                            _log.Information($"Server restarted with profile: {profile.Name}", "Main");
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Failed to restart server", "Main");
                        }

                        // Close loading dialog and hide window again ONLY if MinimizeToTray is enabled
                        Dispatcher.UIThread.Post(() =>
                        {
                            try { loadingWin?.Dispose(); } catch { }
                            if (wasHidden && _settings.MinimizeToTray)
                            {
                                try
                                {
                                    var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                                    desktop?.MainWindow?.Hide();
                                }
                                catch { }
                            }
                            CanStartServer = false;
                            CanStopServer = true;
                            CanRestartServer = true;
                            OnPropertyChanged(nameof(StartBtnText));
                            OnPropertyChanged(nameof(StopBtnText));
                            _ = RefreshDashboardAfterRestart();
                        });
                    });

                    // Set initial state while waiting
                    CanStartServer = false;
                    CanStopServer = false;
                    CanRestartServer = false;
                }
                else
                {
                    // No default profile — just attach to external server
                    _serverManager.AttachExternalServer(host, port);
                    CanStartServer = false;
                    CanStopServer = true;
                    CanRestartServer = true;
                }

                OnPropertyChanged(nameof(StartBtnText));
                OnPropertyChanged(nameof(StopBtnText));
            }
            else
            {
                CanStartServer = true;
                CanStopServer = false;
                CanRestartServer = true;
            }

            OnPropertyChanged(nameof(StartBtnText));
            OnPropertyChanged(nameof(StopBtnText));
        }
        catch (Exception ex)
        {
            _log.Debug($"Startup health check failed: {ex.Message}", "Main");
            CanStartServer = true;
            CanStopServer = false;
            CanRestartServer = true;
        }
    }

    void OnServerStatusChanged(object? sender, ServerStatus status)
    {
        CanStopServer = status.State == Core.Enums.ServerState.Running ||
                        status.State == Core.Enums.ServerState.Starting;
        CanStartServer = status.State == Core.Enums.ServerState.Stopped ||
                         status.State == Core.Enums.ServerState.Error;
        CanRestartServer = status.State == Core.Enums.ServerState.Running ||
                           status.State == Core.Enums.ServerState.Stopped ||
                           status.State == Core.Enums.ServerState.Error;

        OnPropertyChanged(nameof(StartBtnText));
        OnPropertyChanged(nameof(StopBtnText));
    }

    async Task<Core.Models.ServerProfile?> GetActiveProfileAsync()
    {
        Core.Models.ServerProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
        profile ??= await _profileManager.GetDefaultProfileAsync();
        return profile;
    }
}
