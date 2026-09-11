using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Views;

namespace LlamaStudio.ViewModels;

public class DashboardViewModel : ObservableObject, IDisposable
{
	private IReadOnlyList<GpuInfo> _gpuInfos = Array.Empty<GpuInfo>();

	/// <summary>
	/// GPU list for the dashboard. MUST notify on change: DashboardGpuMonitoringControl
	/// rebuilds its live cards on the "GpuInfos" PropertyChanged event (r78 fix —
	/// the plain auto-property kept the main-page GPU graphs frozen until the page
	/// was recreated on tab switch).
	/// </summary>
	public IReadOnlyList<GpuInfo> GpuInfos
	{
		get => _gpuInfos;
		private set
		{
			if (ReferenceEquals(_gpuInfos, value))
			{
				return;
			}
			_gpuInfos = value;
			OnPropertyChanged(nameof(GpuInfos));
		}
	}

	public bool ShowSingleGpuMonitor => GpuInfos.Count <= 1;
	private readonly IServerManager _serverManager;

	private readonly IModelScanner _modelScanner;

	private readonly IProfileManager _profileManager;

	private readonly ISettings _settings;

	private readonly INavigationService _navigation;

	private readonly ILocalizationService _loc;

	private readonly ILlamaUpdater _updater;

	private readonly IAppUpdater _appUpdater;

	private readonly IGpuMonitor _gpuMonitor;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly IVersionCache _versionCache;

	private readonly DispatcherTimer _gpuTimer;
	private bool _gpuRefreshInProgress;

	private readonly DispatcherTimer _updateCheckTimer;

	private readonly MonitoringViewModel? _monitoringVm;

	private bool _isInitializing;

	private bool _isSettingDefault;

	[ObservableProperty]
	private ServerStatus _serverStatus = new ServerStatus();

	private double _lastTps;

	private double _lastPromptTps;

	[ObservableProperty]
	private int _totalModels;

	[ObservableProperty]
	private int _totalProfiles;

	[ObservableProperty]
	private string _activeVersion = "Not installed";

	[ObservableProperty]
	private double _estimatedVramGb;

	[ObservableProperty]
	private double _vramPercentage;

	[ObservableProperty]
	private string _selectedModel = "No model selected";

	[ObservableProperty]
	private GpuInfo? _gpuInfo;

	[ObservableProperty]
	private bool _gpuAvailable;

	private static readonly double s_totalRamGb = (double)GetTotalPhysicalMemory() / 1073741824.0;

	[ObservableProperty]
	private bool _hasUpdate;

	[ObservableProperty]
	private bool _latestVersionInstalled;

	[ObservableProperty]
	private string _latestVersion = string.Empty;

	[ObservableProperty]
	private bool _isCheckingVersion;

	[ObservableProperty]
	private bool _isUpdating;

	[ObservableProperty]
	private double _updateProgress;

	[ObservableProperty]
	private bool _showVersionList;

	[ObservableProperty]
	private ObservableCollection<string> _installedVersions = new ObservableCollection<string>();

	[ObservableProperty]
	private string _appVersion = "0.0.0";

	[ObservableProperty]
	private bool _hasAppUpdate;

	[ObservableProperty]
	private bool _isUpdatingApp;

	[ObservableProperty]
	private double _appUpdateProgress;

	[ObservableProperty]
	private string _appLatestVersion = "";

	[ObservableProperty]
	private string? _selectedVersion;

	[ObservableProperty]
	private ObservableCollection<ServerProfile> _allProfiles = new ObservableCollection<ServerProfile>();

	[ObservableProperty]
	private ServerProfile? _selectedProfile;

	[ObservableProperty]
	private ServerProfile? _currentProfile;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleVersionListCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? switchVersionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? setDefaultProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<string>? navigateCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? startServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? stopServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? checkVersionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? openMonitoringCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? updateLlamaCppCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? updateAppCommand;

	public string GpuName => GpuInfo?.Name ?? "N/A";

