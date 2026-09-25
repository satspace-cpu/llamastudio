using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Services;
using LlamaStudio.Views.Pages;

namespace LlamaStudio.ViewModels;

public class MainViewModel : ObservableObject
{
	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly ILocalizationService _loc;

	private readonly IDialogService _dialog;

	private readonly DashboardViewModel _dashboard;

	private readonly ModelsViewModel _models;

	private readonly ServerViewModel _server;

	private readonly ProfilesViewModel _profiles;

	private readonly LogsViewModel _logs;

	private readonly SettingsViewModel _settingsVm;

	private readonly ApiTestViewModel _apiTest;

	private readonly ChatViewModel _chat;

	private readonly MonitoringViewModel _monitoringVm;

	private readonly BenchmarkCenterViewModel _benchmarkVm;

	private readonly LlamaReleasesViewModel _llamaReleases;

	private readonly SupportViewModel _support;

	private readonly WebUiService _webUiService;

	[ObservableProperty]
	private string _selectedPage = "Dashboard";

	[ObservableProperty]
	private Control? _currentPage;

	[ObservableProperty]
	private bool _canStartServer = true;

	[ObservableProperty]
	private bool _canStopServer;

	[ObservableProperty]
	private bool _canRestartServer;

	private int _startupStatusCheckInProgress;

	private List<string> _recentErrors = new List<string>();

	private object _errorLock = new object();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<string>? navigateCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? startServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? stopServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? restartServerCommand;

	private RelayCommand? openWebUiCommand;

	public string StartBtnText
	{
		get
		{
			if (!CanStartServer)
			{
				return _loc.T("main.server_running");
			}
			return _loc.T("main.start_server");
		}
	}

	public string StopBtnText
	{
		get
		{
			if (!CanStopServer)
			{
				return _loc.T("main.server_stopped");
			}
			return _loc.T("main.stop_server");
		}
	}

	public string NavDashboard => _loc.T("main.dashboard");

	public string NavModels => _loc.T("main.models");

	public string NavServer => _loc.T("main.server");

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

	public string WebUiStatusText => _webUiService.IsRunning ? "Веб-сервер запущен" : "Веб-сервер выключен";

