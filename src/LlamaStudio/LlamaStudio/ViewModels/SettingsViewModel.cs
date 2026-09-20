using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Services;
using Microsoft.Win32;

namespace LlamaStudio.ViewModels;

public class SettingsViewModel : ObservableObject, IDisposable
{
	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly ILocalizationService _loc;

	private readonly IAppUpdater _appUpdater;

	private readonly IVersionCache _versionCache;

	private Timer? _autoSaveTimer;

	private bool _isDisposing;

	[ObservableProperty]
	private string _llamaCppBaseDirectory;

	[ObservableProperty]
	private string _llamaCppDirectory;

	[ObservableProperty]
	private string _modelsDirectory;

	[ObservableProperty]
	private string _theme = "Dark";

	[ObservableProperty]
	private bool _autoCheckUpdates = true;

	[ObservableProperty]
	private bool _minimizeToTray = true;

	[ObservableProperty]
	private bool _startMinimized;

	[ObservableProperty]
	private bool _autoStartWithOS;

	[ObservableProperty]
	private bool _startMonitoringWindow = true;

	private bool _showDesktopWindow = true;

	[ObservableProperty]
	private bool _keepServerOnExit;

	[ObservableProperty]
	private bool _autoStartServer;

	[ObservableProperty]
	private string _defaultHost = "127.0.0.1";

	[ObservableProperty]
	private int _defaultPort = 8080;

	[ObservableProperty]
	private string _defaultGpuLayers = "all";

	[ObservableProperty]
	private bool _flashAttention = true;

	[ObservableProperty]
	private int _themeIndex;

	[ObservableProperty]
	private string _updateChannel = "Stable";

	[ObservableProperty]
	private int _updateChannelIndex;

	[ObservableProperty]
	private string _selectedLanguage = "en";

	[ObservableProperty]
	private string _updateStatus;

	[ObservableProperty]
	private string _appVersion = "0.0.0";

	[ObservableProperty]
	private bool _isCheckingUpdates;

	[ObservableProperty]
	private bool _hasUpdateAvailable;

	[ObservableProperty]
	private string _latestVersion = "";

	[ObservableProperty]
	private string _updateChangelog = "";

	[ObservableProperty]
	private bool _isDownloading;

	[ObservableProperty]
	private double _downloadProgress;

	[ObservableProperty]
	private double _monitorOpacity = 0.92;

	[ObservableProperty]
	private bool _monitorAlwaysOnTop = true;

	[ObservableProperty]
	private bool _monitorShowTps = true;

	[ObservableProperty]
	private bool _monitorShowVram = true;

	[ObservableProperty]
	private bool _monitorShowRam = true;

	[ObservableProperty]
	private bool _monitorShowGpuTemp = true;

	[ObservableProperty]
	private bool _monitorShowPowerDraw = true;

	[ObservableProperty]
	private bool _monitorShowGpuCore = true;

	[ObservableProperty]
	private bool _monitorShowFanSpeed = true;

	[ObservableProperty]
	private string _defaultPortText = "8080";

	[ObservableProperty]
	private string _defaultGpuLayersText = "all";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveSettingsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectLlamaCppBaseDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectLlamaCppDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectModelsDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? checkForUpdatesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? downloadUpdateCommand;

	public string[] UpdateChannelOptions => new string[3]
	{
		_loc.T("settings.stable"),
		_loc.T("settings.prerelease"),
		_loc.T("settings.nightly")
	};

	public string MonitorOpacityPercent => $"{MonitorOpacity * 100.0:F0}%";

	public string[] LanguageOptions => LocalizationService.SupportedLanguages;

	public string[] ThemeOptions => new string[1]
	{
		_loc.T("settings.dark")
	};

	public string ThemeDisplay
	{
		get
		{
			string theme = Theme;
			if (!(theme == "Light"))
			{
				if (theme == "System")
				{
					return _loc.T("settings.system");
				}
				return _loc.T("settings.dark");
			}
			return _loc.T("settings.light");
		}
	}

	public string Title => _loc.T("settings.title");

	public string GeneralSection => _loc.T("settings.general_section");

	public string LanguageLabel => _loc.T("settings.language_label");

	public string ThemeLabel => _loc.T("settings.theme_label");

	public string AutoCheckUpdatesLabel => _loc.T("settings.auto_check_updates");

	public string MinimizeToTrayLabel => _loc.T("settings.minimize_to_tray");

	public string StartMinimizedLabel => _loc.T("settings.start_minimized");

	public string AutoStartWithOSLabel => _loc.T("settings.auto_start_with_os");

	public string StartMonitoringWindowLabel => _loc.T("settings.start_monitoring_window");

	public string ShowDesktopWindowLabel => _loc.T("settings.show_desktop_window");

	public string KeepServerOnExitLabel => _loc.T("settings.keep_server_on_exit");

	public string AutoStartServerLabel => _loc.T("settings.auto_start_server");

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

	public string AppVersionLabel => _loc.T("dash.app_version");

	public string AppUpdateStatus
	{
		get
		{
			if (!HasUpdateAvailable)
			{
				return "✓ " + _loc.T("dash.up_to_date");
			}
			return "↑ " + _loc.T("dash.new_version") + " " + LatestVersion;
		}
	}

	public string CheckUpdatesBtn
	{
		get
		{
			if (!IsCheckingUpdates)
			{
				return _loc.T("dash.check_updates");
			}
			return _loc.T("dash.checking_update");
		}
	}

	public string DownloadUpdateBtn
	{
		get
		{
			if (!IsDownloading)
			{
				return _loc.T("dash.update") + " (" + LatestVersion + ")";
			}
			return $"{_loc.T("dash.downloading")} {DownloadProgress:F0}%";
		}
	}

	public string TtLanguage => _loc.T("tt.language");

	public string TtLlamaCppBaseDir => _loc.T("tt.llama_cpp_base_dir");

	public string TtLlamaCppDir => _loc.T("tt.llama_cpp_dir");

	public string TtModelsDir => _loc.T("tt.models_dir");

	public string TtAutoCheckUpdates => _loc.T("tt.auto_check_updates");

	public string TtMinimizeToTray => _loc.T("tt.minimize_to_tray");

	public string TtStartMinimized => _loc.T("tt.start_minimized");

	public string TtAutoStartWithOS => _loc.T("tt.auto_start_with_os");

	public string TtStartMonitoringWindow => _loc.T("tt.start_monitoring_window");

	public string TtShowDesktopWindow => _loc.T("tt.show_desktop_window");

	public string TtKeepServerOnExit => _loc.T("tt.keep_server_on_exit");

	public string TtAutoStartServer => _loc.T("tt.auto_start_server");

	public string TtTheme => _loc.T("tt.theme");

	public string TtUpdateChannel => _loc.T("tt.update_channel");

	public string TtHost => _loc.T("tt.host");

	public string TtHostDefault => _loc.T("tt.host_default");

	public string TtPort => _loc.T("tt.port");

	public string TtPortManual => _loc.T("tt.port_manual");

	public string TtGpuLayers => _loc.T("tt.gpu_layers");