	public string GpuMemoryInfo
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.MemoryTotalGb:F1} GB · Driver {GpuInfo.DriverVersion}";
		}
	}

	public string GpuTempText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.TemperatureCelsius:F0}°C";
		}
	}

	public double GpuTempPercent
	{
		get
		{
			if (GpuInfo == null)
			{
				return 0.0;
			}
			return Math.Min(GpuInfo.TemperatureCelsius / 90.0 * 100.0, 100.0);
		}
	}

	public string GpuMemTempText
	{
		get
		{
			if (GpuInfo == null || !(GpuInfo.MemoryTemperatureCelsius > 0.0))
			{
				return "N/A";
			}
			return $"{GpuInfo.MemoryTemperatureCelsius:F0}°C";
		}
	}

	public double GpuMemTempPercent
	{
		get
		{
			if (GpuInfo == null || !(GpuInfo.MemoryTemperatureCelsius > 0.0))
			{
				return 0.0;
			}
			return Math.Min(GpuInfo.MemoryTemperatureCelsius / 90.0 * 100.0, 100.0);
		}
	}

	public bool HasGpuMemTemp
	{
		get
		{
			GpuInfo? gpuInfo = GpuInfo;
			if (gpuInfo == null)
			{
				return false;
			}
			return gpuInfo.MemoryTemperatureCelsius > 0.0;
		}
	}

	public SolidColorBrush GpuTempBrush
	{
		get
		{
			double tempCelsius = GpuInfo?.TemperatureCelsius ?? 0.0;
			byte r;
			byte g;
			byte b;
			if (tempCelsius <= 55.0)
			{
				r = 16;
				g = 185;
				b = 129;
			}
			else if (tempCelsius <= 70.0)
			{
				double midBlend = (tempCelsius - 55.0) / 15.0;
				r = (byte)(16.0 + 235.0 * midBlend);
				g = (byte)(185.0 + 6.0 * midBlend);
				b = (byte)(129.0 + -93.0 * midBlend);
			}
			else
			{
				double highBlend = Math.Min((tempCelsius - 70.0) / 20.0, 1.0);
				r = (byte)(251.0 + -12.0 * highBlend);
				g = (byte)(191.0 + -123.0 * highBlend);
				b = (byte)(36.0 + 32.0 * highBlend);
			}
			return new SolidColorBrush(Color.FromArgb(byte.MaxValue, r, g, b));
		}
	}

	public SolidColorBrush GpuMemTempBrush
	{
		get
		{
			double memTempCelsius = GpuInfo?.MemoryTemperatureCelsius ?? 0.0;
			if (memTempCelsius <= 0.0)
			{
				return new SolidColorBrush(Color.FromArgb(0, 148, 163, 184));
			}
			byte r;
			byte g;
			byte b;
			if (memTempCelsius <= 55.0)
			{
				r = 16;
				g = 185;
				b = 129;
			}
			else if (memTempCelsius <= 70.0)
			{
				double midBlend = (memTempCelsius - 55.0) / 15.0;
				r = (byte)(16.0 + 235.0 * midBlend);
				g = (byte)(185.0 + 6.0 * midBlend);
				b = (byte)(129.0 + -93.0 * midBlend);
			}
			else
			{
				double highBlend = Math.Min((memTempCelsius - 70.0) / 20.0, 1.0);
				r = (byte)(251.0 + -12.0 * highBlend);
				g = (byte)(191.0 + -123.0 * highBlend);
				b = (byte)(36.0 + 32.0 * highBlend);
			}
			return new SolidColorBrush(Color.FromArgb(byte.MaxValue, r, g, b));
		}
	}

	public string GpuVramGbText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.MemoryUsedGb:F3} / {GpuInfo.MemoryTotalGb:F3} GB";
		}
	}

	public string GpuVramText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.MemoryPercent:F0}%";
		}
	}

	public string GpuCoreText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.GpuUtilization:F0}%";
		}
	}

	public string GpuPowerWattsText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.PowerDrawWatts:F0} W / {GpuInfo.PowerLimitWatts:F0} W";
		}
	}

	public string GpuPowerText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.PowerPercent:F0}%";
		}
	}

	public string GpuFanText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.FanSpeed}%";
		}
	}

	public string GpuClockText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "N/A";
			}
			return $"{GpuInfo.ClockMhz} MHz";
		}
	}

	public string GpuThroughputText
	{
		get
		{
			if (!(_lastTps > 0.0))
			{
				return "—";
			}
			return $"{_lastTps:F1}";
		}
	}

	public string GpuPromptThroughputText => $"{_lastPromptTps:F1}";

	public string RamUsedText
	{
		get
		{
			string ramText;
			if (ServerStatus.RamUsedGb > 0.0)
			{
				ramText = $"{ServerStatus.RamUsedGb:F1} / {s_totalRamGb:F1} GB";
				if (ServerStatus.RamCommittedGb > 0.0)
				{
					ramText += $" · выделено: {ServerStatus.RamCommittedGb:F1} GB";
				}
			}
			else
			{
				ramText = $"Сервер не запущен / {s_totalRamGb:F1} GB";
			}
			double cacheRamLimitGb = ServerStatus.CacheRamLimitGb;
			if (!(cacheRamLimitGb > 0.0) && SelectedProfile?.CacheRam > 0)
			{
				cacheRamLimitGb = SelectedProfile.CacheRam / 1024.0;
			}
			if (cacheRamLimitGb > 0.0)
			{
				ramText += $" · кэш: до {cacheRamLimitGb:F1} GB";
			}
			return ramText;
		}
	}

	public string RamPercentText
	{
		get
		{
			if (!(s_totalRamGb > 0.0) || !(ServerStatus.RamUsedGb > 0.0))
			{
				return "—";
			}
			return $"{ServerStatus.RamUsedGb / s_totalRamGb * 100.0:F0}%";
		}
	}

	public double RamUsedPercent
	{
		get
		{
			if (!(s_totalRamGb > 0.0) || !(ServerStatus.RamUsedGb > 0.0))
			{
				return 0.0;
			}
			return Math.Min(ServerStatus.RamUsedGb / s_totalRamGb * 100.0, 100.0);
		}
	}

	public string RamUsageLabel => _loc.T("dash.ram_usage");

	public double GpuVramUsage => GpuInfo?.MemoryPercent ?? 0.0;

	public double GpuCoreUsage => GpuInfo?.GpuUtilization ?? 0.0;

	public double GpuPowerUsage => GpuInfo?.PowerPercent ?? 0.0;

	public double GpuFanSpeed => GpuInfo?.FanSpeed ?? 0;

	public string ActiveModelName => SelectedModel;

	public string UpdateButtonLabel
	{
		get
		{
			if (!IsCheckingVersion)
			{
				if (!HasUpdate)
				{
					return CheckUpdatesBtn;
				}
				return string.Format(_loc.T("dash.update_version"), LatestVersion);
			}
			return CheckingVersion;
		}
	}

	public string UpdateBtnText
	{
		get
		{
			if (!IsUpdating)
			{
				if (!LatestVersionInstalled)
				{
					return _loc.T("dash.update") + " (" + LatestVersion + ")";
				}
				return _loc.T("llama_releases.use") + " (" + LatestVersion + ")";
			}
			return $"{_loc.T("dash.updating")} {UpdateProgress:F0}%";
		}
	}

	public string VersionStatusText
	{
		get
		{
			if (!IsCheckingVersion)
			{
				if (!HasUpdate)
				{
					return "✓ " + UpToDate;
				}
				return "↑ " + UpdateAvailable + ", " + LatestVersion;
			}
			return "⏳ " + CheckingVersion;
		}
	}

	public string ActiveVersionDisplay
	{
		get
		{
			if (string.IsNullOrWhiteSpace(ActiveVersion))
			{
				return string.Empty;
			}
			string[] versionParts = ActiveVersion.Split('|');
			if (versionParts.Length == 2 && !string.IsNullOrEmpty(versionParts[1]))
			{
				return versionParts[0] + " (" + versionParts[1] + ")";
			}
			return versionParts[0];
		}
	}

	public string SwitchVersionBtn => _loc.T("llama_releases.switch_version");

	public string Title => _loc.T("dash.title");

	public string ServerLabel => _loc.T("dash.server_label");

	public string ModelsCard => _loc.T("main.models");

	public string Indexed => _loc.T("dash.indexed");

	public string ProfilesCard => _loc.T("main.profiles");

	public string Saved => _loc.T("dash.saved");

	public string ProfilesSavedText => string.Format(_loc.T("dash.saved_count_fmt"), TotalProfiles);

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

	public bool ServerStopped => ServerStatus.State == ServerState.Stopped;

	public bool ServerRunning
	{
		get
		{
			if (ServerStatus.State != ServerState.Running)
			{
				return ServerStatus.State == ServerState.Starting;
			}
			return true;
		}
	}

	public string StartServerBtn => _loc.T("server.start_btn");

	public string StopServerBtn => _loc.T("server.stop_btn");

	public string MonitoringBtn => _loc.T("main.monitoring");

	public string AppVersionLabel => _loc.T("dash.app_version");

	public string AppUpdateBtnText
	{
		get
		{
			if (!IsUpdatingApp)
			{
				return _loc.T("dash.update") + " (v" + AppLatestVersion + ")";
			}
			return $"{_loc.T("dash.downloading")} {AppUpdateProgress:F0}%";
		}
	}

	public string AppUpdateStatus
	{
		get
		{
			if (!HasAppUpdate)
			{
				return "✓ " + _loc.T("dash.up_to_date");
			}
			return "↑ " + _loc.T("dash.new_version") + " " + AppLatestVersion;
		}
	}

	public string ProfileName => CurrentProfile?.Name ?? _loc.T("dash.no_profile");

	public bool IsDefaultProfile => CurrentProfile?.IsDefault ?? false;

	public bool CanSetDefault
	{
		get
		{
			if (SelectedProfile != null)
			{
				return !SelectedProfile.IsDefault;
			}
			return false;
		}
	}

	public string ProfileMainModel
	{
		get
		{
			if (CurrentProfile == null || string.IsNullOrEmpty(CurrentProfile.ModelPath))
			{
				return "—";
			}
			return Path.GetFileName(CurrentProfile.ModelPath);
		}
	}

	public bool HasMmproj
	{
		get
		{
			if (CurrentProfile != null)
			{
				return !string.IsNullOrEmpty(CurrentProfile.MmprojPath);
			}
			return false;
		}
	}

	public string MmprojName
	{
		get
		{
			if (!HasMmproj)
			{
				return "—";
			}
			return Path.GetFileName(CurrentProfile.MmprojPath);
		}
	}

	public bool HasDraft
	{
		get
		{
			if (CurrentProfile != null)
			{
				return !string.IsNullOrEmpty(CurrentProfile.DraftModelPath);
			}
			return false;
		}
	}

	public string DraftName
	{
		get
		{
			if (!HasDraft)
			{
				return "—";
			}
			return Path.GetFileName(CurrentProfile.DraftModelPath);
		}
	}

	public string ProfileContextText
	{
		get
		{
			if (CurrentProfile == null || CurrentProfile.ContextSize <= 0)
			{
				return "—";
			}
			return $"{CurrentProfile.ContextSize:N0}";
		}
	}

	public string ProfileTempText => CurrentProfile?.Temperature.ToString("F2") ?? "—";

	public bool ProfileHasMtp
	{
		get
		{
			if (CurrentProfile != null)
			{
				return !string.IsNullOrEmpty(CurrentProfile.SpecType);
			}
			return false;
		}
	}

	public bool ProfileHasReasoning => CurrentProfile?.Reasoning ?? false;

	public string ProfileGpuLayersText => CurrentProfile?.GpuLayers ?? "all";

	public bool ProfileFlashAttention => CurrentProfile?.FlashAttention ?? false;

	public bool ProfileCachePrompt => CurrentProfile?.CachePrompt ?? false;

	public bool ProfileContBatching => CurrentProfile?.ContBatching ?? false;

	public int ProfileThreads
	{
		get
		{
			ServerProfile? currentProfile = CurrentProfile;
			if (currentProfile == null || currentProfile.Threads <= 0)
			{
				return 0;
			}
			return CurrentProfile.Threads;
		}
	}

	public string ProfileThreadsText
	{
		get
		{
			if (ProfileThreads <= 0)
			{
				return "—";
			}
			return ProfileThreads.ToString();
		}
	}

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

	public string SetDefaultProfileBtn => _loc.T("dash.set_default_profile_btn");

	public string ActiveProfileLabel => _loc.T("dash.active_profile_label");

	public string ActiveProfileName => CurrentProfile?.Name ?? "—";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerStatus ServerStatus
	{
		get
		{
			return _serverStatus;
		}
		[MemberNotNull("_serverStatus")]
		set
		{
			if (!EqualityComparer<LlamaStudio.Core.Models.ServerStatus>.Default.Equals(_serverStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ServerStatus);
				_serverStatus = value;
				OnServerStatusChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ServerStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TotalModels
	{
		get
		{
			return _totalModels;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_totalModels, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TotalModels);
				_totalModels = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TotalModels);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TotalProfiles
	{
		get
		{
			return _totalProfiles;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_totalProfiles, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TotalProfiles);
				_totalProfiles = value;
				OnTotalProfilesChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TotalProfiles);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ActiveVersion
	{
		get
		{
			return _activeVersion;
		}
		[MemberNotNull("_activeVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_activeVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ActiveVersion);
				_activeVersion = value;
				OnActiveVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ActiveVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double EstimatedVramGb
	{
		get
		{
			return _estimatedVramGb;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_estimatedVramGb, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.EstimatedVramGb);
				_estimatedVramGb = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.EstimatedVramGb);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double VramPercentage
	{
		get
		{
			return _vramPercentage;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_vramPercentage, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.VramPercentage);
				_vramPercentage = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.VramPercentage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedModel
	{
		get
		{
			return _selectedModel;
		}
		[MemberNotNull("_selectedModel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedModel, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedModel);
				_selectedModel = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedModel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public GpuInfo? GpuInfo
	{
		get
		{
			return _gpuInfo;
		}
		set
		{
			if (!EqualityComparer<LlamaStudio.Core.Models.GpuInfo>.Default.Equals(_gpuInfo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GpuInfo);
				_gpuInfo = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GpuInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool GpuAvailable
	{
		get
		{
			return _gpuAvailable;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_gpuAvailable, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GpuAvailable);
				_gpuAvailable = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GpuAvailable);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasUpdate
	{
		get
		{
			return _hasUpdate;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasUpdate, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HasUpdate);
				_hasUpdate = value;
				OnHasUpdateChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HasUpdate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool LatestVersionInstalled
	{
		get
		{
			return _latestVersionInstalled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_latestVersionInstalled, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LatestVersionInstalled);
				_latestVersionInstalled = value;
				OnLatestVersionInstalledChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LatestVersionInstalled);
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
	public bool IsCheckingVersion
	{
		get
		{
			return _isCheckingVersion;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCheckingVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCheckingVersion);
				_isCheckingVersion = value;
				OnIsCheckingVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCheckingVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsUpdating
	{
		get
		{
			return _isUpdating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isUpdating, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsUpdating);
				_isUpdating = value;
				OnIsUpdatingChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsUpdating);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double UpdateProgress
	{
		get
		{
			return _updateProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_updateProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateProgress);
				_updateProgress = value;
				OnUpdateProgressChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowVersionList
	{
		get
		{
			return _showVersionList;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showVersionList, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowVersionList);
				_showVersionList = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowVersionList);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<string> InstalledVersions
	{
		get
		{
			return _installedVersions;
		}
		[MemberNotNull("_installedVersions")]
		set
		{
			if (!EqualityComparer<ObservableCollection<string>>.Default.Equals(_installedVersions, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.InstalledVersions);
				_installedVersions = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.InstalledVersions);
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
	public bool HasAppUpdate
	{
		get
		{
			return _hasAppUpdate;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasAppUpdate, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HasAppUpdate);
				_hasAppUpdate = value;
				OnHasAppUpdateChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HasAppUpdate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsUpdatingApp
	{
		get
		{
			return _isUpdatingApp;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isUpdatingApp, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsUpdatingApp);
				_isUpdatingApp = value;
				OnIsUpdatingAppChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsUpdatingApp);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double AppUpdateProgress
	{
		get
		{
			return _appUpdateProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_appUpdateProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AppUpdateProgress);
				_appUpdateProgress = value;
				OnAppUpdateProgressChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AppUpdateProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string AppLatestVersion
	{
		get
		{
			return _appLatestVersion;
		}
		[MemberNotNull("_appLatestVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_appLatestVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AppLatestVersion);
				_appLatestVersion = value;
				OnAppLatestVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AppLatestVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedVersion
	{
		get
		{
			return _selectedVersion;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedVersion);
				_selectedVersion = value;
				OnSelectedVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ServerProfile> AllProfiles
	{
		get
		{
			return _allProfiles;
		}
		[MemberNotNull("_allProfiles")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ServerProfile>>.Default.Equals(_allProfiles, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AllProfiles);
				_allProfiles = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AllProfiles);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerProfile? SelectedProfile
	{
		get
		{
			return _selectedProfile;
		}
		set
		{
			if (!EqualityComparer<ServerProfile>.Default.Equals(_selectedProfile, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedProfile);
				_selectedProfile = value;
				OnSelectedProfileChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedProfile);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerProfile? CurrentProfile
	{
		get
		{
			return _currentProfile;
		}
		set
		{
			if (!EqualityComparer<ServerProfile>.Default.Equals(_currentProfile, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CurrentProfile);
				_currentProfile = value;
				OnCurrentProfileChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CurrentProfile);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleVersionListCommand => toggleVersionListCommand ?? (toggleVersionListCommand = new RelayCommand(ToggleVersionList));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> SwitchVersionCommand => switchVersionCommand ?? (switchVersionCommand = new AsyncRelayCommand<string>(SwitchVersionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SetDefaultProfileCommand => setDefaultProfileCommand ?? (setDefaultProfileCommand = new AsyncRelayCommand(SetDefaultProfileAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> NavigateCommand => navigateCommand ?? (navigateCommand = new RelayCommand<string>(Navigate));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncRelayCommand(StartServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncRelayCommand(StopServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckVersionCommand => checkVersionCommand ?? (checkVersionCommand = new AsyncRelayCommand(CheckVersionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenMonitoringCommand => openMonitoringCommand ?? (openMonitoringCommand = new RelayCommand(OpenMonitoring));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UpdateLlamaCppCommand => updateLlamaCppCommand ?? (updateLlamaCppCommand = new AsyncRelayCommand(UpdateLlamaCppAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UpdateAppCommand => updateAppCommand ?? (updateAppCommand = new AsyncRelayCommand(UpdateAppAsync));

	private static long GetTotalPhysicalMemory()
	{
		try
		{
			nint memoryStatus = Marshal.AllocHGlobal(64);
			try
			{
				Marshal.WriteInt32(memoryStatus, 64);
				if (GlobalMemoryStatusEx(memoryStatus))
				{
					return Marshal.ReadInt64(memoryStatus, 8);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(memoryStatus);
			}
		}
		catch
		{
		}
		return 68719476736L;
	}

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(nint lpBuffer);

	public string GetUpdateButtonLabel()
	{
		if (!IsCheckingVersion)
		{
			if (!HasUpdate)
			{
				return CheckUpdatesBtn;
			}
			return _loc.T("dash.update") + " (" + LatestVersion + ")";
		}
		return CheckingVersion;
	}

	[RelayCommand]
	private void ToggleVersionList()
	{
		ShowVersionList = !ShowVersionList;
	}

	[RelayCommand]
	private async Task SwitchVersionAsync(string versionDisplay)
	{
		if (string.IsNullOrWhiteSpace(versionDisplay))
		{
			return;
		}
		VersionInfo vi = _versionCache.GetAll().FirstOrDefault((VersionInfo v) => v.Display == versionDisplay);
		if (vi == null)
		{
			string version = versionDisplay.Split('(')[0].Trim();
			vi = _versionCache.GetByVersion(version);
		}
		if (vi == null || !Directory.Exists(vi.Directory) || !File.Exists(Path.Combine(vi.Directory, "llama-server.exe")))
		{
			_log.Warning("Version " + versionDisplay + " not found or incomplete", "Dashboard");
			return;
		}
		string versionKey = vi.VersionKey;
		bool wasRunning = ServerStatus.State == ServerState.Running || ServerStatus.State == ServerState.Starting;
		IDisposable loadingWin = null;
		if (wasRunning)
		{
			loadingWin = _dialog.ShowLoading(string.Format(_loc.T("dash.switching_version"), versionDisplay), _loc.T("dash.restart_server"));
		}
		try
		{
			if (wasRunning)
			{
				_log.Information("Stopping server to switch version to " + versionDisplay, "Dashboard");
				await _serverManager.StopAsync();
				for (int i = 0; i < 30; i++)
				{
					await Task.Delay(500);
					if ((await _serverManager.GetStatusAsync()).State == ServerState.Stopped)
					{
						break;
					}
				}
			}
			ActiveVersion = versionKey;
			SelectedVersion = versionDisplay;
			_settings.LlamaCppDirectory = vi.Directory;
			_settings.ActiveLlamaCppVersion = versionKey;
			await _settings.SaveAsync();
			ServerProfile activeProfile = await GetActiveProfileAsync();
			if (activeProfile != null)
			{
				activeProfile.SelectedServerVersion = versionKey;
				await _profileManager.SaveProfileAsync(activeProfile);
			}
			_log.Information("Switched to version " + versionDisplay + " -> " + vi.Directory, "Dashboard");
			if (wasRunning)
			{
				ServerProfile serverProfile = await GetActiveProfileAsync();
				if (serverProfile != null && !string.IsNullOrWhiteSpace(serverProfile.ModelPath))
				{
					_log.Information("Restarting server with profile: " + serverProfile.Name, "Dashboard");
					await _serverManager.StartAsync(serverProfile);
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to switch version to " + versionDisplay + ": " + ex.Message, "Dashboard");
		}
		finally
		{
			loadingWin?.Dispose();
		}
	}

	private string GetBaseLlamaDir()
	{
		string llamaCppBaseDirectory = _settings.LlamaCppBaseDirectory;
		if (!string.IsNullOrEmpty(llamaCppBaseDirectory) && Directory.Exists(llamaCppBaseDirectory))
		{
			return llamaCppBaseDirectory;
		}
		llamaCppBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
		if (!Directory.Exists(llamaCppBaseDirectory))
		{
			return string.Empty;
		}
		return llamaCppBaseDirectory;
	}

	private static string ExtractVersionTag(string versionKey)
	{
		if (string.IsNullOrEmpty(versionKey))
		{
			return string.Empty;
		}
		int separatorIndex = versionKey.IndexOf('|');
		if (separatorIndex < 0)
		{
			return versionKey;
		}
		return versionKey.Substring(0, separatorIndex);
	}

	private static string ExtractBuildType(string versionKey)
	{
		if (string.IsNullOrEmpty(versionKey))
		{
			return string.Empty;
		}
		int separatorIndex = versionKey.IndexOf('|');
		if (separatorIndex < 0)
		{
			return string.Empty;
		}
		return versionKey.Substring(separatorIndex + 1);
	}

	private async Task RefreshInstalledVersionsAsync()
	{
		List<(string, string, string)> versions = new List<(string, string, string)>();
		foreach (VersionInfo versionInfo in _versionCache.GetAll())
		{
			versions.Add((versionInfo.VersionKey, versionInfo.Version, versionInfo.Display));
		}
		InstalledVersions.Clear();
		foreach (var versionEntry in versions)
		{
			InstalledVersions.Add(versionEntry.Item3);
		}
		if (!string.IsNullOrWhiteSpace(ActiveVersion))
		{
			SelectedVersion = versions.FirstOrDefault<(string, string, string)>(((string VersionKey, string Version, string Display) i) => i.VersionKey == ActiveVersion).Item3;
		}
		if (SelectedVersion == null && !string.IsNullOrEmpty(_settings.ActiveLlamaCppVersion))
		{
			SelectedVersion = versions.FirstOrDefault<(string, string, string)>(((string VersionKey, string Version, string Display) i) => i.VersionKey == _settings.ActiveLlamaCppVersion).Item3;
			if (SelectedVersion != null)
			{
				ActiveVersion = _settings.ActiveLlamaCppVersion;
			}
		}
	}

	private void RaiseProfileProps()
	{
		OnPropertyChanged("ProfileName");
		OnPropertyChanged("ProfileMainModel");
		OnPropertyChanged("HasMmproj");
		OnPropertyChanged("MmprojName");
		OnPropertyChanged("HasDraft");
		OnPropertyChanged("DraftName");
		OnPropertyChanged("ProfileContextText");
		OnPropertyChanged("ProfileTempText");
		OnPropertyChanged("ProfileHasMtp");
		OnPropertyChanged("ProfileHasReasoning");
		OnPropertyChanged("ProfileGpuLayersText");
		OnPropertyChanged("ProfileFlashAttention");
		OnPropertyChanged("ProfileCachePrompt");
		OnPropertyChanged("ProfileContBatching");
		OnPropertyChanged("ProfileThreads");
		OnPropertyChanged("ProfileThreadsText");
		OnPropertyChanged("IsDefaultProfile");
		OnPropertyChanged("CanSetDefault");
		OnPropertyChanged("ActiveProfileName");
	}

	private async Task SaveSelectedProfileIdAsync(string profileId)
	{
		try
		{
			_settings.LastSelectedProfileId = profileId;
			await _settings.SaveAsync();
		}
		catch
		{
		}
	}

	[RelayCommand]
	public async Task SetDefaultProfileAsync()
	{
		if (SelectedProfile == null)
		{
			return;
		}
		string selectedId = SelectedProfile.Id;
		await _profileManager.SetDefaultProfileAsync(selectedId);
		foreach (ServerProfile allProfile in AllProfiles)
		{
			if (allProfile.Id == selectedId)
			{
				allProfile.IsDefault = true;
			}
			else
			{
				allProfile.IsDefault = false;
			}
		}
		OnPropertyChanged("CanSetDefault");
		OnPropertyChanged("IsDefaultProfile");
	}

	public DashboardViewModel(IServerManager serverManager, IModelScanner modelScanner, IProfileManager profileManager, ISettings settings, INavigationService navigation, ILocalizationService loc, ILlamaUpdater updater, IAppUpdater appUpdater, IGpuMonitor gpuMonitor, ILogService log, IDialogService dialog, IVersionCache versionCache, MonitoringViewModel? monitoringVm = null)
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
		_versionCache = versionCache;
		_monitoringVm = monitoringVm;
		GpuAvailable = gpuMonitor.IsAvailable;
		AppVersion = _appUpdater.GetCurrentVersion();
		_gpuTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(2.0)
		};
		_gpuTimer.Tick += delegate
		{
			RefreshGpuAsync();
		};
		_gpuTimer.Start();
		_updateCheckTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMinutes(5.0)
		};
		_updateCheckTimer.Tick += delegate
		{
			CheckAppUpdatesAsync();
		};
		_updateCheckTimer.Start();
		_serverManager.StatusChanged += delegate(object? s, ServerStatus status)
		{
			ServerStatus = status;
		};
		_loc.OnLanguageChanged += OnLanguageChanged;
		_profileManager.ProfileChanged += OnProfileChanged;
		_settings.SettingsChanged += OnSettingsChanged;
		if (_settings.AutoCheckUpdates)
		{
			Task.Run(async delegate
			{
				await Task.Delay(3000);
				await CheckAppUpdatesAsync();
			});
		}
	}

	public void Dispose()
	{
		_gpuTimer.Stop();
		_updateCheckTimer.Stop();
	}

	private async void OnProfileChanged(string profileId)
	{
		try
		{
			ServerProfile freshProfile = await _profileManager.GetProfileAsync(profileId);
			if (freshProfile == null)
			{
				return;
			}
			await Dispatcher.UIThread.InvokeAsync(delegate
			{
				_isInitializing = true;
				try
				{
					ServerProfile serverProfile = AllProfiles.FirstOrDefault((ServerProfile p) => p.Id == profileId);
					if (serverProfile != null)
					{
						serverProfile.IsDefault = freshProfile.IsDefault;
					}
					else
					{
						AllProfiles.Add(freshProfile);
					}
				}
				finally
				{
					_isInitializing = false;
				}
			});
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Dashboard OnProfileChanged failed", "Dashboard");
		}
	}

	private void OnSettingsChanged()
	{
		Dispatcher.UIThread.InvokeAsync(delegate
		{
			if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion) && _settings.ActiveLlamaCppVersion != ActiveVersion)
			{
				ActiveVersion = _settings.ActiveLlamaCppVersion;
				RefreshInstalledVersionsAsync();
			}
		});
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] localizedPropertyNames = new string[60]
		{
			"Title", "ServerLabel", "ModelsCard", "Indexed", "ProfilesCard", "Saved", "ProfilesSavedText", "LlamaCppCard", "ActiveBadge", "VramUsage",
			"GpuMemory", "ActiveModelLabel", "QuickActions", "ScanModelsBtn", "DiscoverGguf", "CreateProfileBtn", "LaunchConfig", "CheckUpdatesBtn", "LlamaVersionLabel", "UpToDate",
			"UpdateAvailable", "CheckingVersion", "GpuMonitorTitle", "GpuLive", "GpuVramLabel", "GpuCoreLabel", "GpuPowerLabel", "GpuFanLabel", "GpuTempLabel", "GpuMemTempLabel",
			"GpuClockLabel", "GpuModelLabel", "ServerStopped", "ServerRunning", "StartServerBtn", "StopServerBtn", "GpuThroughputText", "GpuThroughputLabel", "GpuPromptThroughputText", "GpuPromptThroughputLabel",
			"ProfileName", "ProfileTitle", "ProfileMainLabel", "ProfileMmprojLabel", "ProfileDraftLabel", "ProfileContextLabel", "ProfileTempLabel", "ProfileGpuLayersLabel", "ProfileThreadsLabel", "ProfileMtpBadge",
			"ProfileReasoningBadge", "ProfileFlashAttnBadge", "ProfileCacheBadge", "ProfileBatchingBadge", "SelectProfileLabel", "DefaultBadge", "SetDefaultBtn", "ActiveProfileLabel", "ActiveProfileName", "SwitchVersionBtn"
		};
		foreach (string propertyName in localizedPropertyNames)
		{
			OnPropertyChanged(propertyName);
		}
		OnPropertyChanged("GetUpdateButtonLabel");
	}

	[RelayCommand]
	private void Navigate(string page)
	{
		_navigation.Navigate(page);
	}

	[RelayCommand]
	private async Task StartServerAsync()
	{
		ServerProfile serverProfile = await GetActiveProfileAsync();
		if (serverProfile != null)
		{
			if (string.IsNullOrWhiteSpace(serverProfile.Host) && !string.IsNullOrWhiteSpace(_settings.DefaultHost))
			{
				serverProfile.Host = _settings.DefaultHost;
			}
			if (serverProfile.Port <= 0 && _settings.DefaultPort > 0)
			{
				serverProfile.Port = _settings.DefaultPort;
			}
			await _serverManager.StartAsync(serverProfile);
		}
	}

	// r94: public so the GpuPowerLimitControl can persist per-profile power limits.
	public async Task<ServerProfile?> GetActiveProfileAsync()
	{
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
	// r94: used by the GpuPowerLimitControl after a successful apply.
	public Task<string> SaveProfileAsync(ServerProfile profile) => _profileManager.SaveProfileAsync(profile);

	[RelayCommand]
	private async Task StopServerAsync()
	{
		await _serverManager.StopAsync();
	}

	[RelayCommand]
	private async Task CheckVersionAsync()
	{
		IsCheckingVersion = true;
		try
		{
			// r42: latest release now means the newest build (prerelease b####).
			LlamaCppRelease llamaCppRelease = await _updater.GetLatestReleaseAsync(true);
			if (llamaCppRelease != null)
			{
				LatestVersion = llamaCppRelease.TagName;
				string activeVersionTag = ExtractVersionTag(ActiveVersion);
				HasUpdate = !string.Equals(activeVersionTag, llamaCppRelease.TagName, StringComparison.OrdinalIgnoreCase);
				if (HasUpdate)
				{
					string activeBuildType = ExtractBuildType(ActiveVersion);
					if (!string.IsNullOrEmpty(activeBuildType))
					{
						LatestVersionInstalled = _versionCache.IsVersionInstalled(llamaCppRelease.TagName, activeBuildType);
					}
					else
					{
						LatestVersionInstalled = _versionCache.IsVersionInstalled(llamaCppRelease.TagName);
					}
				}
				else
				{
					LatestVersionInstalled = false;
				}
			}
		}
		catch
		{
		}
		finally
		{
			IsCheckingVersion = false;
		}
		await RefreshInstalledVersionsAsync();
	}

	[RelayCommand]
	private void OpenMonitoring()
	{
		try
		{
			if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
			{
				_monitoringVm?.RefreshFromSettings();
				Window window = classicDesktopStyleApplicationLifetime.Windows.FirstOrDefault((Window w) => w is MonitoringWindow);
				if (window != null)
				{
					window.Show();
					window.Activate();
				}
				else
				{
					new MonitoringWindow(_monitoringVm).Show(classicDesktopStyleApplicationLifetime.MainWindow);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	[RelayCommand]
	private async Task UpdateLlamaCppAsync()
	{
		if (string.IsNullOrWhiteSpace(LatestVersion))
		{
			return;
		}
		if (LatestVersionInstalled)
		{
			VersionInfo byVersion = _versionCache.GetByVersion(LatestVersion);
			if (byVersion != null)
			{
				await SwitchVersionAsync(byVersion.Display);
				HasUpdate = false;
				LatestVersionInstalled = false;
				return;
			}
		}
		IsUpdating = true;
		UpdateProgress = 0.0;
		try
		{
			_updater.DownloadProgress += delegate(object? s, double progress)
			{
				UpdateProgress = progress;
			};
			string baseDir = _settings.LlamaCppBaseDirectory;
			if (string.IsNullOrEmpty(baseDir))
			{
				baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
				_settings.LlamaCppBaseDirectory = baseDir;
			}
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			string installPath = await _updater.DownloadAndExtractAsync(LatestVersion, baseDir);
			await _versionCache.ScanAsync(baseDir);
			VersionInfo byPath = _versionCache.GetByPath(installPath);
			string newVersionKey = byPath?.VersionKey ?? LatestVersion;
			string installedVersionName = byPath?.Display ?? LatestVersion;
			_settings.LlamaCppDirectory = installPath;
			_settings.ActiveLlamaCppVersion = newVersionKey;
			await _settings.SaveAsync();
			ActiveVersion = newVersionKey;
			HasUpdate = false;
			await RefreshInstalledVersionsAsync();
			if (ServerStatus.State == ServerState.Running)
			{
				if (await _dialog.ShowConfirmationAsync(string.Format(_loc.T("dash.update_restart_server"), installedVersionName), _loc.T("dash.update_installed")))
				{
					await RestartServerAfterUpdateAsync();
				}
			}
			else
			{
				await _dialog.ShowSuccessAsync(string.Format(_loc.T("dash.update_installed_msg"), installedVersionName), _loc.T("dash.update_installed"));
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			IsUpdating = false;
			_updater.DownloadProgress -= delegate(object? s, double progress)
			{
				UpdateProgress = progress;
			};
		}
	}

	private async Task RestartServerAfterUpdateAsync()
	{
		try
		{
			await _serverManager.StopAsync();
			await Task.Delay(1000);
			ServerProfile serverProfile = CurrentProfile ?? SelectedProfile;
			if (serverProfile != null)
			{
				await _serverManager.StartAsync(serverProfile);
				ServerStatus = await _serverManager.GetStatusAsync();
				await _dialog.ShowSuccessAsync(_loc.T("dash.server_restarted"), _loc.T("dash.success"));
			}
		}
		catch (Exception ex)
		{
			await _dialog.ShowErrorAsync(_loc.T("dash.restart_failed") + ": " + ex.Message, _loc.T("dash.error"));
		}
	}

	private async Task CheckAppUpdatesAsync()
	{
		try
		{
			AppUpdateInfo update = await _appUpdater.CheckForUpdatesAsync();
			await Dispatcher.UIThread.InvokeAsync(delegate
			{
				if (update != null)
				{
					HasAppUpdate = true;
					AppLatestVersion = update.Version;
					_log.Information("App update available: " + update.Version, "Dashboard");
				}
				else
				{
					HasAppUpdate = false;
					_log.Information("App is up to date: " + AppVersion, "Dashboard");
				}
			});
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to check app updates (keeping previous state): " + ex.Message, "Dashboard");
		}
	}

	[RelayCommand]
	private async Task UpdateAppAsync()
	{
		try
		{
			IsUpdatingApp = true;
			AppUpdateProgress = 0.0;
			_appUpdater.ProgressChanged += delegate(object? s, double progress)
			{
				AppUpdateProgress = progress;
			};
			AppUpdateInfo appUpdateInfo = await _appUpdater.CheckForUpdatesAsync();
			if (appUpdateInfo == null)
			{
				IsUpdatingApp = false;
				return;
			}
			AppLatestVersion = appUpdateInfo.Version;
			HasAppUpdate = true;
			string tempDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
			if (!Directory.Exists(tempDir))
			{
				Directory.CreateDirectory(tempDir);
			}
			string downloadedPath = await _appUpdater.DownloadUpdateAsync(appUpdateInfo, tempDir);
			if (await _dialog.ShowConfirmationAsync(string.Format(_loc.T("update.downloaded_restart"), AppLatestVersion), _loc.T("update.ready_title")))
			{
				await ApplyUpdateAndRestart(downloadedPath);
				return;
			}
			File.WriteAllText(Path.Combine(tempDir, "pending_update.txt"), downloadedPath);
			await _dialog.ShowMessageAsync(string.Format(_loc.T("update.ready_restart_later"), AppLatestVersion), _loc.T("update.ready_title"));
		}
		catch (Exception ex)
		{
			_log.Error("App update failed: " + ex.Message, "Dashboard");
		}
		finally
		{
			IsUpdatingApp = false;
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
		string currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
		if (!string.IsNullOrEmpty(currentExePath))
		{
			string updateDir = Path.Combine(Path.GetTempPath(), "LlamaStudio_Update");
			string logPath = Path.Combine(updateDir, "update_log.txt");
			string restartBatPath = Path.Combine(updateDir, "restart.bat");
			string currentExeEscaped = EscapeBatString(currentExePath);
			string downloadedExeEscaped = EscapeBatString(downloadedPath);
			string logPathEscaped = EscapeBatString(logPath);
			string restartBatEscaped = EscapeBatString(restartBatPath);
			File.WriteAllText(restartBatPath, $"@echo off\r\nchcp 65001 >nul\r\nsetlocal\r\necho [{{date}} {{time}}] Starting update process > \"{logPathEscaped}\"\r\necho Current EXE: {currentExeEscaped} >> \"{logPathEscaped}\"\r\necho Downloaded EXE: {downloadedExeEscaped} >> \"{logPathEscaped}\"\r\n\r\n:: Kill any remaining processes\r\ntaskkill /F /IM LlamaStudio.exe >> \"{logPathEscaped}\" 2>&1\r\ntimeout /t 3 /nobreak >nul\r\n\r\n:: Replace the executable\r\necho Deleting old EXE... >> \"{logPathEscaped}\"\r\ndel /F /Q \"{currentExeEscaped}\" >> \"{logPathEscaped}\" 2>&1\r\ntimeout /t 1 /nobreak >nul\r\n\r\necho Copying new EXE... >> \"{logPathEscaped}\"\r\ncopy /Y \"{downloadedExeEscaped}\" \"{currentExeEscaped}\" >> \"{logPathEscaped}\" 2>&1\r\n\r\nif exist \"{currentExeEscaped}\" (\r\n    echo Update successful! Starting new version... >> \"{logPathEscaped}\"\r\n    timeout /t 1 /nobreak >nul\r\n    start \"\" \"{currentExeEscaped}\"\r\n    :: Clean up after successful start\r\n    ping 127.0.0.1 -n 2 >nul\r\n    del \"{restartBatEscaped}\" >> \"{logPathEscaped}\" 2>&1\r\n) else (\r\n    echo ERROR: New file not found after copy! >> \"{logPathEscaped}\"\r\n    pause\r\n)\r\n", new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			_log.Information("Update script created: " + restartBatPath, "Dashboard");
			_log.Information("Current EXE: " + currentExePath, "Dashboard");
			_log.Information("Downloaded EXE: " + downloadedPath, "Dashboard");
			Process.Start(new ProcessStartInfo
			{
				FileName = restartBatPath,
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			_log.Information("App update downloaded, restarting...", "Dashboard");
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
			_log.Error("Linux update: the application folder cannot be swapped (too high in the tree): " + appDir, "Dashboard");
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
			_log.Error("Linux update: no write permission for " + parent + ": " + ex.Message, "Dashboard");
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
		_log.Information("Linux update script created: " + scriptPath, "Dashboard");
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
			_log.Error("Linux update: failed to start the update script: " + ex.Message, "Dashboard");
			await _dialog.ShowMessageAsync(_loc.T("update.linux_cannot_update"), _loc.T("update.ready_title"));
			return;
		}
		_log.Information("Linux update: restarting...", "Dashboard");
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

	public async Task RefreshAsync()
	{
		_isInitializing = true;
		try
		{
			ServerStatus = await _serverManager.GetStatusAsync();
			if (!string.IsNullOrWhiteSpace(_settings.ModelsDirectory) && Directory.Exists(_settings.ModelsDirectory))
			{
				TotalModels = (await _modelScanner.ScanDirectoryAsync(_settings.ModelsDirectory)).Count;
			}
			List<ServerProfile> profiles = (await _profileManager.GetAllProfilesAsync()).Where((ServerProfile p) => string.IsNullOrWhiteSpace(p.ModelPath) || File.Exists(p.ModelPath)).ToList();
			TotalProfiles = profiles.Count;
			AllProfiles.Clear();
			foreach (ServerProfile profile in profiles)
			{
				AllProfiles.Add(profile);
			}
			ServerProfile serverProfile = null;
			if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
			{
				serverProfile = AllProfiles.FirstOrDefault((ServerProfile p) => p.Id == _settings.LastSelectedProfileId);
			}
			if (serverProfile == null)
			{
				serverProfile = AllProfiles.FirstOrDefault((ServerProfile p) => p.IsDefault) ?? AllProfiles.FirstOrDefault();
			}
			if (SelectedProfile?.Id != serverProfile?.Id)
			{
				SelectedProfile = serverProfile;
			}
			CurrentProfile = serverProfile;
			SelectedModel = ((serverProfile != null && serverProfile.ModelPath != null) ? Path.GetFileName(serverProfile.ModelPath) : "No model selected");
			if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
			{
				ActiveVersion = _settings.ActiveLlamaCppVersion;
			}
			await RefreshInstalledVersionsAsync();
			EstimatedVramGb = ServerStatus.VramUsedGb;
			VramPercentage = ((ServerStatus.VramUsedGb > 0.0) ? Math.Min(ServerStatus.VramUsedGb / 24.0 * 100.0, 100.0) : 0.0);
			// Populate the dashboard monitor during the initial load. The timer is
			// intentionally only a fallback for later telemetry updates; waiting for
			// a tab switch must not be required to create the GPU cards.
			await RefreshGpuAsync();
			RaiseProfileProps();
			CheckVersionAsync();
		}
		finally
		{
			_isInitializing = false;
		}
	}

	private async Task RefreshGpuAsync()
	{
		if (_gpuRefreshInProgress)
		{
			return;
		}
		_gpuRefreshInProgress = true;
		try
		{
			List<GpuInfo> gpuInfos = (await _gpuMonitor.GetGpuInfosAsync()).ToList();
			GpuInfo? remoteGpu = _monitoringVm?.GpuInfos.FirstOrDefault(gpu => gpu.IsRemote);
			if (remoteGpu != null)
			{
				gpuInfos.RemoveAll(gpu => gpu.IsRemote);
				gpuInfos.Add(remoteGpu);
			}
			GpuInfo gpuInfo = gpuInfos.FirstOrDefault();
			if (gpuInfo != null)
			{
				GpuInfos = gpuInfos;
				GpuInfo = gpuInfo;
				OnPropertyChanged("ShowSingleGpuMonitor");
				OnPropertyChanged("GpuName");
				OnPropertyChanged("GpuMemoryInfo");
				OnPropertyChanged("GpuTempText");
				OnPropertyChanged("GpuVramGbText");
				OnPropertyChanged("GpuVramText");
				OnPropertyChanged("GpuCoreText");
				OnPropertyChanged("GpuPowerWattsText");
				OnPropertyChanged("GpuPowerText");
				OnPropertyChanged("GpuFanText");
				OnPropertyChanged("GpuClockText");
				OnPropertyChanged("GpuTempPercent");
				OnPropertyChanged("GpuTempBrush");
				OnPropertyChanged("GpuMemTempText");
				OnPropertyChanged("GpuMemTempPercent");
				OnPropertyChanged("GpuMemTempBrush");
				OnPropertyChanged("HasGpuMemTemp");
				OnPropertyChanged("GpuVramUsage");
				OnPropertyChanged("GpuCoreUsage");
				OnPropertyChanged("GpuPowerUsage");
				OnPropertyChanged("GpuFanSpeed");
			}
		}
		catch
		{
		}
		finally
		{
			_gpuRefreshInProgress = false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnServerStatusChanged(ServerStatus value)
	{
		_lastTps = value.TokensPerSecond;
		_lastPromptTps = value.PromptTokensPerSecond;
		EstimatedVramGb = value.VramUsedGb;
		VramPercentage = ((value.VramUsedGb > 0.0) ? Math.Min(value.VramUsedGb / 24.0 * 100.0, 100.0) : 0.0);
		SelectedModel = value.ModelName ?? "No model selected";
		OnPropertyChanged("ServerStopped");
		OnPropertyChanged("ServerRunning");
		OnPropertyChanged("GpuThroughputText");
		OnPropertyChanged("GpuPromptThroughputText");
		OnPropertyChanged("RamUsedText");
		OnPropertyChanged("RamPercentText");
		OnPropertyChanged("RamUsedPercent");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTotalProfilesChanged(int value)
	{
		OnPropertyChanged("ProfilesSavedText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnActiveVersionChanged(string value)
	{
		OnPropertyChanged("UpdateButtonLabel");
		OnPropertyChanged("VersionStatusText");
		OnPropertyChanged("UpdateBtnText");
		OnPropertyChanged("ActiveVersionDisplay");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHasUpdateChanged(bool value)
	{
		OnPropertyChanged("UpdateButtonLabel");
		OnPropertyChanged("VersionStatusText");
		OnPropertyChanged("UpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLatestVersionInstalledChanged(bool value)
	{
		OnPropertyChanged("UpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLatestVersionChanged(string value)
	{
		OnPropertyChanged("UpdateButtonLabel");
		OnPropertyChanged("VersionStatusText");
		OnPropertyChanged("UpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsCheckingVersionChanged(bool value)
	{
		OnPropertyChanged("UpdateButtonLabel");
		OnPropertyChanged("VersionStatusText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsUpdatingChanged(bool value)
	{
		OnPropertyChanged("UpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnUpdateProgressChanged(double value)
	{
		OnPropertyChanged("UpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHasAppUpdateChanged(bool value)
	{
		OnPropertyChanged("AppUpdateStatus");
		OnPropertyChanged("AppUpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsUpdatingAppChanged(bool value)
	{
		OnPropertyChanged("AppUpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAppUpdateProgressChanged(double value)
	{
		OnPropertyChanged("AppUpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAppLatestVersionChanged(string value)
	{
		OnPropertyChanged("AppUpdateStatus");
		OnPropertyChanged("AppUpdateBtnText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedVersionChanged(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			VersionInfo versionInfo = _versionCache.GetAll().FirstOrDefault((VersionInfo v) => v.Display == value);
			if (versionInfo == null)
			{
				string version = value.Split('(')[0].Trim();
				versionInfo = _versionCache.GetByVersion(version);
			}
			if (versionInfo == null || !versionInfo.Directory.Equals(_settings.LlamaCppDirectory.TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase))
			{
				SwitchVersionAsync(value);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedProfileChanged(ServerProfile? value)
	{
		CurrentProfile = value;
		if (value != null)
		{
			SaveSelectedProfileIdAsync(value.Id.ToString());
			if (!_isInitializing && !_isSettingDefault)
			{
				_profileManager.NotifyProfileChanged(value.Id.ToString());
			}
		}
		RaiseProfileProps();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnCurrentProfileChanged(ServerProfile? value)
	{
		RaiseProfileProps();
	}
}