	public string WebUiAddress
	{
		get
		{
			string host = string.IsNullOrWhiteSpace(_settings.WebUiHost) ? "127.0.0.1" : _settings.WebUiHost.Trim();
			int port = _settings.WebUiPort > 0 ? _settings.WebUiPort : 8090;
			return host + ":" + port;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedPage
	{
		get
		{
			return _selectedPage;
		}
		[MemberNotNull("_selectedPage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedPage, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedPage);
				_selectedPage = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedPage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public Control? CurrentPage
	{
		get
		{
			return _currentPage;
		}
		set
		{
			if (!EqualityComparer<Control>.Default.Equals(_currentPage, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CurrentPage);
				_currentPage = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CurrentPage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CanStartServer
	{
		get
		{
			return _canStartServer;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_canStartServer, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CanStartServer);
				_canStartServer = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CanStartServer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CanStopServer
	{
		get
		{
			return _canStopServer;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_canStopServer, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CanStopServer);
				_canStopServer = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CanStopServer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CanRestartServer
	{
		get
		{
			return _canRestartServer;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_canRestartServer, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CanRestartServer);
				_canRestartServer = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CanRestartServer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> NavigateCommand => navigateCommand ?? (navigateCommand = new RelayCommand<string>(Navigate));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncRelayCommand(StartServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncRelayCommand(StopServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestartServerCommand => restartServerCommand ?? (restartServerCommand = new AsyncRelayCommand(RestartServer));

	public IRelayCommand OpenWebUiCommand => openWebUiCommand ?? (openWebUiCommand = new RelayCommand(OpenWebUi));

	public MainViewModel(IServerManager serverManager, IProfileManager profileManager, ISettings settings, ILogService log, ILocalizationService loc, IDialogService dialog, DashboardViewModel dashboard, ModelsViewModel models, ServerViewModel server, ProfilesViewModel profiles, LogsViewModel logs, SettingsViewModel settingsVm, ApiTestViewModel apiTest, ChatViewModel chat, MonitoringViewModel monitoringVm, BenchmarkCenterViewModel benchmarkVm, LlamaReleasesViewModel llamaReleases, SupportViewModel support, WebUiService webUiService)
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
		_benchmarkVm = benchmarkVm;
		_llamaReleases = llamaReleases;
		_support = support;
		_webUiService = webUiService;
		_serverManager.StatusChanged += OnServerStatusChanged;
		_serverManager.LogReceived += OnServerLogReceived;
		_loc.OnLanguageChanged += OnLanguageChanged;
		NavigateTo("Dashboard");
		InitializeAsync();
	}

	private void OnServerLogReceived(object? sender, LogEntry entry)
	{
		if (entry.Level != LogLevel.Error && entry.Level != LogLevel.Fatal)
		{
			return;
		}
		lock (_errorLock)
		{
			_recentErrors.Add(entry.Message);
			if (_recentErrors.Count > 20)
			{
				_recentErrors.RemoveAt(0);
			}
		}
	}

	private string GetRecentError()
	{
		lock (_errorLock)
		{
			string result;
			if (_recentErrors.Count <= 0)
			{
				result = _loc.T("server.unknown_error");
			}
			else
			{
				List<string> recentErrors = _recentErrors;
				result = recentErrors[recentErrors.Count - 1];
			}
			return result;
		}
	}

	private void ClearRecentErrors()
	{
		lock (_errorLock)
		{
			_recentErrors.Clear();
		}
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		OnPropertyChanged("NavDashboard");
		OnPropertyChanged("NavModels");
		OnPropertyChanged("NavServer");
		OnPropertyChanged("NavLogs");
		OnPropertyChanged("NavApiTest");
		OnPropertyChanged("NavChat");
		OnPropertyChanged("NavMonitoring");
		OnPropertyChanged("NavLlamaReleases");
		OnPropertyChanged("NavSettings");
		OnPropertyChanged("NavSupport");
		OnPropertyChanged("BtnStartServer");
		OnPropertyChanged("BtnStopServer");
		OnPropertyChanged("BtnRestartServer");
		OnPropertyChanged("Subtitle");
	}

	private async Task InitializeAsync()
	{
		try
		{
			await _profiles.LoadProfilesAsync();
			await _dashboard.RefreshAsync();
			if (!string.IsNullOrWhiteSpace(_settings.ModelsDirectory) && Directory.Exists(_settings.ModelsDirectory))
			{
				await _models.ScanModels();
			}
			_log.Information("Application initialized", "Main");
			await RefreshServerStatus();
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Initialization failed", "Main");
		}
	}

	public void NavigateTo(string page)
	{
		try
		{
			_log.Information($"NavigateTo called: page='{page}', SelectedPage='{SelectedPage}', CurrentPage={CurrentPage?.GetType().Name}", "Main");
			if (page == "Chat")
			{
				OpenLlamaServerInBrowser();
				return;
			}
			if (SelectedPage == page && CurrentPage != null)
			{
				_log.Information("NavigateTo skipped: already on page '" + page + "'", "Main");
				return;
			}
			SelectedPage = page;
			_log.Information("SelectedPage set to '" + page + "'", "Main");
			if (page == "Dashboard")
			{
				_dashboard.RefreshAsync();
			}
			Control control = page switch
			{
				"Dashboard" => new DashboardPage(_dashboard), 
				"Models" => new ModelsPage(_models), 
				"Server" => new ServerPage(_server), 
				"Logs" => new LogsPage(_logs), 
				"ApiTest" => new ApiTestPage(_apiTest), 
				"Chat" => new ChatPage(_chat), 
				"Monitoring" => new MonitoringPage(_monitoringVm, _benchmarkVm), 
				"LlamaReleases" => new LlamaReleasesPage(_llamaReleases), 
				"Settings" => new SettingsPage(_settingsVm), 
				"Support" => new SupportPage(_support), 
				_ => new DashboardPage(_dashboard), 
			};
			_log.Information("Created page control: " + control.GetType().Name, "Main");
			CurrentPage = control;
			_log.Information("CurrentPage set to " + control.GetType().Name, "Main");
		}
		catch (Exception exception)
		{
			_log.Error(exception, "NavigateTo(" + page + ") failed", "Main");
		}
	}

	private void OpenLlamaServerInBrowser()
	{
		string host = string.IsNullOrWhiteSpace(_settings.DefaultHost) ? "127.0.0.1" : _settings.DefaultHost.Trim();
		int port = _settings.DefaultPort > 0 ? _settings.DefaultPort : 8080;

		if (host is "0.0.0.0" or "::")
		{
			host = "localhost";
		}
		else if (host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
		{
			// The bundled web UI keeps origin-specific state; localhost is the
			// working local origin for this server configuration.
			host = "localhost";
		}

		if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
			!host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			host = "http://" + host;
		}

		// Uri.Port returns the scheme default (80/443) even when the port was
		// not written in the settings. Always apply the configured server port.
		UriBuilder address = new UriBuilder(host)
		{
			Port = port
		};
		string url = address.Uri.ToString().TrimEnd('/');

		_log.Information("Opening Llama server in browser: " + url, "Main");
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});
	}

	private void OpenWebUi()
	{
		if (!_webUiService.IsRunning)
		{
			return;
		}
		string host = string.IsNullOrWhiteSpace(_settings.WebUiHost) ? "127.0.0.1" : _settings.WebUiHost.Trim();
		if (host is "0.0.0.0" or "::")
		{
			host = "localhost";
		}
		if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			host = "http://" + host;
		}
		UriBuilder address = new UriBuilder(host)
		{
			Port = _settings.WebUiPort > 0 ? _settings.WebUiPort : 8090
		};
		string url = address.Uri.ToString().TrimEnd('/');
		_log.Information("Opening Studio Web UI in browser: " + url, "Main");
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});
	}

	[RelayCommand]
	private void Navigate(string page)
	{
		try
		{
			NavigateTo(page);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "NavigateCommand(" + page + ") failed", "Main");
		}
	}

	[RelayCommand]
	private async Task StartServer()
	{
		try
		{
			string host = _settings.DefaultHost ?? "127.0.0.1";
			int port = ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080);
			if ((await _serverManager.HealthCheckAsync(host, port)).State == ServerState.Running)
			{
				CanStartServer = false;
				CanStopServer = true;
				CanRestartServer = true;
				_log.Information($"Server already running at {host}:{port}", "UI");
				await _dialog.ShowInfoAsync(_loc.T("dialog.server_already_running"));
				return;
			}
			// r111: a start is already in progress (e.g. auto-start on boot with a
			// big model still loading) — do not start a second server.
			if ((await _serverManager.GetStatusAsync()).State == ServerState.Starting)
			{
				_log.Information("Start requested while server is already starting", "UI");
				await _dialog.ShowInfoAsync(_loc.T("dialog.server_starting"));
				return;
			}
			ServerProfile profile = await GetActiveProfileAsync();
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
			if (string.IsNullOrWhiteSpace(profile.Host) && !string.IsNullOrWhiteSpace(_settings.DefaultHost))
			{
				profile.Host = _settings.DefaultHost;
			}
			if (profile.Port <= 0 && _settings.DefaultPort > 0)
			{
				profile.Port = _settings.DefaultPort;
			}
			ClearRecentErrors();
			await _serverManager.StartAsync(profile);
			bool started = false;
			int p = ((profile.Port > 0) ? profile.Port : port);
			string h = profile.Host ?? host;
			for (int i = 0; i < 30; i++)
			{
				await Task.Delay(500);
				ServerStatus serverStatus = await _serverManager.GetStatusAsync();
				if (serverStatus.State == ServerState.Running)
				{
					started = true;
					break;
				}
				if (serverStatus.State == ServerState.Error)
				{
					break;
				}
				if ((await _serverManager.HealthCheckAsync(h, p)).State == ServerState.Running)
				{
					started = true;
					break;
				}
			}
			if (!started)
			{
				string recentError = GetRecentError();
				_log.Warning("Server failed to start: " + recentError, "UI");
				await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_start_failed"), recentError));
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
	private async Task StopServer()
	{
		try
		{
			await _serverManager.StopAsync();
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(500);
				if ((await _serverManager.GetStatusAsync()).State == ServerState.Stopped)
				{
					break;
				}
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
	private async Task RestartServer()
	{
		try
		{
			await _serverManager.StopAsync();
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(500);
				if ((await _serverManager.GetStatusAsync()).State == ServerState.Stopped)
				{
					break;
				}
			}
			ClearRecentErrors();
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile == null || string.IsNullOrWhiteSpace(serverProfile.ModelPath))
			{
				return;
			}
			await _serverManager.StartAsync(serverProfile);
			bool started = false;
			for (int i = 0; i < 30; i++)
			{
				await Task.Delay(500);
				ServerStatus serverStatus = await _serverManager.GetStatusAsync();
				if (serverStatus.State == ServerState.Running)
				{
					started = true;
					break;
				}
				if (serverStatus.State == ServerState.Error)
				{
					break;
				}
			}
			if (!started)
			{
				string recentError = GetRecentError();
				_log.Warning("Server failed to restart: " + recentError, "UI");
				await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_restart_failed"), recentError));
			}
			else
			{
				_log.Information("Server restarted successfully", "UI");
			}
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to restart server", "UI");
			await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.server_restart_error"), ex.Message));
		}
	}

	private async Task RefreshDashboardAfterRestart()
	{
		try
		{
			await _dashboard.RefreshAsync();
		}
		catch
		{
		}
	}

	private async Task RefreshServerStatus()
	{
		if (Interlocked.Exchange(ref _startupStatusCheckInProgress, 1) != 0)
		{
			return;
		}
		try
		{
			string host = "127.0.0.1";
			int port = ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080);
			ServerStatus serverStatus = await _serverManager.GetStatusAsync();
			if (serverStatus.State == ServerState.Running)
			{
				host = serverStatus.Host ?? host;
				port = ((serverStatus.Port > 0) ? serverStatus.Port : port);
			}
			if ((await _serverManager.HealthCheckAsync(host, port)).State == ServerState.Running)
			{
				ServerProfile profile = await GetActiveProfileAsync();
				if (profile != null)
				{
					if (string.IsNullOrWhiteSpace(profile.Host) && !string.IsNullOrWhiteSpace(_settings.DefaultHost))
					{
						profile.Host = _settings.DefaultHost;
					}
					if (profile.Port <= 0 && _settings.DefaultPort > 0)
					{
						profile.Port = _settings.DefaultPort;
					}
					bool wasHidden = false;
					try
					{
						IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
						if (classicDesktopStyleApplicationLifetime?.MainWindow != null && !classicDesktopStyleApplicationLifetime.MainWindow.IsVisible)
						{
							classicDesktopStyleApplicationLifetime.MainWindow.Show();
							classicDesktopStyleApplicationLifetime.MainWindow.Activate();
							wasHidden = true;
						}
					}
					catch
					{
					}
					IDisposable loadingWin = _dialog.ShowLoading(string.Format(_loc.T("server.restart_profile"), profile.Name), "Загрузка");
					Task.Run(async delegate
					{
						try
						{
							await _serverManager.StopAsync();
							await Task.Delay(1500);
							await _serverManager.StartAsync(profile);
							_log.Information("Server restarted with profile: " + profile.Name, "Main");
						}
						catch (Exception exception)
						{
							_log.Error(exception, "Failed to restart server", "Main");
						}
						Dispatcher.UIThread.Post(delegate
						{
							try
							{
								loadingWin?.Dispose();
							}
							catch
							{
							}
							if (wasHidden && _settings.MinimizeToTray)
							{
								try
								{
									(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Hide();
								}
								catch
								{
								}
							}
							CanStartServer = false;
							CanStopServer = true;
							CanRestartServer = true;
							OnPropertyChanged("StartBtnText");
							OnPropertyChanged("StopBtnText");
							RefreshDashboardAfterRestart();
						});
					});
					CanStartServer = false;
					CanStopServer = false;
					CanRestartServer = false;
				}
				else
				{
					_serverManager.AttachExternalServer(host, port);
					CanStartServer = false;
					CanStopServer = true;
					CanRestartServer = true;
				}
				OnPropertyChanged("StartBtnText");
				OnPropertyChanged("StopBtnText");
			}
			else if (serverStatus.State == ServerState.Starting)
			{
				// r111: a start is in progress (auto-start on app launch while the
				// model is still loading). Do NOT revert to the active
				// "Start server" button — that made users press "Start" again
				// while the server was already loading.
				CanStartServer = false;
				CanStopServer = true;
				CanRestartServer = true;
			}
			else
			{
				CanStartServer = true;
				CanStopServer = false;
				CanRestartServer = true;
			}
			OnPropertyChanged("StartBtnText");
			OnPropertyChanged("StopBtnText");
		}
		catch (Exception ex)
		{
			_log.Debug("Startup health check failed: " + ex.Message, "Main");
			CanStartServer = true;
			CanStopServer = false;
			CanRestartServer = true;
		}
		finally
		{
			Volatile.Write(ref _startupStatusCheckInProgress, 0);
		}
	}

	private void OnServerStatusChanged(object? sender, ServerStatus status)
	{
		CanStopServer = status.State == ServerState.Running || status.State == ServerState.Starting;
		CanStartServer = status.State == ServerState.Stopped || status.State == ServerState.Error;
		CanRestartServer = status.State == ServerState.Running || status.State == ServerState.Stopped || status.State == ServerState.Error;
		OnPropertyChanged("StartBtnText");
		OnPropertyChanged("StopBtnText");
	}

	private async Task<ServerProfile?> GetActiveProfileAsync()
	{
		// ServerViewModel has already loaded and applied the selected profile during
		// application construction. Prefer that live instance so startup cannot race
		// with the asynchronous profile save and launch stale 50/50 GPU settings.
		if (_server?.SelectedProfile != null && (string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId) || _server.SelectedProfile.Id == _settings.LastSelectedProfileId))
		{
			return _server.SelectedProfile;
		}
		ServerProfile serverProfile = null;
		if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
		{
			serverProfile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
		}
		if (serverProfile == null)
		{
			serverProfile = await _profileManager.GetDefaultProfileAsync();
		}
		return serverProfile;
	}
}