	public string TtGpuLayersDefault => _loc.T("tt.gpu_layers_default");

	public string TtFlashAttention => _loc.T("tt.flash_attention");

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LlamaCppBaseDirectory
	{
		get
		{
			return _llamaCppBaseDirectory;
		}
		[MemberNotNull("_llamaCppBaseDirectory")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_llamaCppBaseDirectory, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LlamaCppBaseDirectory);
				_llamaCppBaseDirectory = value;
				OnLlamaCppBaseDirectoryChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LlamaCppBaseDirectory);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LlamaCppDirectory
	{
		get
		{
			return _llamaCppDirectory;
		}
		[MemberNotNull("_llamaCppDirectory")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_llamaCppDirectory, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LlamaCppDirectory);
				_llamaCppDirectory = value;
				OnLlamaCppDirectoryChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LlamaCppDirectory);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelsDirectory
	{
		get
		{
			return _modelsDirectory;
		}
		[MemberNotNull("_modelsDirectory")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelsDirectory, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelsDirectory);
				_modelsDirectory = value;
				OnModelsDirectoryChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelsDirectory);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Theme
	{
		get
		{
			return _theme;
		}
		[MemberNotNull("_theme")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_theme, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Theme);
				_theme = value;
				OnThemeChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Theme);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoCheckUpdates
	{
		get
		{
			return _autoCheckUpdates;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoCheckUpdates, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AutoCheckUpdates);
				_autoCheckUpdates = value;
				OnAutoCheckUpdatesChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AutoCheckUpdates);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MinimizeToTray
	{
		get
		{
			return _minimizeToTray;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_minimizeToTray, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MinimizeToTray);
				_minimizeToTray = value;
				OnMinimizeToTrayChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MinimizeToTray);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool StartMinimized
	{
		get
		{
			return _startMinimized;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_startMinimized, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.StartMinimized);
				_startMinimized = value;
				OnStartMinimizedChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.StartMinimized);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoStartWithOS
	{
		get
		{
			return _autoStartWithOS;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoStartWithOS, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AutoStartWithOS);
				_autoStartWithOS = value;
				OnAutoStartWithOSChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AutoStartWithOS);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool StartMonitoringWindow
	{
		get
		{
			return _startMonitoringWindow;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_startMonitoringWindow, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.StartMonitoringWindow);
				_startMonitoringWindow = value;
				OnStartMonitoringWindowChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.StartMonitoringWindow);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowDesktopWindow
	{
		get
		{
			return _showDesktopWindow;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showDesktopWindow, value))
			{
				OnPropertyChanging("ShowDesktopWindow");
				_showDesktopWindow = value;
				OnShowDesktopWindowChanged(value);
				OnPropertyChanged("ShowDesktopWindow");
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool KeepServerOnExit
	{
		get
		{
			return _keepServerOnExit;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_keepServerOnExit, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.KeepServerOnExit);
				_keepServerOnExit = value;
				OnKeepServerOnExitChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.KeepServerOnExit);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoStartServer
	{
		get
		{
			return _autoStartServer;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoStartServer, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AutoStartServer);
				_autoStartServer = value;
				OnAutoStartServerChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AutoStartServer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DefaultHost
	{
		get
		{
			return _defaultHost;
		}
		[MemberNotNull("_defaultHost")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_defaultHost, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DefaultHost);
				_defaultHost = value;
				OnDefaultHostChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DefaultHost);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int DefaultPort
	{
		get
		{
			return _defaultPort;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_defaultPort, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DefaultPort);
				_defaultPort = value;
				OnDefaultPortChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DefaultPort);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DefaultGpuLayers
	{
		get
		{
			return _defaultGpuLayers;
		}
		[MemberNotNull("_defaultGpuLayers")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_defaultGpuLayers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DefaultGpuLayers);
				_defaultGpuLayers = value;
				OnDefaultGpuLayersChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DefaultGpuLayers);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool FlashAttention
	{
		get
		{
			return _flashAttention;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_flashAttention, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FlashAttention);
				_flashAttention = value;
				OnFlashAttentionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FlashAttention);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ThemeIndex
	{
		get
		{
			return _themeIndex;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_themeIndex, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ThemeIndex);
				_themeIndex = value;
				OnThemeIndexChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ThemeIndex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UpdateChannel
	{
		get
		{
			return _updateChannel;
		}
		[MemberNotNull("_updateChannel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_updateChannel, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateChannel);
				_updateChannel = value;
				OnUpdateChannelChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateChannel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int UpdateChannelIndex
	{
		get
		{
			return _updateChannelIndex;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_updateChannelIndex, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateChannelIndex);
				_updateChannelIndex = value;
				OnUpdateChannelIndexChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateChannelIndex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedLanguage
	{
		get
		{
			return _selectedLanguage;
		}
		[MemberNotNull("_selectedLanguage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedLanguage, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedLanguage);
				_selectedLanguage = value;
				OnSelectedLanguageChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedLanguage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UpdateStatus
	{
		get
		{
			return _updateStatus;
		}
		[MemberNotNull("_updateStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_updateStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateStatus);
				_updateStatus = value;
				OnUpdateStatusChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string AppVersion
	{
		get
		{
			return _appVersion;
		}
		[MemberNotNull("_appVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_appVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AppVersion);
				_appVersion = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AppVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsCheckingUpdates
	{
		get
		{
			return _isCheckingUpdates;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCheckingUpdates, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCheckingUpdates);
				_isCheckingUpdates = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCheckingUpdates);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasUpdateAvailable
	{
		get
		{
			return _hasUpdateAvailable;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasUpdateAvailable, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HasUpdateAvailable);
				_hasUpdateAvailable = value;
				OnHasUpdateAvailableChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HasUpdateAvailable);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LatestVersion
	{
		get
		{
			return _latestVersion;
		}
		[MemberNotNull("_latestVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_latestVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LatestVersion);
				_latestVersion = value;
				OnLatestVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LatestVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UpdateChangelog
	{
		get
		{
			return _updateChangelog;
		}
		[MemberNotNull("_updateChangelog")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_updateChangelog, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateChangelog);
				_updateChangelog = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateChangelog);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDownloading
	{
		get
		{
			return _isDownloading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDownloading, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsDownloading);
				_isDownloading = value;
				OnIsDownloadingChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsDownloading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double DownloadProgress
	{
		get
		{
			return _downloadProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_downloadProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DownloadProgress);
				_downloadProgress = value;
				OnDownloadProgressChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DownloadProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double MonitorOpacity
	{
		get
		{
			return _monitorOpacity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_monitorOpacity, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorOpacity);
				_monitorOpacity = value;
				OnMonitorOpacityChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorOpacity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorAlwaysOnTop
	{
		get
		{
			return _monitorAlwaysOnTop;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorAlwaysOnTop, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorAlwaysOnTop);
				_monitorAlwaysOnTop = value;
				OnMonitorAlwaysOnTopChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorAlwaysOnTop);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowTps
	{
		get
		{
			return _monitorShowTps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowTps, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowTps);
				_monitorShowTps = value;
				OnMonitorShowTpsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowTps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowVram
	{
		get
		{
			return _monitorShowVram;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowVram, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowVram);
				_monitorShowVram = value;
				OnMonitorShowVramChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowVram);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowRam
	{
		get
		{
			return _monitorShowRam;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowRam, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowRam);
				_monitorShowRam = value;
				OnMonitorShowRamChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowRam);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowGpuTemp
	{
		get
		{
			return _monitorShowGpuTemp;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowGpuTemp, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowGpuTemp);
				_monitorShowGpuTemp = value;
				OnMonitorShowGpuTempChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowGpuTemp);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowPowerDraw
	{
		get
		{
			return _monitorShowPowerDraw;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowPowerDraw, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowPowerDraw);
				_monitorShowPowerDraw = value;
				OnMonitorShowPowerDrawChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowPowerDraw);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowGpuCore
	{
		get
		{
			return _monitorShowGpuCore;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowGpuCore, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowGpuCore);
				_monitorShowGpuCore = value;
				OnMonitorShowGpuCoreChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowGpuCore);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MonitorShowFanSpeed
	{
		get
		{
			return _monitorShowFanSpeed;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_monitorShowFanSpeed, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MonitorShowFanSpeed);
				_monitorShowFanSpeed = value;
				OnMonitorShowFanSpeedChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MonitorShowFanSpeed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DefaultPortText
	{
		get
		{
			return _defaultPortText;
		}
		[MemberNotNull("_defaultPortText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_defaultPortText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DefaultPortText);
				_defaultPortText = value;
				OnDefaultPortTextChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DefaultPortText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DefaultGpuLayersText
	{
		get
		{
			return _defaultGpuLayersText;
		}
		[MemberNotNull("_defaultGpuLayersText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_defaultGpuLayersText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DefaultGpuLayersText);
				_defaultGpuLayersText = value;
				OnDefaultGpuLayersTextChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DefaultGpuLayersText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveSettingsCommand => saveSettingsCommand ?? (saveSettingsCommand = new AsyncRelayCommand(SaveSettings));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectLlamaCppBaseDirectoryCommand => selectLlamaCppBaseDirectoryCommand ?? (selectLlamaCppBaseDirectoryCommand = new AsyncRelayCommand(SelectLlamaCppBaseDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectLlamaCppDirectoryCommand => selectLlamaCppDirectoryCommand ?? (selectLlamaCppDirectoryCommand = new AsyncRelayCommand(SelectLlamaCppDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectModelsDirectoryCommand => selectModelsDirectoryCommand ?? (selectModelsDirectoryCommand = new AsyncRelayCommand(SelectModelsDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckForUpdatesCommand => checkForUpdatesCommand ?? (checkForUpdatesCommand = new AsyncRelayCommand(CheckForUpdates));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DownloadUpdateCommand => downloadUpdateCommand ?? (downloadUpdateCommand = new AsyncRelayCommand(DownloadUpdate));

	public SettingsViewModel(ISettings settings, ILogService log, IDialogService dialog, ILocalizationService loc, IAppUpdater appUpdater, IVersionCache versionCache)
	{
		_settings = settings;
		_log = log;
		_dialog = dialog;
		_loc = loc;
		_appUpdater = appUpdater;
		_versionCache = versionCache;
		AppVersion = _appUpdater.GetCurrentVersion();
		UpdateStatus = _loc.T("settings.not_checked");
		_appUpdater.StatusChanged += delegate(object? s, string msg)
		{
			UpdateStatus = msg;
		};
		_appUpdater.UpdateAvailable += delegate(object? s, AppUpdateInfo info)
		{
			HasUpdateAvailable = true;
			LatestVersion = info.Version;
			UpdateChangelog = info.Changelog;
		};
		LlamaCppBaseDirectory = _settings.LlamaCppBaseDirectory;
		LlamaCppDirectory = _settings.LlamaCppDirectory;
		ModelsDirectory = _settings.ModelsDirectory;
		Theme = "Dark";
		ThemeIndex = 0;
		AutoCheckUpdates = _settings.AutoCheckUpdates;
		MinimizeToTray = _settings.MinimizeToTray;
		StartMinimized = _settings.StartMinimized;
		AutoStartWithOS = _settings.AutoStartWithOS;
		StartMonitoringWindow = _settings.StartMonitoringWindow;
		ShowDesktopWindow = _settings.ShowDesktopWindow;
		ApplyAutoStart(_settings.AutoStartWithOS);
		KeepServerOnExit = _settings.KeepServerOnExit;
		AutoStartServer = _settings.AutoStartServer;
		DefaultHost = _settings.DefaultHost;
		DefaultPort = _settings.DefaultPort;
		DefaultPortText = _settings.DefaultPort.ToString();
		DefaultGpuLayers = _settings.DefaultGpuLayers ?? "all";
		DefaultGpuLayersText = _settings.DefaultGpuLayers ?? "all";
		FlashAttention = _settings.FlashAttention;
		UpdateChannel = _settings.UpdateChannel switch
		{
			LlamaStudio.Core.Enums.UpdateChannel.PreRelease => "PreRelease", 
			LlamaStudio.Core.Enums.UpdateChannel.Nightly => "Nightly", 
			_ => "Stable", 
		};
		UpdateChannelIndex = _settings.UpdateChannel switch
		{
			LlamaStudio.Core.Enums.UpdateChannel.PreRelease => 1, 
			LlamaStudio.Core.Enums.UpdateChannel.Nightly => 2, 
			_ => 0, 
		};
		SelectedLanguage = _loc.Language;
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
		// r74: keep this VM in sync with settings changed elsewhere (Web UI, Models
		// page, Server page). Without this, the Exit handler's ApplySettingsToModel()
		// (and the settings auto-save) writes back the stale snapshot captured at
		// app start and reverts user changes — e.g. the ModelsDirectory picked on
		// the Models page.
		_settings.SettingsChanged += ReloadFromSettings;
		if (_settings.AutoCheckUpdates)
		{
			Task.Run(async delegate
			{
				await Task.Delay(3000);
				try
				{
					AppUpdateInfo appUpdateInfo = await _appUpdater.CheckForUpdatesAsync();
					if (appUpdateInfo != null)
					{
						HasUpdateAvailable = true;
						LatestVersion = appUpdateInfo.Version;
						UpdateChangelog = appUpdateInfo.Changelog;
					}
					else
					{
						HasUpdateAvailable = false;
					}
				}
				catch
				{
				}
			});
		}
		_autoSaveTimer = new Timer(1000.0)
		{
			AutoReset = false
		};
		_autoSaveTimer.Elapsed += delegate
		{
			SaveSettingsInternalAsync();
		};
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] localizedPropertyNames = new string[60]
		{
			"UpdateStatus", "Title", "GeneralSection", "LanguageLabel", "ThemeLabel", "AutoCheckUpdatesLabel", "MinimizeToTrayLabel", "StartMinimizedLabel", "AutoStartWithOSLabel", "StartMonitoringWindowLabel", "ShowDesktopWindowLabel",
			"KeepServerOnExitLabel", "AutoStartServerLabel", "PathsSection", "LlamaCppBaseDirLabel", "LlamaCppDirLabel", "ModelsDirLabel", "BrowseBtn", "ServerDefaultsSection", "HostLabel", "PortLabel",
			"GpuLayersLabel", "FlashAttentionLabel", "UpdateChannelLabel", "SaveSettingsBtn", "DarkTheme", "LightTheme", "SystemTheme", "StableChannel", "PrereleaseChannel", "NightlyChannel",
			"RussianLanguage", "EnglishLanguage", "TtLanguage", "TtLlamaCppBaseDir", "TtLlamaCppDir", "TtModelsDir", "TtAutoCheckUpdates", "TtMinimizeToTray", "TtStartMinimized", "TtAutoStartWithOS",
			"TtStartMonitoringWindow", "TtShowDesktopWindow", "TtKeepServerOnExit", "TtAutoStartServer", "TtTheme", "TtUpdateChannel", "TtHost", "TtHostDefault", "TtPort", "TtPortManual", "TtGpuLayers",
			"TtGpuLayersDefault", "TtFlashAttention", "AppVersionLabel", "AppUpdateStatus", "CheckUpdatesBtn", "DownloadUpdateBtn", "ThemeOptions", "UpdateChannelOptions"
		};
		foreach (string propertyName in localizedPropertyNames)
		{
			OnPropertyChanged(propertyName);
		}
		Theme = "Dark";
		ThemeIndex = 0;
		UpdateChannel = _settings.UpdateChannel switch
		{
			LlamaStudio.Core.Enums.UpdateChannel.PreRelease => "PreRelease", 
			LlamaStudio.Core.Enums.UpdateChannel.Nightly => "Nightly", 
			_ => "Stable", 
		};
		UpdateChannelIndex = _settings.UpdateChannel switch
		{
			LlamaStudio.Core.Enums.UpdateChannel.PreRelease => 1, 
			LlamaStudio.Core.Enums.UpdateChannel.Nightly => 2, 
			_ => 0, 
		};
	}

	private static void ApplyTheme(string? theme)
	{
		App.ApplyTheme(theme);
	}

	private static void ApplyThemeColors(App app, bool lightMode)
	{
		if (lightMode)
		{
			app.Resources["ThemeBackground"] = new SolidColorBrush(Color.Parse("#F5F5F5"));
			app.Resources["ThemeSurface"] = new SolidColorBrush(Color.Parse("#FAFAFA"));
			app.Resources["ThemeCard"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["ThemeTextPrimary"] = new SolidColorBrush(Color.Parse("#1F2328"));
			app.Resources["ThemeTextSecondary"] = new SolidColorBrush(Color.Parse("#656D76"));
			app.Resources["ThemeTextTertiary"] = new SolidColorBrush(Color.Parse("#8B949E"));
			app.Resources["ThemeBorder"] = new SolidColorBrush(Color.Parse("#D0D7DE"));
			app.Resources["ThemeBorderStrong"] = new SolidColorBrush(Color.Parse("#C9D1D9"));
			app.Resources["ThemeInputBackground"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["ThemeAccent"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["ThemeAccentHover"] = new SolidColorBrush(Color.Parse("#1A7FCA"));
			app.Resources["CardServerBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardModelsBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardProfilesBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardVersionBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardModelBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardSectionBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["CardServerAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsAccent"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesAccent"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionAccent"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelAccent"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardServerLabel"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsLabel"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesLabel"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionLabel"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelLabel"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardSubText"] = new SolidColorBrush(Color.Parse("#6B7280"));
			app.Resources["GpuMonitorBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["GpuMonitorInner"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["GpuMonitorHeader"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressTrack"] = new SolidColorBrush(Color.Parse("#E1E4E8"));
			app.Resources["ProgressVram"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["ProgressCore"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["ProgressPower"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["ProgressFan"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressRam"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["SidebarBackground"] = new SolidColorBrush(Color.Parse("#F0F0F0"));
			app.Resources["SidebarItemBackground"] = new SolidColorBrush(Color.Parse("#E8E8E8"));
			app.Resources["SidebarItemSelected"] = new SolidColorBrush(Color.Parse("#D4D4D4"));
			app.Resources["SidebarItemText"] = new SolidColorBrush(Color.Parse("#57606A"));
			app.Resources["SidebarItemTextSelected"] = new SolidColorBrush(Color.Parse("#1F2328"));
			app.Resources["TabItemBackground"] = new SolidColorBrush(Color.Parse("#E8E8E8"));
			app.Resources["TabItemText"] = new SolidColorBrush(Color.Parse("#57606A"));
			app.Resources["TabItemSelectedBackground"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["TabItemSelectedText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemHoverBackground"] = new SolidColorBrush(Color.Parse("#D4D4D4"));
			app.Resources["ProfileInnerBackground"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["ProfileModelBackground"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["BadgeMtp"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BadgeReasoning"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BadgeFlash"] = new SolidColorBrush(Color.Parse("#2563EB"));
			app.Resources["BadgeCache"] = new SolidColorBrush(Color.Parse("#D97706"));
			app.Resources["BadgeBatching"] = new SolidColorBrush(Color.Parse("#0891B2"));
			app.Resources["BadgeText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["ServerBannerBackground"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
			app.Resources["ServerBannerLabel"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["ServerBannerSub"] = new SolidColorBrush(Color.Parse("#6B7280"));
			app.Resources["SectionTitle"] = new SolidColorBrush(Color.Parse("#1F2328"));
			app.Resources["SectionLabel"] = new SolidColorBrush(Color.Parse("#393F46"));
			app.Resources["SectionLabelLight"] = new SolidColorBrush(Color.Parse("#656D76"));
			app.Resources["BtnPrimary"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["BtnSuccess"] = new SolidColorBrush(Color.Parse("#2DA44E"));
			app.Resources["BtnDanger"] = new SolidColorBrush(Color.Parse("#CF222E"));
			app.Resources["BtnSecondary"] = new SolidColorBrush(Color.Parse("#E8E8E8"));
			app.Resources["BtnPurple"] = new SolidColorBrush(Color.Parse("#8B54FF"));
			app.Resources["BtnCyan"] = new SolidColorBrush(Color.Parse("#0891B2"));
		}
		else
		{
			app.Resources["ThemeBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeSurface"] = new SolidColorBrush(Color.Parse("#1E293B"));
			app.Resources["ThemeCard"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeTextPrimary"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["ThemeTextSecondary"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["ThemeTextTertiary"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["ThemeBorder"] = new SolidColorBrush(Color.Parse("#334155"));
			app.Resources["ThemeBorderStrong"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["ThemeInputBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["ThemeAccentHover"] = new SolidColorBrush(Color.Parse("#4F46E5"));
			app.Resources["CardServerBackground"] = new SolidColorBrush(Color.Parse("#1E1B4B"));
			app.Resources["CardModelsBackground"] = new SolidColorBrush(Color.Parse("#064E3B"));
			app.Resources["CardProfilesBackground"] = new SolidColorBrush(Color.Parse("#7C2D12"));
			app.Resources["CardVersionBackground"] = new SolidColorBrush(Color.Parse("#1E3A5F"));
			app.Resources["CardModelBackground"] = new SolidColorBrush(Color.Parse("#172554"));
			app.Resources["CardSectionBackground"] = new SolidColorBrush(Color.Parse("#1F1533"));
			app.Resources["CardServerAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsAccent"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesAccent"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionAccent"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelAccent"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardServerLabel"] = new SolidColorBrush(Color.Parse("#818CF8"));
			app.Resources["CardModelsLabel"] = new SolidColorBrush(Color.Parse("#34D399"));
			app.Resources["CardProfilesLabel"] = new SolidColorBrush(Color.Parse("#FDBA74"));
			app.Resources["CardVersionLabel"] = new SolidColorBrush(Color.Parse("#60A5FA"));
			app.Resources["CardModelLabel"] = new SolidColorBrush(Color.Parse("#60A5FA"));
			app.Resources["CardSubText"] = new SolidColorBrush(Color.Parse("#A5B4FC"));
			app.Resources["GpuMonitorBackground"] = new SolidColorBrush(Color.Parse("#1F1533"));
			app.Resources["GpuMonitorInner"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["GpuMonitorHeader"] = new SolidColorBrush(Color.Parse("#A78BFA"));
			app.Resources["ProgressTrack"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["ProgressVram"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["ProgressCore"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["ProgressPower"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["ProgressFan"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressRam"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["SidebarBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["SidebarItemBackground"] = new SolidColorBrush(Color.Parse("#1E293B"));
			app.Resources["SidebarItemSelected"] = new SolidColorBrush(Color.Parse("#27272A"));
			app.Resources["SidebarItemText"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["SidebarItemTextSelected"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemBackground"] = new SolidColorBrush(Color.Parse("#334155"));
			app.Resources["TabItemText"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["TabItemSelectedBackground"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["TabItemSelectedText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemHoverBackground"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["ProfileInnerBackground"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["ProfileModelBackground"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["BadgeMtp"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BadgeReasoning"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BadgeFlash"] = new SolidColorBrush(Color.Parse("#2563EB"));
			app.Resources["BadgeCache"] = new SolidColorBrush(Color.Parse("#D97706"));
			app.Resources["BadgeBatching"] = new SolidColorBrush(Color.Parse("#0891B2"));
			app.Resources["BadgeText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["ServerBannerBackground"] = new SolidColorBrush(Color.Parse("#1E1B4B"));
			app.Resources["ServerBannerLabel"] = new SolidColorBrush(Color.Parse("#818CF8"));
			app.Resources["ServerBannerSub"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["SectionTitle"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["SectionLabel"] = new SolidColorBrush(Color.Parse("#E2E8F0"));
			app.Resources["SectionLabelLight"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["BtnPrimary"] = new SolidColorBrush(Color.Parse("#4338CA"));
			app.Resources["BtnSuccess"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BtnDanger"] = new SolidColorBrush(Color.Parse("#DC2626"));
			app.Resources["BtnSecondary"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["BtnPurple"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BtnCyan"] = new SolidColorBrush(Color.Parse("#0891B2"));
		}
	}

	private static ThemeVariant DetectSystemThemeVariant()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			object themeValue = registryKey?.GetValue("AppsUseLightTheme");
			int result;
			if (themeValue != null)
			{
				return (int.TryParse(themeValue.ToString(), out result) && result == 1) ? ThemeVariant.Light : ThemeVariant.Dark;
			}
		}
		catch
		{
		}
		return ThemeVariant.Light;
	}

	internal static void ApplyAutoStart(bool enabled)
	{
		try
		{
			if (OperatingSystem.IsLinux())
			{
				ApplyLinuxAutoStart(enabled);
				return;
			}

			if (!OperatingSystem.IsWindows())
			{
				return;
			}

			string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
			if (string.IsNullOrEmpty(executablePath))
			{
				return;
			}
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey != null)
			{
				if (enabled)
				{
					registryKey.SetValue("LlamaStudio", "\"" + executablePath + "\"");
				}
				else
				{
					registryKey.DeleteValue("LlamaStudio", throwOnMissingValue: false);
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyLinuxAutoStart(bool enabled)
	{
		string autostartDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart");
		string desktopEntryPath = Path.Combine(autostartDirectory, "LlamaStudio.desktop");
		string legacyDesktopEntryPath = Path.Combine(autostartDirectory, "llama-studio-linux-dev.desktop");

		if (!enabled)
		{
			File.Delete(desktopEntryPath);
			File.Delete(legacyDesktopEntryPath);
			return;
		}

		Directory.CreateDirectory(autostartDirectory);
		string executable = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "dotnet";
		string arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(QuoteDesktopArgument));
		string command = string.IsNullOrWhiteSpace(arguments)
			? QuoteDesktopArgument(executable)
			: QuoteDesktopArgument(executable) + " " + arguments;
		string workingDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		File.WriteAllText(desktopEntryPath,
			"[Desktop Entry]\n" +
			"Version=1.0\n" +
			"Type=Application\n" +
			"Name=LlamaStudio\n" +
			"Comment=Llama.cpp manager\n" +
			"Exec=" + command + "\n" +
			"Path=" + workingDirectory + "\n" +
			"Terminal=false\n" +
			"Categories=Utility;Development;\n" +
			"StartupNotify=true\n");

		// This file belonged to the retired development build.  Keeping it would
		// launch a second, stale copy even when the setting is switched off.
		File.Delete(legacyDesktopEntryPath);
	}

	private static string QuoteDesktopArgument(string value)
	{
		return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}

	private void ResetAutoSave()
	{
		_autoSaveTimer?.Stop();
		_autoSaveTimer?.Start();
	}

	private async Task SaveSettingsInternalAsync()
	{
		if (!_isDisposing)
		{
			ApplySettingsToModel();
			await _settings.SaveAsync();
			_log.Information("Settings auto-saved", "Settings");
		}
	}

	public void ApplySettingsToModel()
	{
		_settings.LlamaCppBaseDirectory = LlamaCppBaseDirectory;
		_settings.LlamaCppDirectory = LlamaCppDirectory;
		_settings.ModelsDirectory = ModelsDirectory;
		_settings.Theme = "Dark";
		_settings.Language = SelectedLanguage;
		_settings.AutoCheckUpdates = AutoCheckUpdates;
		_settings.MinimizeToTray = MinimizeToTray;
		_settings.StartMinimized = StartMinimized;
		_settings.AutoStartWithOS = AutoStartWithOS;
		_settings.StartMonitoringWindow = StartMonitoringWindow;
		_settings.ShowDesktopWindow = ShowDesktopWindow;
		_settings.KeepServerOnExit = KeepServerOnExit;
		_settings.AutoStartServer = AutoStartServer;
		_settings.DefaultHost = DefaultHost;
		_settings.DefaultPort = DefaultPort;
		_settings.DefaultGpuLayers = DefaultGpuLayers;
		_settings.FlashAttention = FlashAttention;
		ISettings settings = _settings;
		string updateChannel = UpdateChannel;
		UpdateChannel parsedChannel = ((updateChannel == "PreRelease") ? LlamaStudio.Core.Enums.UpdateChannel.PreRelease : ((updateChannel == "Nightly") ? LlamaStudio.Core.Enums.UpdateChannel.Nightly : LlamaStudio.Core.Enums.UpdateChannel.Stable));
		settings.UpdateChannel = parsedChannel;
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

	/// <summary>
	/// r74: re-read fields that ApplySettingsToModel() pushes to the settings model,
	/// so a save performed elsewhere (Web UI / Models page / Server page) is never
	/// reverted by this VM's snapshot on the next auto-save or app exit.
	/// </summary>
	private void ReloadFromSettings()
	{
		if (_isDisposing)
		{
			return;
		}
		try
		{
			LlamaCppBaseDirectory = _settings.LlamaCppBaseDirectory;
			LlamaCppDirectory = _settings.LlamaCppDirectory;
			ModelsDirectory = _settings.ModelsDirectory;
			AutoCheckUpdates = _settings.AutoCheckUpdates;
			MinimizeToTray = _settings.MinimizeToTray;
			StartMinimized = _settings.StartMinimized;
			AutoStartWithOS = _settings.AutoStartWithOS;
			StartMonitoringWindow = _settings.StartMonitoringWindow;
			ShowDesktopWindow = _settings.ShowDesktopWindow;
			KeepServerOnExit = _settings.KeepServerOnExit;
			AutoStartServer = _settings.AutoStartServer;
			DefaultHost = _settings.DefaultHost;
			DefaultPort = _settings.DefaultPort;
			DefaultPortText = _settings.DefaultPort.ToString();
			DefaultGpuLayers = _settings.DefaultGpuLayers ?? "all";
			DefaultGpuLayersText = _settings.DefaultGpuLayers ?? "all";
			FlashAttention = _settings.FlashAttention;
			UpdateChannel = _settings.UpdateChannel switch
			{
				LlamaStudio.Core.Enums.UpdateChannel.PreRelease => "PreRelease",
				LlamaStudio.Core.Enums.UpdateChannel.Nightly => "Nightly",
				_ => "Stable",
			};
			UpdateChannelIndex = _settings.UpdateChannel switch
			{
				LlamaStudio.Core.Enums.UpdateChannel.PreRelease => 1,
				LlamaStudio.Core.Enums.UpdateChannel.Nightly => 2,
				_ => 0,
			};
			MonitorOpacity = _settings.MonitorOpacity;
			MonitorAlwaysOnTop = _settings.MonitorAlwaysOnTop;
			MonitorShowTps = _settings.MonitorShowTps;
			MonitorShowVram = _settings.MonitorShowVram;
			MonitorShowRam = _settings.MonitorShowRam;
			MonitorShowGpuTemp = _settings.MonitorShowGpuTemp;
			MonitorShowPowerDraw = _settings.MonitorShowPowerDraw;
			MonitorShowGpuCore = _settings.MonitorShowGpuCore;
			MonitorShowFanSpeed = _settings.MonitorShowFanSpeed;
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		_isDisposing = true;
		_settings.SettingsChanged -= ReloadFromSettings;
		_autoSaveTimer?.Stop();
		_autoSaveTimer?.Dispose();
		_autoSaveTimer = null;
		_appUpdater.StatusChanged -= delegate(object? s, string msg)
		{
			UpdateStatus = msg;
		};
		_appUpdater.UpdateAvailable -= delegate(object? s, AppUpdateInfo info)
		{
			HasUpdateAvailable = true;
			LatestVersion = info.Version;
			UpdateChangelog = info.Changelog;
		};
	}

	[RelayCommand]
	private async Task SaveSettings()
	{
		_autoSaveTimer?.Stop();
		ApplySettingsToModel();
		await _settings.SaveAsync();
		_log.Information("Settings saved manually", "Settings");
	}

	[RelayCommand]
	private async Task SelectLlamaCppBaseDirectory()
	{
		string path = await _dialog.SelectFolderAsync(_loc.T("settings.select_llama_base_dir"), LlamaCppBaseDirectory);
		if (!string.IsNullOrEmpty(path))
		{
			string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string rootPath = (Path.GetPathRoot(fullPath) ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase))
			{
				await _dialog.ShowErrorAsync(global::LlamaStudio.NativeLanguage.Text("Выберите папку с версиями llama.cpp, а не корень диска.", "Select the folder containing llama.cpp versions, not the root of a drive."));
				return;
			}
			LlamaCppBaseDirectory = path;
			_settings.LlamaCppBaseDirectory = path;
			await _settings.SaveAsync();
			_versionCache.Clear();
			await _versionCache.ScanAsync(path);
		}
	}

	[RelayCommand]
	private async Task SelectLlamaCppDirectory()
	{
		string path = await _dialog.SelectFolderAsync(_loc.T("settings.select_llama_dir"), LlamaCppDirectory);
		if (!string.IsNullOrEmpty(path))
		{
			LlamaCppDirectory = path;
			_settings.LlamaCppDirectory = path;
			await _settings.SaveAsync();
		}
	}

	[RelayCommand]
	private async Task SelectModelsDirectory()
	{
		string path = await _dialog.SelectFolderAsync(_loc.T("models.select_models_dir"), ModelsDirectory);
		if (!string.IsNullOrEmpty(path))
		{
			ModelsDirectory = path;
			_settings.ModelsDirectory = path;
			await _settings.SaveAsync();
		}
	}

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		IsCheckingUpdates = true;
		HasUpdateAvailable = false;
		LatestVersion = "";
		UpdateChangelog = "";
		UpdateStatus = _loc.T("settings.checking_updates");
		try
		{
			AppUpdateInfo appUpdateInfo = await _appUpdater.CheckForUpdatesAsync();
			if (appUpdateInfo == null)
			{
				UpdateStatus = string.Format(_loc.T("settings.no_update_available"), AppVersion);
				_log.Information("No updates available for LlamaStudio " + AppVersion, "Settings");
			}
			else
			{
				UpdateStatus = string.Format(_loc.T("settings.update_available"), appUpdateInfo.Version);
				_log.Information("Update available: " + appUpdateInfo.Version, "Settings");
			}
		}
		catch (Exception ex)
		{
			UpdateStatus = string.Format(_loc.T("settings.error_prefix"), ex.Message);
			_log.Error(string.Format(_loc.T("settings.error_prefix"), ex.Message), "Settings");
		}
		finally
		{
			IsCheckingUpdates = false;
		}
	}

	[RelayCommand]
	private async Task DownloadUpdate()
	{
		if (!HasUpdateAvailable)
		{
			return;
		}
		IsDownloading = true;
		DownloadProgress = 0.0;
		UpdateStatus = _loc.T("settings.downloading_update");
		try
		{
			_appUpdater.ProgressChanged += delegate(object? s, double progress)
			{
				DownloadProgress = progress;
			};
			string tempDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
			if (!Directory.Exists(tempDir))
			{
				Directory.CreateDirectory(tempDir);
			}
			AppUpdateInfo appUpdateInfo = await _appUpdater.CheckForUpdatesAsync();
			if (appUpdateInfo == null)
			{
				UpdateStatus = _loc.T("settings.no_update_available_now");
				HasUpdateAvailable = false;
				return;
			}
			string downloadedPath = await _appUpdater.DownloadUpdateAsync(appUpdateInfo, tempDir);
			UpdateStatus = _loc.T("settings.update_downloaded");
			if (await _dialog.ShowConfirmationAsync(string.Format(_loc.T("update.downloaded_restart"), LatestVersion), _loc.T("update.ready_title")))
			{
				await ApplyUpdateAndRestart(downloadedPath);
				return;
			}
			File.WriteAllText(Path.Combine(tempDir, "pending_update.txt"), downloadedPath);
			UpdateStatus = string.Format(_loc.T("settings.ready_restart_later"), LatestVersion);
			await _dialog.ShowMessageAsync(string.Format(_loc.T("update.ready_restart_later"), LatestVersion), _loc.T("update.ready_title"));
		}
		catch (Exception ex)
		{
			UpdateStatus = string.Format(_loc.T("settings.error_prefix"), ex.Message);
			_log.Error(string.Format(_loc.T("settings.error_prefix"), ex.Message), "Settings");
		}
		finally
		{
			IsDownloading = false;
		}
	}

	private async Task ApplyUpdateAndRestart(string downloadedPath)
	{
		// r87: on Linux the update artifact is a tar.gz and the update swaps
		// the installation folder (extract next door, swap, restart, rollback
		// on failure). The Windows single-file EXE flow below stays as is.
		if (OperatingSystem.IsLinux())
		{
			await ApplyLinuxUpdateAndRestart(downloadedPath);
			return;
		}
		string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
		if (!string.IsNullOrEmpty(executablePath))
		{
			string path = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
			string logPath = Path.Combine(path, "update_log.txt");
			string restartScriptPath = Path.Combine(path, "restart.bat");
			string escapedExe = EscapeBatString(executablePath);
			string escapedNewExe = EscapeBatString(downloadedPath);
			string escapedLog = EscapeBatString(logPath);
			string escapedBat = EscapeBatString(restartScriptPath);
			File.WriteAllText(restartScriptPath, $"@echo off\r\nchcp 65001 >nul\r\nsetlocal\r\necho [{{date}} {{time}}] Starting update process > \"{escapedLog}\"\r\necho Current EXE: {escapedExe} >> \"{escapedLog}\"\r\necho Downloaded EXE: {escapedNewExe} >> \"{escapedLog}\"\r\n\r\n:: Kill any remaining processes\r\ntaskkill /F /IM LlamaStudio.exe >> \"{escapedLog}\" 2>&1\r\ntimeout /t 3 /nobreak >nul\r\n\r\n:: Replace the executable\r\necho Deleting old EXE... >> \"{escapedLog}\"\r\ndel /F /Q \"{escapedExe}\" >> \"{escapedLog}\" 2>&1\r\ntimeout /t 1 /nobreak >nul\r\n\r\necho Copying new EXE... >> \"{escapedLog}\"\r\ncopy /Y \"{escapedNewExe}\" \"{escapedExe}\" >> \"{escapedLog}\" 2>&1\r\n\r\nif exist \"{escapedExe}\" (\r\n    echo Update successful! Starting new version... >> \"{escapedLog}\"\r\n    timeout /t 1 /nobreak >nul\r\n    start \"\" \"{escapedExe}\"\r\n    :: Clean up after successful start\r\n    ping 127.0.0.1 -n 2 >nul\r\n    del \"{escapedBat}\" >> \"{escapedLog}\" 2>&1\r\n) else (\r\n    echo ERROR: New file not found after copy! >> \"{escapedLog}\"\r\n    pause\r\n)\r\n", new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			_log.Information("Update script created: " + restartScriptPath, "Settings");
			_log.Information("Current EXE: " + executablePath, "Settings");
			_log.Information("Downloaded EXE: " + downloadedPath, "Settings");
			Process.Start(new ProcessStartInfo
			{
				FileName = restartScriptPath,
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			_log.Information("App update downloaded, restarting...", "Settings");
			Environment.Exit(0);
		}
	}

	private async Task ApplyLinuxUpdateAndRestart(string tarballPath)
	{
		string? processPath = Environment.ProcessPath;
		string appDir = string.IsNullOrEmpty(processPath) ? string.Empty : Path.GetDirectoryName(processPath) ?? string.Empty;
		string parent = string.IsNullOrEmpty(appDir) ? string.Empty : Path.GetDirectoryName(appDir) ?? string.Empty;
		// Safety: the swap must move a dedicated app folder, never a root-level
		// or user directory (the binary installed straight into ~ or /home/user).
		int segments = appDir.Length == 0 ? 0 : Path.GetFullPath(appDir).Split(Path.DirectorySeparatorChar).Count(segment => segment.Length > 0);
		if (string.IsNullOrEmpty(appDir) || string.IsNullOrEmpty(parent) || segments < 3)
		{
			_log.Error("Linux update: the application folder cannot be swapped (too high in the tree): " + appDir, "Settings");
			await _dialog.ShowMessageAsync(_loc.T("update.linux_cannot_update"), _loc.T("update.ready_title"));
			return;
		}
		try
		{
			string probe = Path.Combine(parent, ".llamastudio_write_test_" + Environment.ProcessId);
			File.WriteAllText(probe, "ok");
			File.Delete(probe);
		}
		catch (Exception ex)
		{
			_log.Error("Linux update: no write permission for " + parent + ": " + ex.Message, "Settings");
			await _dialog.ShowMessageAsync(_loc.T("update.linux_cannot_update"), _loc.T("update.ready_title"));
			return;
		}
		string stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
		string tempDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
		string logPath = Path.Combine(tempDir, "update_log.txt");
		string scriptPath = Path.Combine(tempDir, "restart-linux.sh");
		string extractDir = Path.Combine(parent, ".llamastudio-new-" + stamp);
		string oldDir = Path.Combine(parent, Path.GetFileName(appDir) + ".old-" + stamp);
		// The release tarball contains a top-level "LlamaStudio" folder.
		string newDir = Path.Combine(extractDir, "LlamaStudio");
		string newBinary = Path.Combine(newDir, "LlamaStudio");
		string script = "#!/bin/bash\n"
			+ "LOG=" + ShellQuote(logPath) + "\n"
			+ "mkdir -p \"$(dirname \"$LOG\")\"\n"
			+ "echo \"[$(date -Is)] Linux update start\" > \"$LOG\"\n"
			+ "mkdir -p " + ShellQuote(extractDir) + "\n"
			+ "tar -xzf " + ShellQuote(tarballPath) + " -C " + ShellQuote(extractDir) + " >> \"$LOG\" 2>&1\n"
			+ "if [ ! -x " + ShellQuote(newBinary) + " ]; then\n"
			+ "  echo \"[$(date -Is)] ERROR: new binary not found in tarball\" >> \"$LOG\"\n"
			+ "  rm -rf " + ShellQuote(extractDir) + "\n"
			+ "  exit 1\n"
			+ "fi\n"
			+ "mv " + ShellQuote(appDir) + " " + ShellQuote(oldDir) + "\n"
			+ "mv " + ShellQuote(newDir) + " " + ShellQuote(appDir) + "\n"
			+ "cd " + ShellQuote(appDir) + "\n"
			+ "setsid nohup " + ShellQuote(Path.Combine(appDir, "LlamaStudio")) + " >> \"$HOME/.llamastudio-update-restart.log\" 2>&1 &\n"
			+ "sleep 3\n"
			+ "if pgrep -f " + ShellQuote(Path.Combine(appDir, "LlamaStudio")) + " > /dev/null; then\n"
			+ "  echo \"[$(date -Is)] Update OK, new version started\" >> \"$LOG\"\n"
			+ "  rm -rf " + ShellQuote(oldDir) + " " + ShellQuote(extractDir) + "\n"
			+ "else\n"
			+ "  echo \"[$(date -Is)] ERROR: new version did not start, rolling back\" >> \"$LOG\"\n"
			+ "  rm -rf " + ShellQuote(appDir) + "\n"
			+ "  mv " + ShellQuote(oldDir) + " " + ShellQuote(appDir) + "\n"
			+ "fi\n";
		File.WriteAllText(scriptPath, script);
		_log.Information("Linux update script created: " + scriptPath, "Settings");
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "bash",
				Arguments = "\"" + scriptPath + "\"",
				UseShellExecute = false
			});
		}
		catch (Exception ex)
		{
			_log.Error("Linux update: failed to start the update script: " + ex.Message, "Settings");
			await _dialog.ShowMessageAsync(_loc.T("update.linux_cannot_update"), _loc.T("update.ready_title"));
			return;
		}
		_log.Information("Linux update: restarting...", "Settings");
		Environment.Exit(0);
	}

	private static string ShellQuote(string s) => "'" + s.Replace("'", "'\\''") + "'";

	private static string EscapeBatString(string s)
	{
		return s.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
			.Replace("<", "^<")
			.Replace(">", "^>")
			.Replace("(", "^(")
			.Replace(")", "^)")
			.Replace("!", "^!")
			.Replace("%", "%%");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLlamaCppBaseDirectoryChanged(string value)
	{
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLlamaCppDirectoryChanged(string value)
	{
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnModelsDirectoryChanged(string value)
	{
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnThemeChanged(string value)
	{
		const string darkTheme = "Dark";
		_settings.Theme = darkTheme;
		ResetAutoSave();
		ApplyTheme(darkTheme);
		if (Theme != darkTheme)
		{
			Theme = darkTheme;
		}
		if (ThemeIndex != 0)
		{
			ThemeIndex = 0;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAutoCheckUpdatesChanged(bool value)
	{
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMinimizeToTrayChanged(bool value)
	{
		_settings.MinimizeToTray = value;
		_settings.SaveAsync();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnStartMinimizedChanged(bool value)
	{
		_settings.StartMinimized = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAutoStartWithOSChanged(bool value)
	{
		_settings.AutoStartWithOS = value;
		ApplyAutoStart(value);
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnStartMonitoringWindowChanged(bool value)
	{
		_settings.StartMonitoringWindow = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnShowDesktopWindowChanged(bool value)
	{
		_settings.ShowDesktopWindow = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnKeepServerOnExitChanged(bool value)
	{
		_settings.KeepServerOnExit = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAutoStartServerChanged(bool value)
	{
		_settings.AutoStartServer = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDefaultHostChanged(string value)
	{
		_settings.DefaultHost = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDefaultPortChanged(int value)
	{
		_settings.DefaultPort = value;
		ResetAutoSave();
		if (DefaultPortText != value.ToString())
		{
			DefaultPortText = value.ToString();
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDefaultGpuLayersChanged(string value)
	{
		_settings.DefaultGpuLayers = value;
		ResetAutoSave();
		if (DefaultGpuLayersText != value)
		{
			DefaultGpuLayersText = value;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnFlashAttentionChanged(bool value)
	{
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnThemeIndexChanged(int value)
	{
		if (value != 0)
		{
			ThemeIndex = 0;
			return;
		}
		if (Theme != "Dark")
		{
			Theme = "Dark";
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnUpdateChannelChanged(string value)
	{
		ISettings settings = _settings;
		UpdateChannel updateChannel = ((value == "PreRelease") ? LlamaStudio.Core.Enums.UpdateChannel.PreRelease : ((value == "Nightly") ? LlamaStudio.Core.Enums.UpdateChannel.Nightly : LlamaStudio.Core.Enums.UpdateChannel.Stable));
		settings.UpdateChannel = updateChannel;
		ResetAutoSave();
		int updateChannelIndex = ((value == "PreRelease") ? 1 : ((value == "Nightly") ? 2 : 0));
		UpdateChannelIndex = updateChannelIndex;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnUpdateChannelIndexChanged(int value)
	{
		UpdateChannel = value switch
		{
			1 => "PreRelease", 
			2 => "Nightly", 
			_ => "Stable", 
		};
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedLanguageChanged(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			_loc.ChangeLanguage(value);
			ResetAutoSave();
			_log.Information("Language changed to: " + value, "Settings");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnUpdateStatusChanged(string value)
	{
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHasUpdateAvailableChanged(bool value)
	{
		OnPropertyChanged("AppUpdateStatus");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLatestVersionChanged(string value)
	{
		OnPropertyChanged("AppUpdateStatus");
		OnPropertyChanged("DownloadUpdateBtn");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsDownloadingChanged(bool value)
	{
		OnPropertyChanged("DownloadUpdateBtn");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDownloadProgressChanged(double value)
	{
		OnPropertyChanged("DownloadUpdateBtn");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorOpacityChanged(double value)
	{
		OnPropertyChanged("MonitorOpacityPercent");
		_settings.MonitorOpacity = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorAlwaysOnTopChanged(bool value)
	{
		_settings.MonitorAlwaysOnTop = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowTpsChanged(bool value)
	{
		_settings.MonitorShowTps = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowVramChanged(bool value)
	{
		_settings.MonitorShowVram = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowRamChanged(bool value)
	{
		_settings.MonitorShowRam = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowGpuTempChanged(bool value)
	{
		_settings.MonitorShowGpuTemp = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowPowerDrawChanged(bool value)
	{
		_settings.MonitorShowPowerDraw = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowGpuCoreChanged(bool value)
	{
		_settings.MonitorShowGpuCore = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMonitorShowFanSpeedChanged(bool value)
	{
		_settings.MonitorShowFanSpeed = value;
		ResetAutoSave();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDefaultPortTextChanged(string value)
	{
		if (int.TryParse(value, out var result) && result >= 1 && result <= 65535)
		{
			DefaultPort = result;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDefaultGpuLayersTextChanged(string value)
	{
		DefaultGpuLayers = value;
	}
}
