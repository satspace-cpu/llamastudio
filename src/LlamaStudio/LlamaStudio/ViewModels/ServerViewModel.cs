using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
using Renci.SshNet;

namespace LlamaStudio.ViewModels;

public class ServerViewModel : ObservableObject
{
	private static string LlamaServerExecutableName => OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server";
	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly IModelScanner _scanner;

	private readonly ILlamaUpdater _updater;

	private readonly ILocalizationService _loc;

	private readonly IHelpParser _helpParser;

	private readonly ICliValidator _cliValidator;

	private readonly IHuggingFaceDownloader _hfDownloader;

	private readonly IGpuMonitor _gpuMonitor;

	private readonly MonitoringViewModel _monitoringVm;

	private readonly IVersionCache _versionCache;

	private readonly DispatcherTimer _gpuTimer;

	private int _tickCounter;

	private int _rpcAutoStartInProgress;

	private int _rpcStartInProgress;

	private bool _rpcStartupAttempted;

	private Task? _rpcStartupTask;

	private string? _previousSelectedProfileId;

	private DateTime _lastServerRestart = DateTime.MinValue;

	private Task? _profileRestartTask;

	private readonly TaskCompletionSource<bool> _profileNotifiedTcs = new TaskCompletionSource<bool>();

	[ObservableProperty]
	private ServerState _state;

	[ObservableProperty]
	private double _vramUsedGb;

	[ObservableProperty]
	private double _ramUsedGb;

	private double _ramCommittedGb;

	public double RamCommittedGb
	{
		get
		{
			return _ramCommittedGb;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_ramCommittedGb, value))
			{
				_ramCommittedGb = value;
				OnRamCommittedGbChanged(value);
				OnPropertyChanged("RamCommittedGb");
			}
		}
	}

	private static readonly double s_totalRamGb = (double)GetTotalPhysicalMemory() / 1073741824.0;

	[ObservableProperty]
	private double _tokensPerSecond;

	[ObservableProperty]
	private double _promptTokensPerSecond;

	[ObservableProperty]
	private double _gpuVramUsedGb;

	[ObservableProperty]
	private int _queueSize;

	[ObservableProperty]
	private int _activeSlots;

	[ObservableProperty]
	private TimeSpan _uptime;

	[ObservableProperty]
	private string _processId = "—";

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	[ObservableProperty]
	private ObservableCollection<ServerProfile> _profiles = new ObservableCollection<ServerProfile>();

	[ObservableProperty]
	private ServerProfile? _selectedProfile;

	[ObservableProperty]
	private string _commandLinePreview = string.Empty;

	[ObservableProperty]
	private string _modelPath = string.Empty;

	[ObservableProperty]
	private string _mmprojPath = string.Empty;

	[ObservableProperty]
	private string _draftModelPath = string.Empty;

	[ObservableProperty]
	private string _modelDisplayName = "No model selected";

	[ObservableProperty]
	private string _mmprojDisplayName = "No mmproj selected";

	[ObservableProperty]
	private string _draftDisplayName = "No draft model selected";

	[ObservableProperty]
	private string _hfRepo = string.Empty;

	[ObservableProperty]
	private string _hfFile = string.Empty;

	[ObservableProperty]
	private bool _hfOffline;

	[ObservableProperty]
	private string _hfRepoDraft = string.Empty;

	[ObservableProperty]
	private bool _isDownloadingHf;

	[ObservableProperty]
	private double _hfDownloadProgress;

	[ObservableProperty]
	private string _hfDownloadStatus = string.Empty;

	[ObservableProperty]
	private string _hfDownloadDirectory = string.Empty;

	[ObservableProperty]
	private string _modelDescription = string.Empty;

	[ObservableProperty]
	private string _modelSizeInfo = string.Empty;

	[ObservableProperty]
	private string _modelParamsInfo = string.Empty;

	[ObservableProperty]
	private string _host = "127.0.0.1";

	[ObservableProperty]
	private int _port = 8080;

	private bool _rpcEnabled;

	private string _rpcEndpoint = string.Empty;

	private string _rpcConnectionStatus = string.Empty;
	private string _rpcSshStatus = string.Empty;

	private string _rpcHost = string.Empty;

	private int _rpcPort = 50052;

	private string _rpcSshUsername = "server";

	private string _rpcSshPassword = string.Empty;
	private bool _rpcSshPasswordEdited;
	private bool _applyingRpcProfile;

	private bool _rpcSavePassword;

	private bool _rpcUseCache;

	private string _rpcServerPath = string.Empty;

	private bool _rpcConnectionVerified;
	private bool _rpcSshConnected;
	private int _rpcSshCheckInProgress;

	private bool _remoteTelemetryEnabled = true;
	private int _remoteTelemetryRefreshInProgress;
	private DateTime _lastRemoteTelemetryAt = DateTime.MinValue;
	private GpuInfo? _lastRemoteGpuTelemetry;

	// Diagnostic mode: the remote ggml-rpc-server is started and stopped manually.
	// Keep the RPC endpoint in the llama-server command line, but do not let the
	// desktop app touch the remote host while isolating RPC transport failures.
	private const bool PassiveRpcMode = false;

	private const int RemoteGpuIndex = 100000;

	[ObservableProperty]
	private int _timeout = 3600;

	[ObservableProperty]
	private int _slots = 1;

	[ObservableProperty]
	private string _gpuLayers = "all";

	[ObservableProperty]
	private int _threads = -1;

	[ObservableProperty]
	private int _threadsBatch = -1;

	[ObservableProperty]
	private bool _flashAttention = true;

	[ObservableProperty]
	private bool _mmap = true;

	[ObservableProperty]
	private bool _mlock;

	[ObservableProperty]
	private bool _mmqEnabled = true;

	[ObservableProperty]
	private bool _kvOffloadEnabled = true;

	[ObservableProperty]
	private int _mainGpu = -1;

	[ObservableProperty]
	private GpuSplitMode _gpuSplitMode;

	[ObservableProperty]
	private string _tensorSplit = string.Empty;

	private readonly ObservableCollection<GpuInfo> _gpuInfos = new ObservableCollection<GpuInfo>();

	private int[] _pendingGpuSelection = Array.Empty<int>();

	private bool _gpuSelectionInitialized;

	public ReadOnlyObservableCollection<GpuInfo> GpuInfos { get; }

	public bool HasMultipleGpus => GpuInfos.Count > 1;

	public bool ShowLegacyGpuFields => !HasMultipleGpus;

	public bool HasGpuSelection => GpuInfos.Count > 0;

	public string SelectedGpuDeviceText => string.Join(",", GpuInfos.Where((GpuInfo gpu) => gpu.IsSelected).Select((GpuInfo gpu) => gpu.CudaId));

	public string RecommendedTensorSplitText => string.Join(",", CalculateRecommendedSplit(GpuInfos.Where((GpuInfo gpu) => gpu.IsSelected).ToList()));

	[ObservableProperty]
	private int _contextSize;

	[ObservableProperty]
	private int _batchSize = 2048;

	[ObservableProperty]
	private int _ubatchSize = 512;

	[ObservableProperty]
	private int _maxTokens = -1;

	[ObservableProperty]
	private string _cacheTypeK = "f16";

	[ObservableProperty]
	private string _cacheTypeV = "f16";

	[ObservableProperty]
	private int _cacheRam = -1;

	[ObservableProperty]
	private int _cacheReuse;

	[ObservableProperty]
	private float _temperature = 0.8f;

	[ObservableProperty]
	private int _topK = 40;

	[ObservableProperty]
	private float _topP = 0.95f;

	[ObservableProperty]
	private float _minP = 0.05f;

	[ObservableProperty]
	private float _typicalP = 1f;

	[ObservableProperty]
	private float _repeatPenalty = 1f;

	[ObservableProperty]
	private int _repeatLastN = 64;

	[ObservableProperty]
	private float _presencePenalty;

	[ObservableProperty]
	private float _frequencyPenalty;

	[ObservableProperty]
	private int _seed = -1;

	[ObservableProperty]
	private MirostatMode _mirostatMode;

	[ObservableProperty]
	private float _mirostatTau = 5f;

	[ObservableProperty]
	private float _mirostatEta = 0.1f;

	[ObservableProperty]
	private float _dryMultiplier;

	[ObservableProperty]
	private float _dryBase = 1.75f;

	[ObservableProperty]
	private float _dynatempStddev;

	[ObservableProperty]
	private float _xtcProbability;

	[ObservableProperty]
	private float _xtcThreshold = 0.1f;

	[ObservableProperty]
	private string _specType = "none";

	[ObservableProperty]
	private int _predictCount = -1;

	[ObservableProperty]
	private bool _speculativeDecoding;

	private bool _jinja = true;

	private bool _draftModelLocalOnly;

	[ObservableProperty]
	private string _specDraftGpuLayers = string.Empty;

	[ObservableProperty]
	private int _specDraftNMax = 3;

	[ObservableProperty]
	private int _specDraftNMin;

	[ObservableProperty]
	private float _specDraftPSplit = 0.1f;

	[ObservableProperty]
	private float _specDraftPMin;

	[ObservableProperty]
	private bool _priorityHigh;

	[ObservableProperty]
	private bool _embeddingMode;

	[ObservableProperty]
	private string _poolingType = string.Empty;

	[ObservableProperty]
	private bool _numa;

	[ObservableProperty]
	private string _processPriority = "Normal";

	[ObservableProperty]
	private bool _cachePrompt = true;

	[ObservableProperty]
	private bool _contBatching = true;

	[ObservableProperty]
	private bool _contextShift = true;

	[ObservableProperty]
	private bool _verboseLogging;

	[ObservableProperty]
	private bool _enableWebUI = true;

	[ObservableProperty]
	private bool _enableSlots = true;

	[ObservableProperty]
	private bool _enableMetrics;

	[ObservableProperty]
	private bool _reasoning;

	[ObservableProperty]
	private int _reasoningBudget;

	[ObservableProperty]
	private string _ropeScaling = string.Empty;

	[ObservableProperty]
	private double? _ropeFreqBase;

	[ObservableProperty]
	private double? _ropeFreqScale;

	[ObservableProperty]
	private int? _yarnOriginalContext;

	[ObservableProperty]
	private double? _yarnExtFactor;

	[ObservableProperty]
	private double? _yarnAttnFactor;

	[ObservableProperty]
	private double? _yarnBetaFast;

	[ObservableProperty]
	private double? _yarnBetaSlow;

	[ObservableProperty]
	private string _apiKey = string.Empty;

	[ObservableProperty]
	private string _alias = string.Empty;

	[ObservableProperty]
	private string _additionalArgs = string.Empty;

	[ObservableProperty]
	private string _llamaCppDirectory = string.Empty;

	[ObservableProperty]
	private string _executablePath = string.Empty;

	[ObservableProperty]
	private bool _executableFound;

	[ObservableProperty]
	private string _llamaVersionInfo = string.Empty;

	[ObservableProperty]
	private ObservableCollection<string> _availableServers = new ObservableCollection<string>();

	[ObservableProperty]
	private string? _selectedServer;

	[ObservableProperty]
	private string _cudaInfo = string.Empty;

	private LlamaHelpInfo? _helpInfo;

	[ObservableProperty]
	private ValidationReport? _validationReport;

	[ObservableProperty]
	private CliChangeReport? _cliChangeReport;

	[ObservableProperty]
	private bool _isCliValidating;

	[ObservableProperty]
	private string _validationSummary = string.Empty;

	[ObservableProperty]
	private string _validationDetails = string.Empty;

	[ObservableProperty]
	private SolidColorBrush _validationSummaryForeground = new SolidColorBrush(Color.Parse("#F87171"));

	[ObservableProperty]
	private ObservableCollection<GgufModelInfo> _scannedModels = new ObservableCollection<GgufModelInfo>();

	[ObservableProperty]
	private GgufModelInfo? _quickSelectedModel;

	[ObservableProperty]
	private bool _isScanningModels;

	private Timer? _commandLineDebounceTimer;

	private Timer? _autoSaveDebounceTimer;

	private bool _isInitializing;
	private bool _profileReady;
	private bool _isApplyingProfile;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<RecommendedModel>? selectRecommendedModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? downloadHfModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? browseModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? browseMmprojCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? browseDraftModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? quickScanModelsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? quickSelectModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? quickSelectMmprojCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? quickSelectDraftCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? startServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? stopServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? measureSpeedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? createNewProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveToProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? renameProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? deleteProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? cleanupMissingModelProfilesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? setDefaultProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? copyFromProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? checkLlamaCppVersionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? validateCliCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectLlamaCppDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? scanServersAsyncCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? selectServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? exportToBatCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? copyCommandLineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? importFromBatCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? openArgPickerCommand;

	public string Title => _loc.T("server.title");

	/// <summary>r105: while a Benchmark test is going on (llama-bench owns the
	/// GPU and the server is stopped by design) starting the server is forbidden —
	/// the button says exactly that instead of «Запустить сервер».</summary>
	public string StartBtn => BenchmarkCenterViewModel.Current is { IsRunning: true }
		? "● Идёт тест llama-bench"
		: _loc.T("server.start_btn");

	public string StopBtn => _loc.T("server.stop_btn");

	public string MeasureSpeedBtn => _loc.T("server.measure_speed_btn");

	public string SaveProfileBtn => _loc.T("server.save_profile_btn");

	public string LoadProfileBtn => _loc.T("server.load_profile_btn");

	public string CopyFromProfileBtn => _loc.T("server.copy_from_profile_btn");

	public string RenameProfileBtn => _loc.T("server.rename_profile_btn");

	public string DeleteProfileBtn => _loc.T("server.delete_profile_btn");

	public string CreateProfileBtn => _loc.T("server.create_profile_btn");

	public string ExportBatBtn => _loc.T("server.export_bat_btn");

	public string CopyCmdLineBtn => _loc.T("server.copy_cmdline_btn");

	public string NoProfileSelected => _loc.T("server.no_profile_selected");

	public string NoModelSelected => _loc.T("server.no_model_selected");

	public string BrowseModelBtn => _loc.T("server.browse_model_btn");

	public string BrowseMmprojBtn => _loc.T("server.browse_mmproj_btn");

	public string BrowseDraftBtn => _loc.T("server.browse_draft_btn");

	public string QuickScanBtn => _loc.T("server.quick_scan_btn");

	public string QuickSelectModelBtn => _loc.T("server.quick_select_model_btn");

	public string QuickSelectMmprojBtn => _loc.T("server.quick_select_mmproj_btn");

	public string QuickSelectDraftBtn => _loc.T("server.quick_select_draft_btn");

	public string CheckVersionBtn => _loc.T("server.check_version_btn");

	public string ValidateCliBtn => _loc.T("server.validate_cli_btn");

	public string SelectLlamaDirBtn => _loc.T("server.select_llama_dir_btn");

	public string ExecutableFoundText
	{
		get
		{
			if (!ExecutableFound)
			{
				return _loc.T("server.exec_not_found");
			}
			return string.Format(_loc.T("server.exec_found"), ExecutablePath);
		}
	}

	public string ServerDirectory
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
			{
				VersionInfo byVersionKey = _versionCache.GetByVersionKey(_settings.ActiveLlamaCppVersion);
				if (byVersionKey != null && Directory.Exists(byVersionKey.Directory))
				{
					return byVersionKey.Directory;
				}
				byVersionKey = _versionCache.GetByVersion(_settings.ActiveLlamaCppVersion);
				if (byVersionKey != null && Directory.Exists(byVersionKey.Directory))
				{
					return byVersionKey.Directory;
				}
			}
			return _settings.LlamaCppDirectory;
		}
	}

	public string StateLabel => _loc.T("server.state_label");

	public string TabModel => _loc.T("server.tab_model");

	public string TabGpu => _loc.T("server.tab_gpu");

	public string TabContextSampling => _loc.T("server.tab_context_sampling");

	public string TabAdvanced => _loc.T("server.tab_advanced");

	public string TabConnection => _loc.T("server.tab_connection");

	public string ServerPathTitle => _loc.T("server.server_path_title");

	public string ServerBuildLabel => _loc.T("server.build_label");

	public string ExecHint => _loc.T("server.exec_hint");

	public string MainModelLabel => _loc.T("server.main_model") + " (-m)";

	public string ModelWatermark => _loc.T("server.model_watermark");

	public string MmprojTitle => _loc.T("server.mmproj_title") + " (--mmproj)";

	public string OptionalBadge => _loc.T("server.optional");

	public string MmprojWatermark => _loc.T("server.mmproj_watermark");

	public string DraftTitle => _loc.T("server.draft_title") + " (-md)";

	public string AdvancedBadge => _loc.T("server.advanced_badge");

	public string EnableSpeculative => _loc.T("server.enable_speculative") + " (-md)";

	public string DraftWatermark => _loc.T("server.draft_watermark");

	public string QuickSelectTitle => _loc.T("server.quick_select_title");

	public string HfTitle => _loc.T("server.hf_title") + " (--hf)";

	public string DownloadToLabel => _loc.T("server.download_to_fmt");

	public string SelectThisModelBtn => _loc.T("server.select_this_model");

	public string DownloadModelBtn => _loc.T("server.download_model_btn");

	public string DownloadingStatus => _loc.T("server.downloading_status");

	public string OrManual => _loc.T("server.or_manual");

	public string RepoLabel => _loc.T("server.repo_label");

	public string OfflineMode => _loc.T("server.offline_mode") + " (--offline)";

	public string GpuAcceleration => _loc.T("server.gpu_acceleration");

	public string GpuLayersLabel => _loc.T("server.gpu_layers_label") + " (-ngl)" + NglModeSuffix;

	// r68: show the explicit GPU layers mode in the label (Auto / All / number).
	public string NglModeSuffix
	{
		get
		{
			if (string.IsNullOrWhiteSpace(GpuLayers) || GpuLayers == "-1")
			{
				return " \u2014 " + _loc.T("server.gpu_layers_mode_auto");
			}
			if (GpuLayers == "all")
			{
				return " \u2014 " + _loc.T("server.gpu_layers_mode_all");
			}
			return " \u2014 " + GpuLayers;
		}
	}

	public string ThreadsLabel => _loc.T("server.threads_label") + " (-t)";

	public string ThreadsBatchLabel => _loc.T("server.threads_batch_label") + " (-tb)";

	public string MainGpuLabel => _loc.T("server.main_gpu_label") + " (-mg)";

	public string MainGpuShortLabel => _loc.T("server.main_gpu_label").TrimEnd(':', ' ');

	public string SplitModeLabel => _loc.T("server.split_mode_label") + " (-sm)";

	public string SplitModeTip => _loc.T("server.split_mode_tip");

	public string UsedGpusLabel => _loc.T("server.used_gpus");

	public string MainGpuDisabledTensorSplitLabel => _loc.T("server.main_gpu_disabled_tensor_split");

	public string MainGpuSplitTip => _loc.T("server.main_gpu_split_tip");

	public string MainGpuTip => _loc.T("server.main_gpu_tip");

	public string ModelDistributionLabel => _loc.T("server.model_distribution");

	public string AutomaticDistributionLabel => _loc.T("server.auto_distribution");

	public string SplitByVramLabel => _loc.T("server.split_by_vram");

	public string SplitEqualLabel => _loc.T("server.split_equal");

	public string GpuMonitoringLabel => _loc.T("server.gpu_monitoring");

	public string RamLabel => _loc.T("mon.ram");

	public string VramLabel => _loc.T("mon.vram");

	public string GpuTempLabel => _loc.T("mon.gpu_temp");

	public string GpuMemTempLabel => _loc.T("mon.mem_temperature");

	public string PowerLabel => _loc.T("mon.power");

	public string GpuCoreLabel => _loc.T("mon.gpu_core");

	public string FanLabel => _loc.T("mon.fan");

	public string ClockLabel => _loc.T("mon.clock");

	public string ComputeCapabilityLabel => _loc.T("mon.compute_capability");

	public string FlashAttentionCheck => _loc.T("server.flash_attention") + " (-fa)";

	public string MemoryMapCheck => _loc.T("server.memory_map") + " (--mmap)";

	public string MlockCheck => _loc.T("server.mlock") + " (--mlock)";

	public string MmqCheck => "MMQ (--no-mmq)";

	public string KvOffloadCheck => "KV Offload (-nkvo)";

	public string CacheTypeKLabel => _loc.T("server.cache_type_k") + " (-ctk)";

	public string CacheTypeVLabel => _loc.T("server.cache_type_v") + " (-ctv)";

	public string CacheTypeKTip => _loc.T("server.tip_cache_type_k");

	public string CacheTypeVTip => _loc.T("server.tip_cache_type_v");

	public IEnumerable<string> CacheTypeKOptions => new string[9] { "f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1" };

	public IEnumerable<string> CacheTypeVOptions => new string[9] { "f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1" };

	public string CacheRamLabel => _loc.T("server.cache_ram") + " (--cache-ram)";

	public string CacheRamTip => _loc.T("server.tip_cache_ram");

	public string CacheReuseLabel => _loc.T("server.cache_reuse") + " (--cache-reuse)";

	public string CacheReuseTip => _loc.T("server.tip_cache_reuse");

	public string ContextBatchTitle => _loc.T("server.context_batch");

	public string ContextLabel => _loc.T("server.context_label") + " (-c)";

	public string BatchLabel => _loc.T("server.batch_label") + " (-b)";

	public string UbatchLabel => _loc.T("server.ubatch_label") + " (-ub)";

	public string MaxTokensLabel => _loc.T("server.max_tokens_label") + " (-n)";

	public string SamplingTitle => _loc.T("server.sampling_title");

	public string TemperatureLabel => _loc.T("server.temperature_label") + " (--temp)";

	public string TopPLabel => _loc.T("server.top_p_label") + " (--top-p)";

	public string TopKLabel => _loc.T("server.top_k_label") + " (--top-k)";

	public string MinPLabel => _loc.T("server.min_p_label") + " (--min-p)";

	public string TypicalPLabel => _loc.T("server.typical_p_label") + " (--typical-p)";

	public string RepeatPenaltyLabel => _loc.T("server.repeat_penalty_label") + " (--repeat-penalty)";

	public string RepeatLastNLabel => _loc.T("server.repeat_last_n_label") + " (--repeat-last-n)";

	public string PresencePenaltyLabel => _loc.T("server.presence_penalty_label") + " (--presence-penalty)";

	public string FrequencyPenaltyLabel => _loc.T("server.frequency_penalty_label") + " (--frequency-penalty)";

	public string SeedLabel => _loc.T("server.seed_label") + " (-s)";

	public string MirostatSamplingCheck => _loc.T("server.mirostat_sampling") + " (--mirostat)";

	public string TauLabel => _loc.T("server.tau_label") + " (--mirostat-ent)";

	public string EtaLabel => _loc.T("server.eta_label") + " (--mirostat-lr)";

	public string MtpTitle => _loc.T("server.mtp_title");

	public string SpecTypeLabel => _loc.T("server.spec_type_label") + " (--spec-type)";

	public string TipSpecType => _loc.T("server.tip_spec_type");

	public bool CanPinDraftModelToLocal => string.Equals(SpecType?.Trim(), "draft", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(SpecType?.Trim(), "draft-simple", StringComparison.OrdinalIgnoreCase);

	public string PredictCountLabel => _loc.T("server.predict_count_label") + " (-n)";

	public string SpecDraftParamsTitle => _loc.T("server.spec_draft_params");

	public string DraftGpuLayersLabel => _loc.T("server.draft_gpu_layers_label") + " (-ngld)";

	public string DraftNMaxLabel => _loc.T("server.draft_n_max_label") + " (--spec-draft-n-max)";

	public string DraftNMinLabel => _loc.T("server.draft_n_min_label") + " (--spec-draft-n-min)";

	public string DraftPSplitLabel => _loc.T("server.draft_p_split_label") + " (--spec-draft-p-split)";

	public string DraftPMinLabel => _loc.T("server.draft_p_min_label") + " (--spec-draft-p-min)";

	public string YarnRopeTitle => _loc.T("server.yarn_rope_title");

	public string RopeScalingLabel => _loc.T("server.rope_scaling_label") + " (--rope-scaling)";

	public string YarnExtFactorLabel => _loc.T("server.yarn_ext_factor_label") + " (--yarn-ext-factor)";

	public string YarnAttnFactorLabel => _loc.T("server.yarn_attn_factor_label") + " (--yarn-attn-factor)";

	public string YarnBetaFastLabel => _loc.T("server.yarn_beta_fast_label") + " (--yarn-beta-fast)";

	public string YarnBetaSlowLabel => _loc.T("server.yarn_beta_slow_label") + " (--yarn-beta-slow)";

	public string RopeFreqBaseLabel => _loc.T("server.rope_freq_base_label") + " (--rope-freq-base)";

	public string RopeFreqScaleLabel => _loc.T("server.rope_freq_scale_label") + " (--rope-freq-scale)";

	public string YarnOrigCtxLabel => _loc.T("server.yarn_orig_ctx_label") + " (--yarn-orig-ctx)";

	public string AdvancedOptionsTitle => _loc.T("server.advanced_options_title");

	public string NumaCheck => _loc.T("server.numa") + " (--numa)";

	public string CachePromptCheck => _loc.T("server.cache_prompt") + " (--cache-prompt)";

	public string ContBatchingCheck => _loc.T("server.cont_batching") + " (-cb)";

	public string ContextShiftCheck => _loc.T("server.context_shift") + " (--context-shift)";

	public string VerboseLoggingCheck => _loc.T("server.verbose_logging") + " (-lv 4)";

	public string WebUICheck => _loc.T("server.web_ui") + " (--ui)";

	public string MetricsCheck => _loc.T("server.metrics") + " (--metrics)";

	public string ReasoningCheck => _loc.T("server.reasoning") + " (-rea)";

	public string EmbeddingModeCheck => _loc.T("server.embedding_mode") + " (--embedding)";

	public string PriorityHighCheck => _loc.T("server.priority_high") + " (process priority)";

	public string TipPriorityHigh => _loc.T("server.tip_priority_high");

	public string CustomArgsTitle => _loc.T("server.custom_args_title");

	public string CustomArgsWatermark => _loc.T("server.custom_args_placeholder");

	public string ExportTitle => _loc.T("server.export_title");

	public string ConnectionSettingsTitle => _loc.T("server.connection_settings");

	public string RpcTitle => _loc.T("server.rpc_title");

	public string RpcDescription => _loc.T("server.rpc_description");

	public string RpcSshTitle => _loc.T("server.rpc_ssh_title");

	public string RpcSshInstruction => _loc.T("server.rpc_ssh_instruction");

	public string RpcManagementTitle => _loc.T("server.rpc_management_title");

	public string RpcServerPathLabel => _loc.T("server.rpc_server_path");

	public string RpcServerPathInstruction => _loc.T("server.rpc_server_path_instruction");

	public string RpcSshStatus
	{
		get => _rpcSshStatus;
		private set => SetProperty(ref _rpcSshStatus, value);
	}

	public string RpcSshButtonLabel => _rpcSshConnected
		? _loc.T("server.rpc_ssh_connected_button")
		: _loc.T("server.rpc_ssh_connect_button");

	public IBrush RpcSshButtonBrush => _rpcSshConnected
		? ThemeBrushes.Get("ThemeSuccess", "#08A878")
		: ThemeBrushes.Get("ThemeDanger", "#E53935");

	public string RpcSshIndicator => _rpcSshConnected ? "✓" : "✕";

	public IBrush RpcSshIndicatorBrush => _rpcSshConnected ? Brushes.LimeGreen : Brushes.IndianRed;

	public string RpcEnabledLabel => _loc.T("server.rpc_enabled");

	public string RpcEndpointLabel => _loc.T("server.rpc_endpoint") + " (--rpc)";

	public string RpcHostLabel => _loc.T("server.rpc_host");

	public string RpcPortLabel => _loc.T("server.rpc_port");

	public string RpcStartLabel => _loc.T("server.rpc_start");

	public string RpcStopLabel => _loc.T("server.rpc_stop");

	public string RpcSshUsernameLabel => _loc.T("server.rpc_ssh_username");

	public string RpcSshPasswordLabel => _loc.T("server.rpc_ssh_password");

	public string RpcSavePasswordLabel => _loc.T("server.rpc_save_password");

	public string RpcUseCacheLabel => _loc.T("server.rpc_use_cache");

	public string RemoteTelemetryLabel => _loc.T("server.rpc_remote_telemetry");

	public bool RemoteTelemetryEnabled
	{
		get => _remoteTelemetryEnabled;
		set
		{
			if (SetProperty(ref _remoteTelemetryEnabled, value))
			{
				_lastRemoteTelemetryAt = DateTime.MinValue;
				AutoSaveProfile();
				_ = RefreshGpuAsync();
			}
		}
	}

	public string RpcCheckSshLabel => _loc.T("server.rpc_check_ssh");

	public string RpcShowPasswordLabel => _loc.T("server.rpc_show_password");

	public string RpcCheckConnectionLabel => _loc.T("server.rpc_check_connection");

	public string HostLabel => _loc.T("server.host_label") + " (--host)";

	public string PortLabel => _loc.T("server.port_label") + " (--port)";

	public string TimeoutLabel => _loc.T("server.timeout_label") + " (-to)";

	public string SlotsLabel => _loc.T("server.slots_label") + " (-np)";

	public string ProfileControlsTitle => _loc.T("server.profile_controls");

	public string ProfileLabel => _loc.T("server.profile_label");

	public string CmdLinePreviewTitle => _loc.T("server.cmdline_preview_title");

	public string UnsupportedFlagsTitle => _loc.T("server.unsupported_flags_title");

	public string UnsupportedFlagsHint => _loc.T("server.unsupported_flags_hint");

	public string TokensPerSecondText => string.Format(_loc.T("server.tps_fmt"), $"{TokensPerSecond:F1}");

	public string PromptTokensPerSecondText => string.Format(_loc.T("server.prompt_tps_fmt"), $"{PromptTokensPerSecond:F1}");

	public string MeasureSpeedBtnText => _loc.T("server.measure_speed_btn");

	public string CreateProfileBtnText => _loc.T("server.create_profile_btn");

	public string CopyFromProfileBtnText => _loc.T("server.copy_from_profile_btn");

	public string RenameProfileBtnText => _loc.T("server.rename_profile_btn");

	public string DeleteProfileBtnText => _loc.T("server.delete_profile_btn");

	public string CleanupMissingModelProfilesBtn => _loc.Language == "en" ? "Clean missing models" : "Очистить без моделей";

	public string SetDefaultProfileBtnText => _loc.T("server.set_default_profile_btn");

	public bool IsDefaultProfile => SelectedProfile?.IsDefault ?? false;

	public bool CanSetDefault
	{
		get
		{
			if (SelectedProfile != null)
			{
				return !IsDefaultProfile;
			}
			return false;
		}
	}

	public string ExportBatBtnText => _loc.T("server.export_bat_btn");

	public string CopyCmdLineBtnText => _loc.T("server.copy_cmdline_btn");

	public string ImportBatBtnText => _loc.T("server.import_bat_btn");

	public string CustomArgsDesc => _loc.T("server.custom_args_desc");

	public string CustomArgsNote => _loc.T("server.custom_args_note");

	public string AddArgsBtnText => _loc.T("server.add_args_btn");

	public string StartServerBtnText => _loc.T("server.start_server_btn");

	public string StopServerBtnText => _loc.T("server.stop_server_btn_text");

	public string TabKvCache => _loc.T("server.tab_kv_cache");

	public string TabMtpSpeculative => _loc.T("server.tab_speculative");

	public string TabRopeYarn => _loc.T("server.tab_rope_yarn");

	public string TabServer => _loc.T("server.tab_server");

	public string TabCustomArgs => _loc.T("server.tab_custom_args");

	public string TabRpc => _loc.T("server.tab_rpc");

	public string TabContext => _loc.T("server.tab_context");

	public string TabSampling => _loc.T("server.tab_sampling");

	public string ModelPathLabel => _loc.T("server.model_path_label");

	public string MmprojPathLabel => _loc.T("server.mmproj_path_label");

	public string DraftPathLabel => _loc.T("server.draft_path_label");

	public string GpuAccelerationTitle => _loc.T("server.gpu_acceleration");

	public string MultiGpuTitle => _loc.T("server.multi_gpu_title");

	public string TensorSplitLabel => _loc.T("server.tensor_split_label") + " (--tensor-split)";

	public string SamplingSectionTitle => _loc.T("server.sampling_section_title");

	public string MtpMultiTokenTitle => _loc.T("server.mtp_multi_token_title");

	public string KvCacheTypeTitle => _loc.T("server.kv_cache_type_title");

	public string MirostatDisabled => "Disabled (0)";

	public string MirostatMode1 => "Mode 1";

	public string MirostatMode2 => "Mode 2";

	public string TipModelPath => _loc.T("server.tip.model_path");

	public string TipMmprojPath => _loc.T("server.tip.mmproj");

	public string TipDraftModelPath => _loc.T("server.tip.draft_model_path");

	public string TipGpuLayers => _loc.T("server.tip.gpu_layers");

	public string TipThreads => _loc.T("server.tip.threads");

	public string TipThreadsBatch => _loc.T("server.tip.threads_batch");

	public string TipMainGpu => _loc.T("server.tip.main_gpu");

	public string TipFlashAttention => _loc.T("server.tip.flash_attention");

	public string TipMmap => _loc.T("server.tip.mmap");

	public string TipMlock => _loc.T("server.tip.mlock");

	public string TipMmq => _loc.T("server.tip.mmq");

	public string TipKvOffload => _loc.T("server.tip.kv_offload");

	public string TipContextSize => _loc.T("server.tip.ctx_size");

	public string TipBatchSize => _loc.T("server.tip.batch_size");

	public string TipUBatchSize => _loc.T("server.tip.ubatch_size");

	public string TipMaxTokens => _loc.T("server.tip.predict");

	public string TipTemperature => _loc.T("server.tip.temperature");

	public string TipTopP => _loc.T("server.tip.top_p");

	public string TipTopK => _loc.T("server.tip.top_k");

	public string TipMinP => _loc.T("server.tip.min_p");

	public string TipTypicalP => _loc.T("server.tip.typical_p");

	public string TipRepeatPenalty => _loc.T("server.tip.repeat_penalty");

	public string TipRepeatLastN => _loc.T("server.tip.repeat_last_n");

	public string TipPresencePenalty => _loc.T("server.tip.presence_penalty");

	public string TipFrequencyPenalty => _loc.T("server.tip.frequency_penalty");

	public string TipSeed => _loc.T("server.tip.seed");

	public string TipMirostat => _loc.T("server.tip.mirostat");

	public string TipTau => _loc.T("server.tip.mirostat_tau");

	public string TipEta => _loc.T("server.tip.mirostat_eta");

	public string TipCacheTypeK => _loc.T("server.tip.cache_type_k");

	public string TipCacheTypeV => _loc.T("server.tip.cache_type_v");

	public string TipCacheRam => _loc.T("server.tip.cache_ram");

	public string TipCacheReuse => _loc.T("server.tip.cache_reuse");

	public string TipPredictCount => _loc.T("server.tip.predict_count");

	public string TipSpecDraftGpuLayers => _loc.T("server.tip.spec_draft_gpu_layers");

	public string TipSpecDraftNMax => _loc.T("server.tip.spec_draft_n_max");

	public string TipSpecDraftNMin => _loc.T("server.tip.spec_draft_n_min");

	public string TipSpecDraftPSplit => _loc.T("server.tip.spec_draft_p_split");

	public string TipSpecDraftPMin => _loc.T("server.tip.spec_draft_p_min");

	public string TipRopeFreqBase => _loc.T("server.tip.rope_freq_base");

	public string TipRopeFreqScale => _loc.T("server.tip.rope_freq_scale");

	public string TipYarnOriginalContext => _loc.T("server.tip.yarn_orig_ctx");

	public string TipNuma => _loc.T("server.tip.numa");

	public string TipCachePrompt => _loc.T("server.tip.cache_prompt");

	public string TipContBatching => _loc.T("server.tip.cont_batching");

	public string TipContextShift => _loc.T("server.tip.context_shift");

	public string TipVerboseLogging => _loc.T("server.tip.verbose_logging");

	public string TipWebUI => _loc.T("server.tip.webui");

	public string TipMetrics => _loc.T("server.tip.metrics");

	public string TipReasoning => _loc.T("server.tip.reasoning");

	public string TipEmbeddingMode => _loc.T("server.tip.embedding_mode");

	public string TipHost => _loc.T("server.tip.host");

	public string TipPort => _loc.T("server.tip.port");

	public string TipTimeout => _loc.T("server.tip.timeout");

	public string TipSlots => _loc.T("server.tip.parallel");

	public string TipCustomArgs => _loc.T("server.tip.additional_args");

	public string TipTensorSplit => _loc.T("server.tip.tensor_split");

	public string TipSpeculativeDecoding => _loc.T("server.tip.speculative_decoding");

	public string TipRopeScaling => _loc.T("server.tip.rope_scaling");

	public string TipYarnExtFactor => _loc.T("server.tip.yarn_ext_factor");

	public string TipYarnAttnFactor => _loc.T("server.tip.yarn_attn_factor");

	public string TipYarnBetaFast => _loc.T("server.tip.yarn_beta_fast");

	public string TipYarnBetaSlow => _loc.T("server.tip.yarn_beta_slow");

	public string TipEnableSlots => _loc.T("server.tip.slots_monitoring");

	public string TipChipRemove => _loc.T("server.tip.chip_remove");

	public string RamUsedText
	{
		get
		{
			string ramText;
			if (RamUsedGb > 0.0)
			{
				ramText = $"{RamUsedGb:F1} / {s_totalRamGb:F1} GB";
				if (RamCommittedGb > 0.0)
				{
					ramText += $" · выделено: {RamCommittedGb:F1} GB";
				}
			}
			else
			{
				ramText = $"Сервер не запущен / {s_totalRamGb:F1} GB";
			}
			if (CacheRam > 0)
			{
				ramText += $" · кэш: до {CacheRam / 1024.0:F1} GB";
			}
			return ramText;
		}
	}

	public ObservableCollection<RecommendedModel> RecommendedModels { get; } = new ObservableCollection<RecommendedModel>();

	public string LlamaCppDirectoryDisplay => _settings.LlamaCppDirectory;

	public int MirostatModeInt
	{
		get
		{
			return (int)MirostatMode;
		}
		set
		{
			MirostatMode = (MirostatMode)value;
		}
	}

	public IEnumerable<string> SpecTypeOptions
	{
		get
		{
			var values = new List<string> { "none", "draft", "draft-simple", "draft-eagle3", "draft-mtp", "draft-dflash" };
			string raw = _helpInfo?.RawOutput ?? string.Empty;
			Match match = Regex.Match(raw, @"--spec-type\s+([^\r\n]+)", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				foreach (string specOption in match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					string token = specOption.Trim();
					if (Regex.IsMatch(token, "^[a-z][a-z0-9-]*$", RegexOptions.IgnoreCase) && !values.Contains(token, StringComparer.OrdinalIgnoreCase))
						values.Add(token);
				}
			}
			return values;
		}
	}

	public ObservableCollection<CustomArgChip> CustomArgChips { get; } = new ObservableCollection<CustomArgChip>();

	public RelayCommand<string> RemoveCommand => new RelayCommand<string>(RemoveChip);

	public RelayCommand AddCustomArgCommand => new RelayCommand(AddCustomArg);

	public RelayCommand RefreshChipsCommand => new RelayCommand(delegate
	{
		UpdateChipsFromAdditionalArgs();
	});

	public List<string> UnsupportedFlags { get; private set; } = new List<string>();

	public string ThreadsText
	{
		get
		{
			return Threads.ToString();
		}
		set
		{
			Threads = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string ThreadsBatchText
	{
		get
		{
			return ThreadsBatch.ToString();
		}
		set
		{
			ThreadsBatch = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string MainGpuText
	{
		get
		{
			return MainGpu.ToString();
		}
		set
		{
			MainGpu = (int.TryParse(value, out var result) ? result : 0);
		}
	}

	public string CacheRamText
	{
		get
		{
			return CacheRam.ToString();
		}
		set
		{
			CacheRam = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string CacheReuseText
	{
		get
		{
			return CacheReuse.ToString();
		}
		set
		{
			CacheReuse = (int.TryParse(value, out var result) ? result : 0);
		}
	}

	public string ContextSizeText
	{
		get
		{
			return ContextSize.ToString();
		}
		set
		{
			ContextSize = (int.TryParse(value, out var result) ? result : 0);
		}
	}

	public string BatchSizeText
	{
		get
		{
			return BatchSize.ToString();
		}
		set
		{
			BatchSize = (int.TryParse(value, out var result) ? result : 2048);
		}
	}

	public string UbatchSizeText
	{
		get
		{
			return UbatchSize.ToString();
		}
		set
		{
			UbatchSize = (int.TryParse(value, out var result) ? result : 512);
		}
	}

	public string MaxTokensText
	{
		get
		{
			return MaxTokens.ToString();
		}
		set
		{
			MaxTokens = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string TemperatureText
	{
		get
		{
			return Temperature.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			Temperature = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.8f);
		}
	}

	public string TopPText
	{
		get
		{
			return TopP.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			TopP = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.95f);
		}
	}

	public string TopKText
	{
		get
		{
			return TopK.ToString();
		}
		set
		{
			TopK = (int.TryParse(value, out var result) ? result : 40);
		}
	}

	public string MinPText
	{
		get
		{
			return MinP.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			MinP = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.05f);
		}
	}

	public string TypicalPText
	{
		get
		{
			return TypicalP.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			TypicalP = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 1f);
		}
	}

	public string RepeatPenaltyText
	{
		get
		{
			return RepeatPenalty.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			RepeatPenalty = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 1.1f);
		}
	}

	public string RepeatLastNText
	{
		get
		{
			return RepeatLastN.ToString();
		}
		set
		{
			RepeatLastN = (int.TryParse(value, out var result) ? result : 64);
		}
	}

	public string PresencePenaltyText
	{
		get
		{
			return PresencePenalty.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			PresencePenalty = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f);
		}
	}

	public string FrequencyPenaltyText
	{
		get
		{
			return FrequencyPenalty.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			FrequencyPenalty = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f);
		}
	}

	public string SeedText
	{
		get
		{
			return Seed.ToString();
		}
		set
		{
			Seed = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string MirostatTauText
	{
		get
		{
			return MirostatTau.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			MirostatTau = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 5f);
		}
	}

	public string MirostatEtaText
	{
		get
		{
			return MirostatEta.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			MirostatEta = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.1f);
		}
	}

	public string PredictCountText
	{
		get
		{
			return PredictCount.ToString();
		}
		set
		{
			PredictCount = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string SpecDraftNMaxText
	{
		get
		{
			return SpecDraftNMax.ToString();
		}
		set
		{
			SpecDraftNMax = (int.TryParse(value, out var result) ? result : 3);
		}
	}

	public string SpecDraftNMinText
	{
		get
		{
			return SpecDraftNMin.ToString();
		}
		set
		{
			SpecDraftNMin = (int.TryParse(value, out var result) ? result : 0);
		}
	}

	public string SpecDraftPSplitText
	{
		get
		{
			return SpecDraftPSplit.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			SpecDraftPSplit = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.1f);
		}
	}

	public string SpecDraftPMinText
	{
		get
		{
			return SpecDraftPMin.ToString("0.00", CultureInfo.InvariantCulture);
		}
		set
		{
			SpecDraftPMin = (float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f);
		}
	}

	public string PortText
	{
		get
		{
			return Port.ToString();
		}
		set
		{
			Port = (int.TryParse(value, out var result) ? result : 8080);
		}
	}

	public string TimeoutText
	{
		get
		{
			return Timeout.ToString();
		}
		set
		{
			Timeout = (int.TryParse(value, out var result) ? result : 600);
		}
	}

	public string SlotsText
	{
		get
		{
			return Slots.ToString();
		}
		set
		{
			Slots = (int.TryParse(value, out var result) ? result : (-1));
		}
	}

	public string RopeFreqBaseText
	{
		get
		{
			return RopeFreqBase?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			RopeFreqBase = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string RopeFreqScaleText
	{
		get
		{
			return RopeFreqScale?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			RopeFreqScale = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string YarnOriginalContextText
	{
		get
		{
			return YarnOriginalContext?.ToString() ?? "";
		}
		set
		{
			YarnOriginalContext = (int.TryParse(value, out var result) ? new int?(result) : ((int?)null));
		}
	}

	public string YarnExtFactorText
	{
		get
		{
			return YarnExtFactor?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			YarnExtFactor = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string YarnAttnFactorText
	{
		get
		{
			return YarnAttnFactor?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			YarnAttnFactor = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string YarnBetaFastText
	{
		get
		{
			return YarnBetaFast?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			YarnBetaFast = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string YarnBetaSlowText
	{
		get
		{
			return YarnBetaSlow?.ToString(CultureInfo.InvariantCulture) ?? "";
		}
		set
		{
			YarnBetaSlow = (double.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? new double?(result) : ((double?)null));
		}
	}

	public string FlashAttentionLabel => FlashAttentionCheck;

	public string MemoryMapLabel => MemoryMapCheck;

	public string MlockLabel => MlockCheck;

	public string MmqLabel => MmqCheck;

	public string KvOffloadLabel => KvOffloadCheck;

	public string NumaLabel => NumaCheck;

	public string CachePromptLabel => CachePromptCheck;

	public string ContBatchingLabel => ContBatchingCheck;

	public string ContextShiftLabel => ContextShiftCheck;

	public string VerboseLoggingLabel => VerboseLoggingCheck;

	public string WebUILabel => WebUICheck;

	public string MetricsLabel => MetricsCheck;

	public string ReasoningLabel => ReasoningCheck;

	public string EmbeddingModeLabel => EmbeddingModeCheck;

	public string PriorityHighLabel => PriorityHighCheck;

	public Task? ProfileRestartTask => _profileRestartTask;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (!EqualityComparer<ServerState>.Default.Equals(_state, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.State);
				_state = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.State);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double VramUsedGb
	{
		get
		{
			return _vramUsedGb;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_vramUsedGb, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.VramUsedGb);
				_vramUsedGb = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.VramUsedGb);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double RamUsedGb
	{
		get
		{
			return _ramUsedGb;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_ramUsedGb, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RamUsedGb);
				_ramUsedGb = value;
				OnRamUsedGbChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RamUsedGb);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double TokensPerSecond
	{
		get
		{
			return _tokensPerSecond;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_tokensPerSecond, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TokensPerSecond);
				_tokensPerSecond = value;
				OnTokensPerSecondChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TokensPerSecond);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double PromptTokensPerSecond
	{
		get
		{
			return _promptTokensPerSecond;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_promptTokensPerSecond, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PromptTokensPerSecond);
				_promptTokensPerSecond = value;
				OnPromptTokensPerSecondChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PromptTokensPerSecond);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double GpuVramUsedGb
	{
		get
		{
			return _gpuVramUsedGb;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_gpuVramUsedGb, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GpuVramUsedGb);
				_gpuVramUsedGb = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GpuVramUsedGb);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int QueueSize
	{
		get
		{
			return _queueSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_queueSize, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.QueueSize);
				_queueSize = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.QueueSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ActiveSlots
	{
		get
		{
			return _activeSlots;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_activeSlots, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ActiveSlots);
				_activeSlots = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ActiveSlots);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public TimeSpan Uptime
	{
		get
		{
			return _uptime;
		}
		set
		{
			if (!EqualityComparer<TimeSpan>.Default.Equals(_uptime, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Uptime);
				_uptime = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Uptime);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProcessId
	{
		get
		{
			return _processId;
		}
		[MemberNotNull("_processId")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_processId, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ProcessId);
				_processId = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ProcessId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		[MemberNotNull("_errorMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_errorMessage, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ErrorMessage);
				_errorMessage = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ErrorMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ServerProfile> Profiles
	{
		get
		{
			return _profiles;
		}
		[MemberNotNull("_profiles")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ServerProfile>>.Default.Equals(_profiles, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Profiles);
				_profiles = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Profiles);
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
	public string CommandLinePreview
	{
		get
		{
			return _commandLinePreview;
		}
		[MemberNotNull("_commandLinePreview")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_commandLinePreview, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CommandLinePreview);
				_commandLinePreview = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CommandLinePreview);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelPath
	{
		get
		{
			return _modelPath;
		}
		[MemberNotNull("_modelPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelPath);
				_modelPath = value;
				ApplyMuseDflashRecommendation();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MmprojPath
	{
		get
		{
			return _mmprojPath;
		}
		[MemberNotNull("_mmprojPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mmprojPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MmprojPath);
				_mmprojPath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MmprojPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DraftModelPath
	{
		get
		{
			return _draftModelPath;
		}
		[MemberNotNull("_draftModelPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_draftModelPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DraftModelPath);
				_draftModelPath = value;
				ApplyMuseDflashRecommendation();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DraftModelPath);
				// r93: a draft model path without the master toggle on produces no
				// -md / --spec-* flags in the command line — turn it on automatically.
				if (!_isInitializing && !_isApplyingProfile && !SpeculativeDecoding
					&& !string.IsNullOrWhiteSpace(value))
				{
					SpeculativeDecoding = true;
				}
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelDisplayName
	{
		get
		{
			return _modelDisplayName;
		}
		[MemberNotNull("_modelDisplayName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelDisplayName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelDisplayName);
				_modelDisplayName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelDisplayName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MmprojDisplayName
	{
		get
		{
			return _mmprojDisplayName;
		}
		[MemberNotNull("_mmprojDisplayName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mmprojDisplayName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MmprojDisplayName);
				_mmprojDisplayName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MmprojDisplayName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DraftDisplayName
	{
		get
		{
			return _draftDisplayName;
		}
		[MemberNotNull("_draftDisplayName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_draftDisplayName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DraftDisplayName);
				_draftDisplayName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DraftDisplayName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfRepo
	{
		get
		{
			return _hfRepo;
		}
		[MemberNotNull("_hfRepo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfRepo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfRepo);
				_hfRepo = value;
				OnHfRepoChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfRepo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfFile
	{
		get
		{
			return _hfFile;
		}
		[MemberNotNull("_hfFile")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfFile, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfFile);
				_hfFile = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfFile);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HfOffline
	{
		get
		{
			return _hfOffline;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hfOffline, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfOffline);
				_hfOffline = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfOffline);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfRepoDraft
	{
		get
		{
			return _hfRepoDraft;
		}
		[MemberNotNull("_hfRepoDraft")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfRepoDraft, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfRepoDraft);
				_hfRepoDraft = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfRepoDraft);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDownloadingHf
	{
		get
		{
			return _isDownloadingHf;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDownloadingHf, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsDownloadingHf);
				_isDownloadingHf = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsDownloadingHf);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double HfDownloadProgress
	{
		get
		{
			return _hfDownloadProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_hfDownloadProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfDownloadProgress);
				_hfDownloadProgress = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfDownloadProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfDownloadStatus
	{
		get
		{
			return _hfDownloadStatus;
		}
		[MemberNotNull("_hfDownloadStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfDownloadStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfDownloadStatus);
				_hfDownloadStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfDownloadStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfDownloadDirectory
	{
		get
		{
			return _hfDownloadDirectory;
		}
		[MemberNotNull("_hfDownloadDirectory")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfDownloadDirectory, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfDownloadDirectory);
				_hfDownloadDirectory = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfDownloadDirectory);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelDescription
	{
		get
		{
			return _modelDescription;
		}
		[MemberNotNull("_modelDescription")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelDescription, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelDescription);
				_modelDescription = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelDescription);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelSizeInfo
	{
		get
		{
			return _modelSizeInfo;
		}
		[MemberNotNull("_modelSizeInfo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelSizeInfo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelSizeInfo);
				_modelSizeInfo = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelSizeInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelParamsInfo
	{
		get
		{
			return _modelParamsInfo;
		}
		[MemberNotNull("_modelParamsInfo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelParamsInfo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelParamsInfo);
				_modelParamsInfo = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelParamsInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return _host;
		}
		[MemberNotNull("_host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_host, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Host);
				_host = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_port, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Port);
				_port = value;
				OnPortChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_timeout, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Timeout);
				_timeout = value;
				OnTimeoutChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Timeout);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Slots
	{
		get
		{
			return _slots;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_slots, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Slots);
				_slots = value;
				OnSlotsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Slots);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string GpuLayers
	{
		get
		{
			return _gpuLayers;
		}
		[MemberNotNull("_gpuLayers")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_gpuLayers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GpuLayers);
				_gpuLayers = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GpuLayers);
				OnPropertyChanged(nameof(GpuLayersLabel));
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Threads
	{
		get
		{
			return _threads;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_threads, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Threads);
				_threads = value;
				OnThreadsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Threads);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ThreadsBatch
	{
		get
		{
			return _threadsBatch;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_threadsBatch, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ThreadsBatch);
				_threadsBatch = value;
				OnThreadsBatchChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ThreadsBatch);
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
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FlashAttention);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Mmap
	{
		get
		{
			return _mmap;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mmap, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Mmap);
				_mmap = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Mmap);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Mlock
	{
		get
		{
			return _mlock;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mlock, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Mlock);
				_mlock = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Mlock);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MmqEnabled
	{
		get
		{
			return _mmqEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mmqEnabled, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MmqEnabled);
				_mmqEnabled = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MmqEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool KvOffloadEnabled
	{
		get
		{
			return _kvOffloadEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_kvOffloadEnabled, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.KvOffloadEnabled);
				_kvOffloadEnabled = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.KvOffloadEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int MainGpu
	{
		get
		{
			return _mainGpu;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_mainGpu, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MainGpu);
				_mainGpu = value;
				OnMainGpuChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MainGpu);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public GpuSplitMode GpuSplitMode
	{
		get
		{
			return _gpuSplitMode;
		}
		set
		{
			if (!EqualityComparer<GpuSplitMode>.Default.Equals(_gpuSplitMode, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GpuSplitMode);
				_gpuSplitMode = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GpuSplitMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string TensorSplit
	{
		get
		{
			return _tensorSplit;
		}
		[MemberNotNull("_tensorSplit")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_tensorSplit, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TensorSplit);
				_tensorSplit = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TensorSplit);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ContextSize
	{
		get
		{
			return _contextSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_contextSize, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContextSize);
				_contextSize = value;
				OnContextSizeChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContextSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int BatchSize
	{
		get
		{
			return _batchSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_batchSize, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.BatchSize);
				_batchSize = value;
				OnBatchSizeChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.BatchSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int UbatchSize
	{
		get
		{
			return _ubatchSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_ubatchSize, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UbatchSize);
				_ubatchSize = value;
				OnUbatchSizeChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UbatchSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int MaxTokens
	{
		get
		{
			return _maxTokens;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_maxTokens, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MaxTokens);
				_maxTokens = value;
				OnMaxTokensChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MaxTokens);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CacheTypeK
	{
		get
		{
			return _cacheTypeK;
		}
		[MemberNotNull("_cacheTypeK")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cacheTypeK, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CacheTypeK);
				_cacheTypeK = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CacheTypeK);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CacheTypeV
	{
		get
		{
			return _cacheTypeV;
		}
		[MemberNotNull("_cacheTypeV")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cacheTypeV, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CacheTypeV);
				_cacheTypeV = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CacheTypeV);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int CacheRam
	{
		get
		{
			return _cacheRam;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_cacheRam, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CacheRam);
				_cacheRam = value;
				OnCacheRamChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CacheRam);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int CacheReuse
	{
		get
		{
			return _cacheReuse;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_cacheReuse, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CacheReuse);
				_cacheReuse = value;
				OnCacheReuseChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CacheReuse);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float Temperature
	{
		get
		{
			return _temperature;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_temperature, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Temperature);
				_temperature = value;
				OnTemperatureChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Temperature);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TopK
	{
		get
		{
			return _topK;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_topK, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TopK);
				_topK = value;
				OnTopKChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TopK);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float TopP
	{
		get
		{
			return _topP;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_topP, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TopP);
				_topP = value;
				OnTopPChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TopP);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float MinP
	{
		get
		{
			return _minP;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_minP, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MinP);
				_minP = value;
				OnMinPChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MinP);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float TypicalP
	{
		get
		{
			return _typicalP;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_typicalP, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TypicalP);
				_typicalP = value;
				OnTypicalPChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TypicalP);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float RepeatPenalty
	{
		get
		{
			return _repeatPenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_repeatPenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RepeatPenalty);
				_repeatPenalty = value;
				OnRepeatPenaltyChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RepeatPenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int RepeatLastN
	{
		get
		{
			return _repeatLastN;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_repeatLastN, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RepeatLastN);
				_repeatLastN = value;
				OnRepeatLastNChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RepeatLastN);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float PresencePenalty
	{
		get
		{
			return _presencePenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_presencePenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PresencePenalty);
				_presencePenalty = value;
				OnPresencePenaltyChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PresencePenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float FrequencyPenalty
	{
		get
		{
			return _frequencyPenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_frequencyPenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FrequencyPenalty);
				_frequencyPenalty = value;
				OnFrequencyPenaltyChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FrequencyPenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Seed
	{
		get
		{
			return _seed;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_seed, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Seed);
				_seed = value;
				OnSeedChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Seed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public MirostatMode MirostatMode
	{
		get
		{
			return _mirostatMode;
		}
		set
		{
			if (!EqualityComparer<MirostatMode>.Default.Equals(_mirostatMode, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MirostatMode);
				_mirostatMode = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MirostatMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float MirostatTau
	{
		get
		{
			return _mirostatTau;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_mirostatTau, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MirostatTau);
				_mirostatTau = value;
				OnMirostatTauChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MirostatTau);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float MirostatEta
	{
		get
		{
			return _mirostatEta;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_mirostatEta, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MirostatEta);
				_mirostatEta = value;
				OnMirostatEtaChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MirostatEta);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float DryMultiplier
	{
		get
		{
			return _dryMultiplier;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_dryMultiplier, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DryMultiplier);
				_dryMultiplier = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DryMultiplier);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float DryBase
	{
		get
		{
			return _dryBase;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_dryBase, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DryBase);
				_dryBase = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DryBase);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float DynatempStddev
	{
		get
		{
			return _dynatempStddev;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_dynatempStddev, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DynatempStddev);
				_dynatempStddev = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DynatempStddev);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float XtcProbability
	{
		get
		{
			return _xtcProbability;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_xtcProbability, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.XtcProbability);
				_xtcProbability = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.XtcProbability);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float XtcThreshold
	{
		get
		{
			return _xtcThreshold;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_xtcThreshold, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.XtcThreshold);
				_xtcThreshold = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.XtcThreshold);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SpecType
	{
		get
		{
			return _specType;
		}
		[MemberNotNull("_specType")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_specType, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecType);
				_specType = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecType);
				OnPropertyChanged(nameof(CanPinDraftModelToLocal));
				// r93: picking a speculative type implies the user wants speculative
				// decoding on — otherwise the command line silently drops --spec-type,
				// -md and all draft parameters (EffectiveSpecType filters every
				// draft-* type out while SpeculativeDecoding is off).
				if (!_isInitializing && !_isApplyingProfile && !SpeculativeDecoding
					&& !string.IsNullOrWhiteSpace(value)
					&& !value.Equals("none", StringComparison.OrdinalIgnoreCase))
				{
					SpeculativeDecoding = true;
				}
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int PredictCount
	{
		get
		{
			return _predictCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_predictCount, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PredictCount);
				_predictCount = value;
				OnPredictCountChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PredictCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool SpeculativeDecoding
	{
		get
		{
			return _speculativeDecoding;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_speculativeDecoding, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpeculativeDecoding);
				_speculativeDecoding = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpeculativeDecoding);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SpecDraftGpuLayers
	{
		get
		{
			return _specDraftGpuLayers;
		}
		[MemberNotNull("_specDraftGpuLayers")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_specDraftGpuLayers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecDraftGpuLayers);
				_specDraftGpuLayers = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecDraftGpuLayers);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int SpecDraftNMax
	{
		get
		{
			return _specDraftNMax;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_specDraftNMax, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecDraftNMax);
				_specDraftNMax = value;
				OnSpecDraftNMaxChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecDraftNMax);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int SpecDraftNMin
	{
		get
		{
			return _specDraftNMin;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_specDraftNMin, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecDraftNMin);
				_specDraftNMin = value;
				OnSpecDraftNMinChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecDraftNMin);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float SpecDraftPSplit
	{
		get
		{
			return _specDraftPSplit;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_specDraftPSplit, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecDraftPSplit);
				_specDraftPSplit = value;
				OnSpecDraftPSplitChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecDraftPSplit);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float SpecDraftPMin
	{
		get
		{
			return _specDraftPMin;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_specDraftPMin, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SpecDraftPMin);
				_specDraftPMin = value;
				OnSpecDraftPMinChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SpecDraftPMin);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PriorityHigh
	{
		get
		{
			return _priorityHigh;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_priorityHigh, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PriorityHigh);
				_priorityHigh = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PriorityHigh);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EmbeddingMode
	{
		get
		{
			return _embeddingMode;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_embeddingMode, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.EmbeddingMode);
				_embeddingMode = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.EmbeddingMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PoolingType
	{
		get
		{
			return _poolingType;
		}
		[MemberNotNull("_poolingType")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_poolingType, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PoolingType);
				_poolingType = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PoolingType);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Numa
	{
		get
		{
			return _numa;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_numa, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Numa);
				_numa = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Numa);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProcessPriority
	{
		get
		{
			return _processPriority;
		}
		[MemberNotNull("_processPriority")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_processPriority, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ProcessPriority);
				_processPriority = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ProcessPriority);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CachePrompt
	{
		get
		{
			return _cachePrompt;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cachePrompt, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CachePrompt);
				_cachePrompt = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CachePrompt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ContBatching
	{
		get
		{
			return _contBatching;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_contBatching, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContBatching);
				_contBatching = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContBatching);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ContextShift
	{
		get
		{
			return _contextShift;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_contextShift, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContextShift);
				_contextShift = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContextShift);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool VerboseLogging
	{
		get
		{
			return _verboseLogging;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_verboseLogging, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.VerboseLogging);
				_verboseLogging = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.VerboseLogging);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EnableWebUI
	{
		get
		{
			return _enableWebUI;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_enableWebUI, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.EnableWebUI);
				_enableWebUI = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.EnableWebUI);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EnableSlots
	{
		get
		{
			return _enableSlots;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_enableSlots, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.EnableSlots);
				_enableSlots = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.EnableSlots);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EnableMetrics
	{
		get
		{
			return _enableMetrics;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_enableMetrics, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.EnableMetrics);
				_enableMetrics = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.EnableMetrics);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Reasoning
	{
		get
		{
			return _reasoning;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_reasoning, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Reasoning);
				_reasoning = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Reasoning);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ReasoningBudget
	{
		get
		{
			return _reasoningBudget;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_reasoningBudget, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ReasoningBudget);
				_reasoningBudget = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ReasoningBudget);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string RopeScaling
	{
		get
		{
			return _ropeScaling;
		}
		[MemberNotNull("_ropeScaling")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_ropeScaling, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RopeScaling);
				_ropeScaling = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RopeScaling);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? RopeFreqBase
	{
		get
		{
			return _ropeFreqBase;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_ropeFreqBase, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RopeFreqBase);
				_ropeFreqBase = value;
				OnRopeFreqBaseChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RopeFreqBase);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? RopeFreqScale
	{
		get
		{
			return _ropeFreqScale;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_ropeFreqScale, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RopeFreqScale);
				_ropeFreqScale = value;
				OnRopeFreqScaleChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RopeFreqScale);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int? YarnOriginalContext
	{
		get
		{
			return _yarnOriginalContext;
		}
		set
		{
			if (!EqualityComparer<int?>.Default.Equals(_yarnOriginalContext, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.YarnOriginalContext);
				_yarnOriginalContext = value;
				OnYarnOriginalContextChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.YarnOriginalContext);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? YarnExtFactor
	{
		get
		{
			return _yarnExtFactor;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_yarnExtFactor, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.YarnExtFactor);
				_yarnExtFactor = value;
				OnYarnExtFactorChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.YarnExtFactor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? YarnAttnFactor
	{
		get
		{
			return _yarnAttnFactor;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_yarnAttnFactor, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.YarnAttnFactor);
				_yarnAttnFactor = value;
				OnYarnAttnFactorChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.YarnAttnFactor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? YarnBetaFast
	{
		get
		{
			return _yarnBetaFast;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_yarnBetaFast, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.YarnBetaFast);
				_yarnBetaFast = value;
				OnYarnBetaFastChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.YarnBetaFast);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double? YarnBetaSlow
	{
		get
		{
			return _yarnBetaSlow;
		}
		set
		{
			if (!EqualityComparer<double?>.Default.Equals(_yarnBetaSlow, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.YarnBetaSlow);
				_yarnBetaSlow = value;
				OnYarnBetaSlowChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.YarnBetaSlow);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ApiKey
	{
		get
		{
			return _apiKey;
		}
		[MemberNotNull("_apiKey")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_apiKey, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ApiKey);
				_apiKey = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ApiKey);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Alias
	{
		get
		{
			return _alias;
		}
		[MemberNotNull("_alias")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_alias, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Alias);
				_alias = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Alias);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string AdditionalArgs
	{
		get
		{
			return _additionalArgs;
		}
		[MemberNotNull("_additionalArgs")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_additionalArgs, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AdditionalArgs);
				_additionalArgs = value;
				OnAdditionalArgsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AdditionalArgs);
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
	public string ExecutablePath
	{
		get
		{
			return _executablePath;
		}
		[MemberNotNull("_executablePath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_executablePath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ExecutablePath);
				_executablePath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ExecutablePath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ExecutableFound
	{
		get
		{
			return _executableFound;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_executableFound, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ExecutableFound);
				_executableFound = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ExecutableFound);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LlamaVersionInfo
	{
		get
		{
			return _llamaVersionInfo;
		}
		[MemberNotNull("_llamaVersionInfo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_llamaVersionInfo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LlamaVersionInfo);
				_llamaVersionInfo = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LlamaVersionInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<string> AvailableServers
	{
		get
		{
			return _availableServers;
		}
		[MemberNotNull("_availableServers")]
		set
		{
			if (!EqualityComparer<ObservableCollection<string>>.Default.Equals(_availableServers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AvailableServers);
				_availableServers = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AvailableServers);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedServer
	{
		get
		{
			return _selectedServer;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedServer, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedServer);
				_selectedServer = value;
				OnSelectedServerChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedServer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CudaInfo
	{
		get
		{
			return _cudaInfo;
		}
		[MemberNotNull("_cudaInfo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cudaInfo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CudaInfo);
				_cudaInfo = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CudaInfo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ValidationReport? ValidationReport
	{
		get
		{
			return _validationReport;
		}
		set
		{
			if (!EqualityComparer<LlamaStudio.Core.Models.ValidationReport>.Default.Equals(_validationReport, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ValidationReport);
				_validationReport = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ValidationReport);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public CliChangeReport? CliChangeReport
	{
		get
		{
			return _cliChangeReport;
		}
		set
		{
			if (!EqualityComparer<LlamaStudio.Core.Models.CliChangeReport>.Default.Equals(_cliChangeReport, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CliChangeReport);
				_cliChangeReport = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CliChangeReport);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsCliValidating
	{
		get
		{
			return _isCliValidating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCliValidating, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCliValidating);
				_isCliValidating = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCliValidating);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ValidationSummary
	{
		get
		{
			return _validationSummary;
		}
		[MemberNotNull("_validationSummary")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_validationSummary, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ValidationSummary);
				_validationSummary = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ValidationSummary);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ValidationDetails
	{
		get
		{
			return _validationDetails;
		}
		[MemberNotNull("_validationDetails")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_validationDetails, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ValidationDetails);
				_validationDetails = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ValidationDetails);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public SolidColorBrush ValidationSummaryForeground
	{
		get
		{
			return _validationSummaryForeground;
		}
		[MemberNotNull("_validationSummaryForeground")]
		set
		{
			if (!EqualityComparer<SolidColorBrush>.Default.Equals(_validationSummaryForeground, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ValidationSummaryForeground);
				_validationSummaryForeground = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ValidationSummaryForeground);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<GgufModelInfo> ScannedModels
	{
		get
		{
			return _scannedModels;
		}
		[MemberNotNull("_scannedModels")]
		set
		{
			if (!EqualityComparer<ObservableCollection<GgufModelInfo>>.Default.Equals(_scannedModels, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ScannedModels);
				_scannedModels = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ScannedModels);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public GgufModelInfo? QuickSelectedModel
	{
		get
		{
			return _quickSelectedModel;
		}
		set
		{
			if (!EqualityComparer<GgufModelInfo>.Default.Equals(_quickSelectedModel, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.QuickSelectedModel);
				_quickSelectedModel = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.QuickSelectedModel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsScanningModels
	{
		get
		{
			return _isScanningModels;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isScanningModels, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsScanningModels);
				_isScanningModels = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsScanningModels);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<RecommendedModel> SelectRecommendedModelCommand => selectRecommendedModelCommand ?? (selectRecommendedModelCommand = new RelayCommand<RecommendedModel>(SelectRecommendedModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DownloadHfModelCommand => downloadHfModelCommand ?? (downloadHfModelCommand = new AsyncRelayCommand(DownloadHfModelAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand BrowseModelCommand => browseModelCommand ?? (browseModelCommand = new AsyncRelayCommand(BrowseModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand BrowseMmprojCommand => browseMmprojCommand ?? (browseMmprojCommand = new AsyncRelayCommand(BrowseMmproj));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand BrowseDraftModelCommand => browseDraftModelCommand ?? (browseDraftModelCommand = new AsyncRelayCommand(BrowseDraftModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand QuickScanModelsCommand => quickScanModelsCommand ?? (quickScanModelsCommand = new AsyncRelayCommand(QuickScanModels));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand QuickSelectModelCommand => quickSelectModelCommand ?? (quickSelectModelCommand = new RelayCommand(QuickSelectModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand QuickSelectMmprojCommand => quickSelectMmprojCommand ?? (quickSelectMmprojCommand = new RelayCommand(QuickSelectMmproj));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand QuickSelectDraftCommand => quickSelectDraftCommand ?? (quickSelectDraftCommand = new RelayCommand(QuickSelectDraft));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncRelayCommand(StartServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncRelayCommand(StopServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand MeasureSpeedCommand => measureSpeedCommand ?? (measureSpeedCommand = new AsyncRelayCommand(MeasureSpeed));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CreateNewProfileCommand => createNewProfileCommand ?? (createNewProfileCommand = new AsyncRelayCommand(CreateNewProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveToProfileCommand => saveToProfileCommand ?? (saveToProfileCommand = new AsyncRelayCommand(SaveToProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RenameProfileCommand => renameProfileCommand ?? (renameProfileCommand = new AsyncRelayCommand(RenameProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DeleteProfileCommand => deleteProfileCommand ?? (deleteProfileCommand = new AsyncRelayCommand(DeleteProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CleanupMissingModelProfilesCommand => cleanupMissingModelProfilesCommand ?? (cleanupMissingModelProfilesCommand = new AsyncRelayCommand(CleanupMissingModelProfiles));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SetDefaultProfileCommand => setDefaultProfileCommand ?? (setDefaultProfileCommand = new AsyncRelayCommand(SetDefaultProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CopyFromProfileCommand => copyFromProfileCommand ?? (copyFromProfileCommand = new AsyncRelayCommand(CopyFromProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckLlamaCppVersionCommand => checkLlamaCppVersionCommand ?? (checkLlamaCppVersionCommand = new AsyncRelayCommand(CheckLlamaCppVersion));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ValidateCliCommand => validateCliCommand ?? (validateCliCommand = new AsyncRelayCommand(ValidateCliAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectLlamaCppDirectoryCommand => selectLlamaCppDirectoryCommand ?? (selectLlamaCppDirectoryCommand = new AsyncRelayCommand(SelectLlamaCppDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ScanServersAsyncCommand => scanServersAsyncCommand ?? (scanServersAsyncCommand = new RelayCommand(ScanServersAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> SelectServerCommand => selectServerCommand ?? (selectServerCommand = new AsyncRelayCommand<string>(SelectServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportToBatCommand => exportToBatCommand ?? (exportToBatCommand = new AsyncRelayCommand(ExportToBat));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CopyCommandLineCommand => copyCommandLineCommand ?? (copyCommandLineCommand = new AsyncRelayCommand(CopyCommandLine));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ImportFromBatCommand => importFromBatCommand ?? (importFromBatCommand = new AsyncRelayCommand(ImportFromBat));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand OpenArgPickerCommand => openArgPickerCommand ?? (openArgPickerCommand = new AsyncRelayCommand(OpenArgPicker));

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

	private static long GetTotalPhysicalMemory()
	{
		try
		{
			nint memoryBuffer = Marshal.AllocHGlobal(64);
			try
			{
				Marshal.WriteInt32(memoryBuffer, 64);
				if (GlobalMemoryStatusEx(memoryBuffer))
				{
					return Marshal.ReadInt64(memoryBuffer, 8);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(memoryBuffer);
			}
		}
		catch
		{
		}
		return 68719476736L;
	}

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(nint lpBuffer);

	private void SetCommandLinePreview(string value)
	{
		CommandLinePreview = value;
	}

	public string? ValidateCustomArguments()
	{
		if (string.IsNullOrWhiteSpace(AdditionalArgs))
		{
			return null;
		}
		string[] argLines = AdditionalArgs.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < argLines.Length; i++)
		{
			string[] parts = argLines[i].Split(new char[2] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length == 0)
			{
				continue;
			}
			string argName = parts[0];
			string argValue = ((parts.Length > 1) ? parts[1].Trim('"') : null);
			LlamaArgumentRegistry.ArgDefinition definition = LlamaArgumentRegistry.GetDefinition(argName);
			if (definition != null && definition.AllowedValues != null && definition.AllowedValues.Length != 0)
			{
				if (string.IsNullOrWhiteSpace(argValue))
				{
					return string.Format(_loc.T("server.arg_requires_value_fmt"), argName, string.Join(", ", definition.AllowedValues));
				}
				if (!definition.AllowedValues.Contains<string>(argValue, StringComparer.OrdinalIgnoreCase))
				{
					return string.Format(_loc.T("server.arg_invalid_value_fmt"), argName, argValue, string.Join(", ", definition.AllowedValues));
				}
			}
		}
		return null;
	}

	private static Dictionary<string, string> ParseAdditionalArgs(string additionalArgs)
	{
		if (string.IsNullOrWhiteSpace(additionalArgs))
		{
			return new Dictionary<string, string>();
		}
		return (from line in additionalArgs.Split(new char[2] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			select line.Split(new char[2] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToDictionary((string[] parts) => parts[0], (string[] parts) => (parts.Length <= 1) ? string.Empty : parts[1]);
	}

	private void AddCustomArg()
	{
		string argText = "--custom-arg";
		if (string.IsNullOrWhiteSpace(AdditionalArgs))
		{
			AdditionalArgs = argText;
		}
		else
		{
			AdditionalArgs = AdditionalArgs.TrimEnd() + "\n" + argText;
		}
	}

	private void RemoveChip(string argument)
	{
		CustomArgChip customArgChip = CustomArgChips.FirstOrDefault((CustomArgChip c) => c.Argument == argument);
		if (customArgChip != null)
		{
			CustomArgChips.Remove(customArgChip);
		}
		UpdateAdditionalArgsFromChips();
	}

	private void UpdateAdditionalArgsFromChips()
	{
		AdditionalArgs = string.Join("\n", CustomArgChips.Select((CustomArgChip c) => c.Argument));
	}

	private void UpdateChipsFromAdditionalArgs()
	{
		CustomArgChips.Clear();
		if (!string.IsNullOrWhiteSpace(AdditionalArgs))
		{
			string[] argLines = AdditionalArgs.Split(new char[2] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (string argLine in argLines)
			{
				CustomArgChips.Add(new CustomArgChip(argLine.Trim()));
			}
		}
	}

	public ServerViewModel(IServerManager serverManager, IProfileManager profileManager, ISettings settings, ILogService log, IDialogService dialog, IModelScanner scanner, ILlamaUpdater updater, ILocalizationService loc, IHelpParser helpParser, ICliValidator cliValidator, IHuggingFaceDownloader hfDownloader, IGpuMonitor gpuMonitor, MonitoringViewModel monitoringVm, IVersionCache versionCache)
	{
		_serverManager = serverManager;
		_profileManager = profileManager;
		_settings = settings;
		_log = log;
		_dialog = dialog;
		_scanner = scanner;
		_updater = updater;
		_loc = loc;
		_helpParser = helpParser;
		_cliValidator = cliValidator;
		_hfDownloader = hfDownloader;
		_gpuMonitor = gpuMonitor;
		_monitoringVm = monitoringVm;
		_versionCache = versionCache;
		GpuInfos = new ReadOnlyObservableCollection<GpuInfo>(_gpuInfos);
		_gpuTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(2.0)
		};
		_gpuTimer.Tick += delegate
		{
			RefreshGpuAsync();
		};
		_gpuTimer.Start();
		_autoSaveDebounceTimer = new Timer(delegate
		{
			Dispatcher.UIThread.Post(delegate
			{
				PerformAutoSaveProfile();
			});
		}, null, -1, -1);
		_serverManager.StatusChanged += OnStatusChanged;
		_loc.OnLanguageChanged += OnLanguageChanged;
		_versionCache.Changed += OnVersionCacheChanged;
		_settings.SettingsChanged += OnSettingsChanged;
		_profileManager.ProfileChanged += OnProfileChanged;
		// r105: the start button must reflect a running benchmark (llama-bench):
		// while a test goes on the server must not be started, and the button
		// says so.
		BenchmarkCenterViewModel.InstanceRunningChanged += OnBenchmarkRunningChanged;
		base.PropertyChanged += OnViewModelPropertyChanged;
		_updater.CliChangesDetected += OnCliChangesDetected;
		// Defaults below are only constructor bootstrap values. Keep the VM in
		// initialization mode until the selected profile has been applied so the
		// command-preview debounce cannot sync those defaults back to disk.
		_isInitializing = true;
		Host = _settings.DefaultHost;
		Port = _settings.DefaultPort;
		GpuLayers = _settings.DefaultGpuLayers;
		FlashAttention = _settings.FlashAttention;
		LlamaCppDirectory = _settings.LlamaCppDirectory;
		HfDownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache\\huggingface\\hub");
		_hfDownloader.DownloadProgress += OnHfDownloadProgress;
		_hfDownloader.StatusMessage += OnHfStatusMessage;
		UpdateExecutablePath();
		PopulateRecommendedModels();
		LoadProfiles();
		_isInitializing = false;
		_profileReady = SelectedProfile != null;
		if (RpcEnabled)
		{
			_rpcStartupTask = EnsureRpcServerOnStartupAsync();
		}
		// The version cache is loaded during application startup. Populate the
		// server selector immediately so it is usable on the first page open.
		ScanServersAsync();
	}

	/// <summary>r105: refresh the start-button label when a benchmark test starts/stops.</summary>
	private void OnBenchmarkRunningChanged()
	{
		OnPropertyChanged(nameof(StartBtn));
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Jinja
	{
		get => _jinja;
		set
		{
			if (_jinja != value)
			{
				_jinja = value;
				OnPropertyChanged(nameof(Jinja));
			}
		}
	}

	public bool DraftModelLocalOnly
	{
		get => _draftModelLocalOnly;
		set
		{
			if (_draftModelLocalOnly != value)
			{
				_draftModelLocalOnly = value;
				OnPropertyChanged(nameof(DraftModelLocalOnly));
			}
		}
	}

	public bool RpcEnabled
	{
		get => _rpcEnabled;
		set
		{
			if (_rpcEnabled != value)
			{
				OnPropertyChanging("RpcEnabled");
				_rpcEnabled = value;
				_rpcStartupAttempted = false;
				OnPropertyChanged("RpcEnabled");
				OnPropertyChanged("RpcCanStart");
				OnPropertyChanged("RpcCanStop");
				AutoSaveProfile();
				UpdateCommandLinePreview();
				_ = RefreshGpuAsync();
			}
		}
	}

	public bool RpcCanStart => RpcEnabled && !PassiveRpcMode && !RpcConnectionVerified;

	public bool RpcCanStop => RpcEnabled && !PassiveRpcMode && RpcConnectionVerified;

	public string RpcEndpoint
	{
		get => _rpcEndpoint;
		set
		{
			value ??= string.Empty;
			if (!string.Equals(_rpcEndpoint, value, StringComparison.Ordinal))
			{
				OnPropertyChanging("RpcEndpoint");
				_rpcEndpoint = value;
				_rpcStartupAttempted = false;
				OnPropertyChanged("RpcEndpoint");
			}
		}
	}

	public string RpcConnectionStatus
	{
		get => _rpcConnectionStatus;
		private set
		{
			if (!string.Equals(_rpcConnectionStatus, value, StringComparison.Ordinal))
			{
				OnPropertyChanging("RpcConnectionStatus");
				_rpcConnectionStatus = value;
				OnPropertyChanged("RpcConnectionStatus");
			}
		}
	}

	private void SetRpcConnectingStatus()
	{
		RpcConnectionStatus = "Подключение...";
	}

	private void SetRpcConnectedStatus()
	{
		RpcConnectionStatus = "Подключено — данные получены";
	}

	private void SetRpcDisconnectedStatus()
	{
		RpcConnectionStatus = "Отключено";
	}

	public string RpcHost
	{
		get => _rpcHost;
		set
		{
			value ??= string.Empty;
			if (!string.Equals(_rpcHost, value, StringComparison.Ordinal))
			{
				OnPropertyChanging("RpcHost");
				_rpcHost = value.Trim();
				_rpcStartupAttempted = false;
				OnPropertyChanged("RpcHost");
				UpdateRpcEndpointFromParts();
			}
		}
	}

	public int RpcPort
	{
		get => _rpcPort;
		set
		{
			if (_rpcPort != value)
			{
				OnPropertyChanging("RpcPort");
				_rpcPort = Math.Clamp(value, 1, 65535);
				_rpcStartupAttempted = false;
				OnPropertyChanged("RpcPort");
				UpdateRpcEndpointFromParts();
			}
		}
	}

	public string RpcSshUsername
	{
		get => _rpcSshUsername;
		set
		{
			if (SetProperty(ref _rpcSshUsername, value ?? string.Empty) && !_applyingRpcProfile)
			{
				AutoSaveProfile();
			}
		}
	}

	public string RpcSshPassword
	{
		get => _rpcSshPassword;
		set
		{
			if (SetProperty(ref _rpcSshPassword, value ?? string.Empty) && !_applyingRpcProfile)
			{
				_rpcSshPasswordEdited = true;
				AutoSaveProfile();
			}
		}
	}

    public bool RpcSavePassword
    {
        get => _rpcSavePassword;
        set
        {
            if (SetProperty(ref _rpcSavePassword, value) && !_applyingRpcProfile)
            {
                AutoSaveProfile();
            }
        }
    }

    public void SaveRpcCredentialsToProfile()
    {
        SaveRpcFieldsToProfile();
    }

    public void SaveRpcPasswordPreferenceToProfile()
    {
        SaveRpcFieldsToProfile();
    }

    private void SaveRpcFieldsToProfile()
    {
        if (!_profileReady || SelectedProfile == null)
        {
            return;
        }

        SelectedProfile.RpcSshUsername = RpcSshUsername.Trim();
        SelectedProfile.RpcSavePassword = RpcSavePassword;
		SelectedProfile.RpcSshPassword = RpcSavePassword ? RpcSshPassword : string.Empty;
		SelectedProfile.RpcUseCache = RpcUseCache;
		SelectedProfile.RpcRemoteTelemetry = RemoteTelemetryEnabled;
		SelectedProfile.RpcServerPath = RpcServerPath.Trim();
        _profileManager.SaveProfileNow(SelectedProfile);
    }

	public bool RpcUseCache
	{
		get => _rpcUseCache;
		set
		{
			if (SetProperty(ref _rpcUseCache, value))
			{
				AutoSaveProfile();
				UpdateCommandLinePreview();
			}
		}
	}

	public string RpcServerPath
	{
		get => _rpcServerPath;
		set
		{
			if (SetProperty(ref _rpcServerPath, value ?? string.Empty))
			{
				AutoSaveProfile();
			}
		}
	}

	public bool RpcConnectionVerified
	{
		get => _rpcConnectionVerified;
		private set
		{
			if (_rpcConnectionVerified != value)
			{
				_rpcConnectionVerified = value;
				OnPropertyChanged("RpcConnectionVerified");
				OnPropertyChanged("RpcCanStart");
				OnPropertyChanged("RpcCanStop");
			}
		}
	}

	public bool RpcSshConnected
	{
		get => _rpcSshConnected;
		private set
		{
			if (_rpcSshConnected != value)
			{
				_rpcSshConnected = value;
				OnPropertyChanged("RpcSshConnected");
				OnPropertyChanged("RpcSshIndicator");
				OnPropertyChanged("RpcSshIndicatorBrush");
				OnPropertyChanged("RpcSshButtonLabel");
				OnPropertyChanged("RpcSshButtonBrush");
			}
		}
	}

	private void OnVersionCacheChanged(object? sender, EventArgs e)
	{
		// Release installation can finish on a background task. Update the
		// ObservableCollection on Avalonia's UI thread so the ComboBox refreshes
		// immediately without restarting LlamaStudio.
		Dispatcher.UIThread.Post(ScanServersAsync);
	}

	private void OnSettingsChanged()
	{
		// Releases and the dashboard update the shared active version directly.
		// Reflect that change in the server selector and command preview too.
		Dispatcher.UIThread.Post(() =>
		{
			UpdateExecutablePath();
			ScanServersAsync();
		});
	}

	private void OnHfDownloadProgress(object? sender, double progress)
	{
		HfDownloadProgress = progress;
	}

	private void OnHfStatusMessage(object? sender, string msg)
	{
		HfDownloadStatus = msg;
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Constructor defaults are assigned before LoadProfiles applies the
		// selected profile. Do not schedule an autosave for those transient
		// values, otherwise they can overwrite the loaded profile after startup.
		if (_isInitializing || _isApplyingProfile)
		{
			return;
		}
		if (!new string[]
		{
			"ModelPath", "MmprojPath", "DraftModelPath", "Host", "Port", "Timeout", "Slots", "GpuLayers", "Threads", "ThreadsBatch",
			"MainGpu", "GpuSplitMode", "TensorSplit", "FlashAttention", "Mmap", "Mlock", "KvOffloadEnabled", "ContextSize", "BatchSize", "UbatchSize", "MaxTokens", "CacheTypeK",
			"CacheTypeV", "CacheRam", "CacheReuse", "Temperature", "TopK", "TopP", "MinP", "TypicalP", "RepeatPenalty", "RepeatLastN",
			"PresencePenalty", "FrequencyPenalty", "Seed", "MirostatMode", "MirostatTau", "MirostatEta", "DryMultiplier", "DryBase", "DynatempStddev", "XtcProbability",
			"XtcThreshold", "SpecType", "PredictCount", "SpeculativeDecoding", "DraftModelLocalOnly", "SpecDraftGpuLayers", "SpecDraftNMax", "SpecDraftNMin", "SpecDraftPSplit", "SpecDraftPMin", "RopeScaling",
			"RopeFreqBase", "RopeFreqScale", "YarnOriginalContext", "YarnExtFactor", "YarnAttnFactor", "YarnBetaFast", "YarnBetaSlow", "EmbeddingMode", "PoolingType", "Numa",
			"CachePrompt", "ContBatching", "ContextShift", "VerboseLogging", "EnableWebUI", "EnableSlots", "EnableMetrics", "Reasoning", "ReasoningBudget", "ApiKey",
			"Alias", "PriorityHigh", "AdditionalArgs", "RpcEnabled", "RpcEndpoint", "RpcHost", "RpcPort", "RpcSshUsername", "RpcSshPassword", "RpcSavePassword", "RpcUseCache", "RemoteTelemetryEnabled"
		}.Contains<string>(e.PropertyName))
		{
			return;
		}
		_commandLineDebounceTimer?.Dispose();
		_commandLineDebounceTimer = new Timer(delegate(object? _)
		{
			_ = UpdateCommandLinePreviewAsync();
			if (_helpInfo != null)
			{
				Dispatcher.UIThread.Post(delegate
				{
					UpdateUnsupportedFlags();
				});
			}
		}, null, 300, -1);
		_autoSaveDebounceTimer?.Change(500, -1);
	}

	private async Task UpdateCommandLinePreviewAsync()
	{
		if (SelectedProfile != null)
		{
			string preview = await Task.Run(delegate
			{
				SyncSettingsToProfile();
				return BuildCommandLinePreview(SelectedProfile);
			});
			Dispatcher.UIThread.Post(delegate
			{
				SetCommandLinePreview(preview);
			});
		}
	}

	private void OnProfileChanged(string profileId)
	{
		_profileRestartTask = HandleProfileChangedAsync(profileId);
	}

	private async Task HandleProfileChangedAsync(string profileId)
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
				ServerProfile serverProfile = Profiles.FirstOrDefault((ServerProfile p) => p.Id == profileId);
				if (serverProfile != null)
				{
					int index = Profiles.IndexOf(serverProfile);
					Profiles[index] = freshProfile;
				}
				else
				{
					Profiles.Add(freshProfile);
				}
				if (SelectedProfile?.Id == profileId)
				{
					bool pathsChanged = false;
					if (!string.IsNullOrWhiteSpace(freshProfile.ModelPath) && freshProfile.ModelPath != ModelPath)
					{
						ModelPath = freshProfile.ModelPath;
						pathsChanged = true;
					}
					if (!string.IsNullOrWhiteSpace(freshProfile.MmprojPath) && freshProfile.MmprojPath != MmprojPath)
					{
						MmprojPath = freshProfile.MmprojPath;
						pathsChanged = true;
					}
					if (!string.IsNullOrWhiteSpace(freshProfile.DraftModelPath) && freshProfile.DraftModelPath != DraftModelPath)
					{
						DraftModelPath = freshProfile.DraftModelPath;
						pathsChanged = true;
					}
					if (pathsChanged)
					{
						SyncSettingsToProfile();
						SetCommandLinePreview(BuildCommandLinePreview(SelectedProfile));
					}
				}
				else if (_settings.LastSelectedProfileId == profileId)
				{
					SelectedProfile = freshProfile;
				}
			});
		}
		catch (Exception exception)
		{
			_log.Error(exception, "HandleProfileChangedAsync failed", "Server");
		}
	}

	public Task WaitForProfileRestart()
	{
		return _profileRestartTask ?? Task.CompletedTask;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] localizedPropertyNames = new string[]
		{
			"Title", "StartBtn", "StopBtn", "MeasureSpeedBtn", "SaveProfileBtn", "LoadProfileBtn", "CopyFromProfileBtn", "RenameProfileBtn", "CreateProfileBtn", "DeleteProfileBtn", "CleanupMissingModelProfilesBtn",
			"ExportBatBtn", "CopyCmdLineBtn", "NoProfileSelected", "NoModelSelected", "BrowseModelBtn", "BrowseMmprojBtn", "BrowseDraftBtn", "QuickScanBtn", "QuickSelectModelBtn", "QuickSelectMmprojBtn",
			"QuickSelectDraftBtn", "CheckVersionBtn", "SelectLlamaDirBtn", "StateLabel", "TabModel", "TabGpu", "TabContextSampling", "TabAdvanced", "TabConnection", "ServerPathTitle",
			"ExecHint", "MainModelLabel", "ModelWatermark", "MmprojTitle", "OptionalBadge", "MmprojWatermark", "DraftTitle", "AdvancedBadge", "EnableSpeculative", "DraftWatermark",
			"QuickSelectTitle", "HfTitle", "DownloadToLabel", "SelectThisModelBtn", "DownloadModelBtn", "DownloadingStatus", "OrManual", "RepoLabel", "OfflineMode", "GpuAcceleration", "ServerBuildLabel",
			"GpuLayersLabel", "ThreadsLabel", "ThreadsBatchLabel", "MainGpuLabel", "MainGpuShortLabel", "UsedGpusLabel", "MainGpuDisabledTensorSplitLabel", "MainGpuSplitTip", "MainGpuTip", "ModelDistributionLabel", "AutomaticDistributionLabel", "GpuMonitoringLabel", "VramLabel", "GpuTempLabel", "GpuMemTempLabel", "PowerLabel", "GpuCoreLabel", "FanLabel", "ClockLabel", "ComputeCapabilityLabel", "FlashAttentionCheck", "MemoryMapCheck", "MlockCheck", "MmqCheck", "KvOffloadCheck", "ContextBatchTitle",
			"ContextLabel", "BatchLabel", "UbatchLabel", "MaxTokensLabel", "SamplingTitle", "TemperatureLabel", "TopPLabel", "TopKLabel", "MinPLabel", "TypicalPLabel",
			"RepeatPenaltyLabel", "RepeatLastNLabel", "PresencePenaltyLabel", "FrequencyPenaltyLabel", "SeedLabel", "MirostatSamplingCheck", "TauLabel", "EtaLabel", "MtpTitle", "PredictCountLabel",
			"SpecDraftParamsTitle", "DraftGpuLayersLabel", "DraftNMaxLabel", "DraftNMinLabel", "DraftPSplitLabel", "DraftPMinLabel", "YarnRopeTitle", "RopeFreqBaseLabel", "RopeFreqScaleLabel", "YarnOrigCtxLabel",
			"AdvancedOptionsTitle", "NumaCheck", "CachePromptCheck", "ContBatchingCheck", "ContextShiftCheck", "VerboseLoggingCheck", "WebUICheck", "MetricsCheck", "ReasoningCheck", "EmbeddingModeCheck",
			"PriorityHighCheck", "CustomArgsTitle", "CustomArgsWatermark", "CustomArgsDesc", "CustomArgsNote", "AddArgsBtnText", "TabCustomArgs", "TabRpc", "RpcTitle", "RpcDescription", "RpcEnabledLabel", "RpcEndpointLabel", "RpcHostLabel", "RpcPortLabel", "RpcCheckConnectionLabel", "RpcCheckSshLabel", "RpcShowPasswordLabel", "RpcStartLabel", "RpcStopLabel", "RpcSshUsernameLabel", "RpcSshPasswordLabel", "RpcSavePasswordLabel", "RpcUseCacheLabel", "RpcSshTitle", "RpcSshInstruction", "RpcManagementTitle", "ExportTitle", "ConnectionSettingsTitle", "HostLabel", "PortLabel", "TimeoutLabel", "SlotsLabel", "ProfileControlsTitle",
			"CmdLinePreviewTitle", "UnsupportedFlagsTitle", "UnsupportedFlagsHint", "ExecutableFoundText", "TipSpecType", "TipPriorityHigh", "TipModelPath", "TipMmprojPath", "TipDraftModelPath", "TipGpuLayers",
			"TipThreads", "TipThreadsBatch", "TipMainGpu", "TipFlashAttention", "TipMmap", "TipMlock", "TipMmq", "TipKvOffload", "TipContextSize", "TipBatchSize",
			"TipUBatchSize", "TipMaxTokens", "TipTemperature", "TipTopP", "TipTopK", "TipMinP", "TipTypicalP", "TipRepeatPenalty", "TipRepeatLastN", "TipPresencePenalty",
			"TipFrequencyPenalty", "TipSeed", "TipMirostat", "TipTau", "TipEta", "TipCacheTypeK", "TipCacheTypeV", "TipCacheRam", "TipCacheReuse", "TipPredictCount",
			"TipSpecDraftGpuLayers", "TipSpecDraftNMax", "TipSpecDraftNMin", "TipSpecDraftPSplit", "TipSpecDraftPMin", "TipRopeFreqBase", "TipRopeFreqScale", "TipYarnOriginalContext", "TipNuma", "TipCachePrompt",
			"TipContBatching", "TipContextShift", "TipVerboseLogging", "TipWebUI", "TipMetrics", "TipReasoning", "TipEmbeddingMode", "TipHost", "TipPort", "TipTimeout",
			"TipSlots", "TipCustomArgs"
		};
		foreach (string propertyName in localizedPropertyNames)
		{
			OnPropertyChanged(propertyName);
		}
	}

	private void PopulateRecommendedModels()
	{
		RecommendedModels.Add(new RecommendedModel("bartowski/Llama-4-Maverick-17B-128E-Instruct-Q4_K_M.gguf", "Llama 4 Maverick 17B", "Q4_K_M", "~9.5 GB", "17B params, 128K context"));
		RecommendedModels.Add(new RecommendedModel("bartowski/Llama-3.3-70B-Instruct-Q4_K_M.gguf", "Llama 3.3 70B Instruct", "Q4_K_M", "~43 GB", "70B params, general purpose"));
		RecommendedModels.Add(new RecommendedModel("bartowski/Qwen2.5-Coder-32B-Instruct-Q4_K_M.gguf", "Qwen 2.5 Coder 32B", "Q4_K_M", "~20 GB", "32B params, code generation"));
		RecommendedModels.Add(new RecommendedModel("bartowski/Mistral-Nemo-12B-Instruct-Q4_K_M.gguf", "Mistral Nemo 12B", "Q4_K_M", "~7.5 GB", "12B params, multilingual"));
		RecommendedModels.Add(new RecommendedModel("bartowski/Llama-3.1-8B-Instruct-Q4_K_M.gguf", "Llama 3.1 8B Instruct", "Q4_K_M", "~5.5 GB", "8B params, lightweight"));
	}

	[RelayCommand]
	private void SelectRecommendedModel(RecommendedModel model)
	{
		HfRepo = model.Repo;
	}

	[RelayCommand]
	private async Task DownloadHfModelAsync()
	{
		if (string.IsNullOrWhiteSpace(HfRepo))
		{
			HfDownloadStatus = _loc.T("models.enter_repo_first");
			return;
		}
		IsDownloadingHf = true;
		HfDownloadProgress = 0.0;
		HfDownloadStatus = _loc.T("server.starting_download");
		try
		{
			string downloadedFilePath = (ModelPath = await _hfDownloader.DownloadModelAsync(HfRepo, HfOffline));
			UpdateDisplayNames();
			HfDownloadStatus = string.Format(_loc.T("server.download_complete"), Path.GetFileName(downloadedFilePath));
			_log.Information("HF model downloaded: " + downloadedFilePath, "Server");
		}
		catch (OperationCanceledException)
		{
			HfDownloadStatus = _loc.T("server.download_cancelled");
		}
		catch (Exception downloadError)
		{
			HfDownloadStatus = string.Format(_loc.T("server.download_error"), downloadError.Message);
			_log.Error(downloadError, "HF download failed", "Server");
		}
		finally
		{
			IsDownloadingHf = false;
		}
	}

	private void UpdateExecutablePath()
	{
		if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
		{
			VersionInfo byVersionKey = _versionCache.GetByVersionKey(_settings.ActiveLlamaCppVersion);
			if (byVersionKey != null && Directory.Exists(byVersionKey.Directory) && File.Exists(Path.Combine(byVersionKey.Directory, LlamaServerExecutableName)))
			{
				ExecutablePath = Path.Combine(byVersionKey.Directory, LlamaServerExecutableName);
				ExecutableFound = true;
				if (ExecutableFound)
				{
					ParseHelpAsync();
				}
				return;
			}
			byVersionKey = _versionCache.GetByVersion(_settings.ActiveLlamaCppVersion);
			if (byVersionKey != null && Directory.Exists(byVersionKey.Directory) && File.Exists(Path.Combine(byVersionKey.Directory, LlamaServerExecutableName)))
			{
				ExecutablePath = Path.Combine(byVersionKey.Directory, LlamaServerExecutableName);
				ExecutableFound = true;
				if (ExecutableFound)
				{
					ParseHelpAsync();
				}
				return;
			}
		}
		string llamaCppDirectory = _settings.LlamaCppDirectory;
		ExecutablePath = Path.Combine(llamaCppDirectory, LlamaServerExecutableName);
		ExecutableFound = File.Exists(ExecutablePath);
		if (ExecutableFound)
		{
			ParseHelpAsync();
			return;
		}
		UnsupportedFlags.Clear();
		_helpInfo = null;
	}

	private async Task CheckServerAndOfferRestart(ServerProfile profile)
	{
		try
		{
			if ((DateTime.UtcNow - _lastServerRestart).TotalSeconds < 3.0 || (await _serverManager.GetStatusAsync()).State != ServerState.Running)
			{
				return;
			}
			_lastServerRestart = DateTime.UtcNow;
			SyncSettingsToProfileTarget(profile);
			IDisposable loadingWin = null;
			try
			{
				loadingWin = _dialog.ShowLoading(string.Format(_loc.T("server.restart_profile"), profile.Name), _loc.T("dialog.loading"));
			}
			catch (Exception ex)
			{
				_log.Warning("ShowLoading failed (window may be hidden): " + ex.Message, "Server");
			}
			try
			{
				await _serverManager.StopAsync();
				await Task.Delay(1500);
				await _serverManager.StartAsync(profile);
				_log.Information("Server restarted with profile: " + profile.Name, "Server");
			}
			catch (Exception restartError)
			{
				_log.Error(restartError, "Failed to restart server", "Server");
				try
				{
					await _dialog.ShowErrorAsync(string.Format(_loc.T("server.restart_error"), restartError.Message));
				}
				catch
				{
				}
			}
			finally
			{
				loadingWin?.Dispose();
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "CheckServerAndOfferRestart failed", "Server");
		}
	}

	private void LoadProfiles()
	{
		bool wasInitializing = _isInitializing;
		_isInitializing = true;
		try
		{
			List<ServerProfile> allProfiles = _profileManager.GetAllProfiles();
			_log.Information($"Loaded {allProfiles.Count} profiles", "Server");
			Profiles.Clear();
			foreach (ServerProfile profile in allProfiles)
			{
				if (!HasMissingModel(profile))
				{
					Profiles.Add(profile);
				}
			}
			ServerProfile serverProfile = null;
			if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
			{
				serverProfile = Profiles.FirstOrDefault((ServerProfile p) => p.Id == _settings.LastSelectedProfileId);
			}
			if (serverProfile == null && Profiles.Count > 0)
			{
				serverProfile = Profiles.FirstOrDefault();
			}
			if (serverProfile != null)
			{
				SelectedProfile = serverProfile;
			}
		}
		finally
		{
			_isInitializing = wasInitializing;
		}
	}

	private void ApplyProfileToSettings(ServerProfile profile)
	{
		bool wasApplyingProfile = _isApplyingProfile;
		_isApplyingProfile = true;
		try
		{
		string previousRpcEndpoint = RpcEndpoint;
		string previousRpcSshUsername = RpcSshUsername;
		bool sameRpcSettings = string.Equals(previousRpcEndpoint, profile.RpcEndpoint ?? string.Empty, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(previousRpcSshUsername, profile.RpcSshUsername ?? string.Empty, StringComparison.Ordinal);
		bool preserveSshStatus = sameRpcSettings && RpcSshConnected;
		bool preserveRpcStatus = sameRpcSettings && RpcConnectionVerified;
		bool preserveEditedPassword = sameRpcSettings && _rpcSshPasswordEdited
			&& string.Equals(_previousSelectedProfileId, profile.Id.ToString(), StringComparison.OrdinalIgnoreCase);
		ModelPath = profile.ModelPath ?? string.Empty;
		MmprojPath = profile.MmprojPath ?? string.Empty;
		DraftModelPath = profile.DraftModelPath ?? string.Empty;
		UpdateDisplayNames();
		HfRepo = profile.HfRepo ?? string.Empty;
		HfFile = profile.HfFile ?? string.Empty;
		HfOffline = profile.HfOffline;
		HfRepoDraft = profile.HfRepoDraft ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(profile.LlamaCppBaseDirectory))
		{
			_settings.LlamaCppBaseDirectory = profile.LlamaCppBaseDirectory;
		}
		Host = ((!string.IsNullOrWhiteSpace(profile.Host)) ? profile.Host : (_settings.DefaultHost ?? "127.0.0.1"));
		Port = ((profile.Port > 0) ? profile.Port : ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080));
		RpcEnabled = profile.RpcEnabled;
		// Older profiles stored the SSH host/port separately while leaving the
		// canonical --rpc endpoint empty. Rebuild it before restoring runtime
		// state so the command preview and connection status use the same address.
		string restoredRpcEndpoint = profile.RpcEndpoint?.Trim() ?? string.Empty;
		if (RpcEnabled && string.IsNullOrWhiteSpace(restoredRpcEndpoint) && !string.IsNullOrWhiteSpace(RpcHost) && RpcPort is >= 1 and <= 65535)
		{
			restoredRpcEndpoint = $"{RpcHost}:{RpcPort}";
		}
		RpcEndpoint = restoredRpcEndpoint;
		ParseRpcEndpointParts(RpcEndpoint);
		if (RpcEnabled && string.IsNullOrWhiteSpace(RpcEndpoint) && !string.IsNullOrWhiteSpace(RpcHost) && RpcPort is >= 1 and <= 65535)
		{
			UpdateRpcEndpointFromParts();
		}
		RpcSshUsername = profile.RpcSshUsername ?? string.Empty;
		if (!preserveEditedPassword)
		{
			_applyingRpcProfile = true;
			RpcSshPassword = profile.RpcSavePassword ? (profile.RpcSshPassword ?? string.Empty) : string.Empty;
			_applyingRpcProfile = false;
			_rpcSshPasswordEdited = false;
		}
		RpcSavePassword = profile.RpcSavePassword;
		RpcUseCache = profile.RpcUseCache;
		RemoteTelemetryEnabled = profile.RpcRemoteTelemetry;
		// This path belongs exclusively to the remote computer. Do not derive it
		// from the local llama.cpp version when loading or creating a profile.
		RpcServerPath = profile.RpcServerPath?.Trim() ?? string.Empty;
		if (!preserveSshStatus)
		{
			RpcSshConnected = false;
			RpcSshStatus = string.Empty;
		}
		if (!preserveRpcStatus)
		{
			RpcConnectionVerified = false;
			SetRpcDisconnectedStatus();
		}
		Timeout = profile.Timeout;
		Slots = profile.Slots;
		GpuLayers = profile.GpuLayers;
		Threads = profile.Threads;
		ThreadsBatch = profile.ThreadsBatch;
		FlashAttention = profile.FlashAttention;
		Mmap = profile.Mmap;
		Mlock = profile.Mlock;
		KvOffloadEnabled = profile.KvOffloadEnabled;
		MainGpu = profile.MainGpu;
		_pendingGpuSelection = profile.SelectedGpuIndices ?? Array.Empty<int>();
		ApplyGpuSelection();
		// "row" was removed from the Studio UI. Profiles from older releases
		// are migrated to layer before they reach the command preview.
		GpuSplitMode = profile.GpuSplitMode == GpuSplitMode.Row ? GpuSplitMode.Layer : profile.GpuSplitMode;
		// r68: normalize tensor + Auto layers to All (see SetGpuSplitMode).
		if (GpuSplitMode == GpuSplitMode.Tensor && (string.IsNullOrWhiteSpace(GpuLayers) || GpuLayers == "-1"))
		{
			GpuLayers = "all";
		}
		TensorSplit = string.Join(",", profile.TensorSplit);
		ContextSize = profile.ContextSize;
		BatchSize = profile.BatchSize;
		UbatchSize = profile.UbatchSize;
		MaxTokens = profile.MaxTokens;
		CacheTypeK = profile.CacheTypeK.ToString().ToLower();
		CacheTypeV = profile.CacheTypeV.ToString().ToLower();
		CacheRam = profile.CacheRam;
		CacheReuse = profile.CacheReuse;
		Temperature = profile.Temperature;
		TopK = profile.TopK;
		TopP = profile.TopP;
		MinP = profile.MinP;
		TypicalP = profile.TypicalP;
		RepeatPenalty = profile.RepeatPenalty;
		RepeatLastN = profile.RepeatLastN;
		PresencePenalty = profile.PresencePenalty;
		FrequencyPenalty = profile.FrequencyPenalty;
		Seed = profile.Seed;
		MirostatMode = profile.Mirostat;
		MirostatTau = profile.MirostatTau;
		MirostatEta = profile.MirostatEta;
		DryMultiplier = profile.DryMultiplier;
		DryBase = profile.DryBase;
		DynatempStddev = profile.DynatempStddev;
		XtcProbability = profile.XtcProbability;
		XtcThreshold = profile.XtcThreshold;
		SpecType = (string.IsNullOrWhiteSpace(profile.SpecType) ? "none" : profile.SpecType);
		PredictCount = profile.PredictCount;
		SpeculativeDecoding = profile.SpeculativeDecoding;
		Jinja = profile.Jinja;
		DraftModelLocalOnly = profile.DraftModelLocalOnly;
		SpecDraftGpuLayers = profile.SpecDraftGpuLayers;
		SpecDraftNMax = profile.SpecDraftNMax;
		SpecDraftNMin = profile.SpecDraftNMin;
		SpecDraftPSplit = profile.SpecDraftPSplit;
		SpecDraftPMin = profile.SpecDraftPMin;
		PriorityHigh = profile.PriorityHigh;
		EmbeddingMode = profile.EmbeddingMode;
		PoolingType = profile.PoolingType;
		Numa = profile.Numa;
		ProcessPriority = profile.ProcessPriority;
		CachePrompt = profile.CachePrompt;
		ContBatching = profile.ContBatching;
		ContextShift = profile.ContextShift;
		VerboseLogging = profile.VerboseLogging;
		EnableWebUI = profile.EnableWebUI;
		EnableSlots = profile.EnableSlots;
		EnableMetrics = profile.EnableMetrics;
		Reasoning = profile.Reasoning;
		ReasoningBudget = profile.ReasoningBudget;
		RopeScaling = profile.RopeScaling;
		RopeFreqBase = profile.RopeFreqBase;
		RopeFreqScale = profile.RopeFreqScale;
		YarnOriginalContext = profile.YarnOriginalContext;
		YarnExtFactor = profile.YarnExtFactor;
		YarnAttnFactor = profile.YarnAttnFactor;
		YarnBetaFast = profile.YarnBetaFast;
		YarnBetaSlow = profile.YarnBetaSlow;
		ApiKey = profile.ApiKey ?? string.Empty;
		Alias = profile.Alias ?? string.Empty;
		AdditionalArgs = string.Join("\n", profile.CustomArguments.Select<KeyValuePair<string, string>, string>((KeyValuePair<string, string> kv) => (!string.IsNullOrEmpty(kv.Value)) ? (kv.Key + " " + kv.Value) : kv.Key));
		UpdateChipsFromAdditionalArgs();
		}
		finally
		{
			_isApplyingProfile = wasApplyingProfile;
		}
	}

	private void UpdateDisplayNames()
	{
		ModelDisplayName = (string.IsNullOrWhiteSpace(ModelPath) ? _loc.T("server.model_not_selected") : Path.GetFileName(ModelPath));
		MmprojDisplayName = (string.IsNullOrWhiteSpace(MmprojPath) ? _loc.T("server.mmproj_not_selected") : Path.GetFileName(MmprojPath));
		DraftDisplayName = (string.IsNullOrWhiteSpace(DraftModelPath) ? _loc.T("server.draft_not_selected") : Path.GetFileName(DraftModelPath));
	}

	public void SetModelFromScanner(GgufModelInfo model)
	{
		if (model != null)
		{
			ModelPath = model.Path;
			UpdateDisplayNames();
			if (SelectedProfile != null)
			{
				SelectedProfile.ModelPath = model.Path;
				_profileManager.SaveProfileAsync(SelectedProfile);
			}
			_log.Information("Model set from scanner: " + model.FileName, "Server");
		}
	}

	public void SetMmprojFromScanner(GgufModelInfo model)
	{
		if (model != null)
		{
			MmprojPath = model.Path;
			UpdateDisplayNames();
			if (SelectedProfile != null)
			{
				SelectedProfile.MmprojPath = model.Path;
				_profileManager.SaveProfileAsync(SelectedProfile);
			}
			_log.Information("mmproj set from scanner: " + model.FileName, "Server");
		}
	}

	public void SetDraftModelFromScanner(GgufModelInfo model)
	{
		if (model != null)
		{
			DraftModelPath = model.Path;
			UpdateDisplayNames();
			if (SelectedProfile != null)
			{
				SelectedProfile.DraftModelPath = model.Path;
				_profileManager.SaveProfileAsync(SelectedProfile);
			}
			_log.Information("Draft model set from scanner: " + model.FileName, "Server");
		}
	}

	private void OnStatusChanged(object? sender, ServerStatus status)
	{
		State = status.State;
		VramUsedGb = status.VramUsedGb;
		RamUsedGb = status.RamUsedGb;
		RamCommittedGb = status.RamCommittedGb;
		TokensPerSecond = status.TokensPerSecond;
		PromptTokensPerSecond = status.PromptTokensPerSecond;
		QueueSize = status.QueueSize;
		ActiveSlots = status.ActiveSlots;
		Uptime = status.Uptime;
		ProcessId = status.ProcessId?.ToString() ?? "—";
		ErrorMessage = status.ErrorMessage ?? string.Empty;
		if (status.State == ServerState.Stopping || status.State == ServerState.Starting)
		{
			_lastServerRestart = DateTime.UtcNow;
		}
		// RPC startup is coordinated once during application startup. Do not
		// restart the remote process from llama-server state notifications: those
		// notifications are emitted several times during one normal launch.
	}

	private static bool HasSameGpuStructure(IReadOnlyList<GpuInfo> current, IReadOnlyList<GpuInfo> refreshed)
	{
		if (current.Count != refreshed.Count)
		{
			return false;
		}
		for (int i = 0; i < current.Count; i++)
		{
			GpuInfo left = current[i];
			GpuInfo right = refreshed[i];
			if (left.Index != right.Index || left.IsRemote != right.IsRemote || !string.Equals(left.RemoteDeviceId, right.RemoteDeviceId, StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}

	private static void CopyGpuInfo(GpuInfo target, GpuInfo source)
	{
		target.Name = source.Name;
		target.ComputeCapability = source.ComputeCapability;
		target.PerformanceScore = source.PerformanceScore;
		target.IsSelected = source.IsSelected;
		target.TemperatureCelsius = source.TemperatureCelsius;
		target.MemoryTemperatureCelsius = source.MemoryTemperatureCelsius;
		target.GpuUtilization = source.GpuUtilization;
		target.MemoryUsedGb = source.MemoryUsedGb;
		target.MemoryTotalGb = source.MemoryTotalGb;
		target.PowerDrawWatts = source.PowerDrawWatts;
		target.PowerLimitWatts = source.PowerLimitWatts;
		target.FanSpeed = source.FanSpeed;
		target.ClockMhz = source.ClockMhz;
		target.DriverVersion = source.DriverVersion;
	}

	private async Task RefreshGpuAsync()
	{
		try
		{
			IReadOnlyList<GpuInfo> detected = await _gpuMonitor.GetGpuInfosAsync();
			if (detected.Count > 0)
			{
				// r99: self-heal. If the pending selection was somehow emptied while
				// a profile with an explicit selection is active (e.g. overlapping
				// refreshes across a profile switch), do NOT treat the empty set as
				// "all cards" (that would light up every card in the UI). Re-sync
				// from the profile — the profile is the source of truth.
				if (_gpuSelectionInitialized && _pendingGpuSelection.Length == 0
					&& SelectedProfile?.SelectedGpuIndices is { Length: > 0 } profileSelection)
				{
					_pendingGpuSelection = profileSelection;
				}
				// Keep the profile selection for devices that are temporarily absent.
				// The missing GPU is excluded from the current runtime list, but its
				// index must remain pending so it can be restored when the device returns.
				// r99: the source of truth for the selection is the pending
				// (profile-driven) list — never the stale in-memory GpuInfos. At
				// startup _gpuInfos is still empty, and the old "empty => all
				// cards" fallback then lit up every card; the next save/start
				// wrote that wrong selection back into the profile file.
				HashSet<int> selected = new HashSet<int>(_pendingGpuSelection);
				bool useAllDetectedGpus = selected.Count == 0;
				List<GpuInfo> refreshedGpuInfos = new List<GpuInfo>();
				foreach (GpuInfo gpu in detected)
				{
					gpu.IsSelected = useAllDetectedGpus || selected.Contains(gpu.Index);
					refreshedGpuInfos.Add(gpu);
				}
				if (RpcEnabled && RpcSshConnected)
				{
					GpuInfo? remoteGpu = RemoteTelemetryEnabled
						? await GetRemoteGpuTelemetryAsync()
						: null;
					refreshedGpuInfos.Add(remoteGpu ?? (RemoteTelemetryEnabled ? _lastRemoteGpuTelemetry : null) ?? new GpuInfo
					{
						Index = RemoteGpuIndex,
						IsRemote = true,
						RemoteDeviceId = "RPC:CUDA0",
						Name = "Remote GPU (RPC)",
						IsSelected = true,
					});
				}
				if (HasSameGpuStructure(_gpuInfos, refreshedGpuInfos))
				{
					for (int i = 0; i < _gpuInfos.Count; i++)
					{
						CopyGpuInfo(_gpuInfos[i], refreshedGpuInfos[i]);
					}
				}
				else
				{
					_gpuInfos.Clear();
					foreach (GpuInfo gpu in refreshedGpuInfos)
					{
						_gpuInfos.Add(gpu);
					}
				}
				_monitoringVm.SetRemoteGpuInfo(_gpuInfos.FirstOrDefault(gpu => gpu.IsRemote));
				_gpuSelectionInitialized = true;
				int[] presentSelected = _gpuInfos.Where(gpu => gpu.IsSelected && gpu.Index >= 0).Select(gpu => gpu.Index).ToArray();
				_pendingGpuSelection = _pendingGpuSelection
					.Where(index => !detected.Any(gpu => gpu.Index == index))
					.Concat(presentSelected)
					.Distinct()
					.ToArray();
				ApplyGpuSelection();
				GpuInfo gpuInfo = detected[0];
				GpuVramUsedGb = gpuInfo.MemoryUsedGb;
				OnPropertyChanged("GpuVramUsedGb");
				OnPropertyChanged("HasMultipleGpus");
				OnPropertyChanged("ShowLegacyGpuFields");
				OnPropertyChanged("HasGpuSelection");
				OnPropertyChanged("SelectedGpuDeviceText");
				OnPropertyChanged("RecommendedTensorSplitText");
				OnPropertyChanged("GpuInfosTelemetry");
				// GPU polling is telemetry, not a profile edit. Persisting the VM
				// snapshot here overwrites just-saved web/desktop settings every tick.
				UpdateCommandLinePreview();
			}
		}
		catch
		{
		}
	}

	private async Task<GpuInfo?> GetRemoteGpuTelemetryAsync()
	{
		if ((DateTime.UtcNow - _lastRemoteTelemetryAt).TotalSeconds < 5 && _lastRemoteGpuTelemetry != null)
		{
			return _lastRemoteGpuTelemetry;
		}
		if (Interlocked.Exchange(ref _remoteTelemetryRefreshInProgress, 1) != 0)
		{
			return _lastRemoteGpuTelemetry;
		}
		try
		{
			const string query = "nvidia-smi --query-gpu=index,name,compute_cap,temperature.gpu,temperature.memory,utilization.gpu,memory.used,memory.total,power.draw,power.limit,fan.speed,clocks.current.graphics,driver_version --format=csv,noheader,nounits";
			string? output = await RunSshScriptForOutputAsync(query);
			GpuInfo? parsed = ParseRemoteGpuTelemetry(output);
			if (parsed != null)
			{
				_lastRemoteGpuTelemetry = parsed;
				_lastRemoteTelemetryAt = DateTime.UtcNow;
			}
			return parsed;
		}
		finally
		{
			Interlocked.Exchange(ref _remoteTelemetryRefreshInProgress, 0);
		}
	}

	private async Task<string?> RunSshScriptForOutputAsync(string script)
	{
		if (string.IsNullOrWhiteSpace(RpcSshUsername) || string.IsNullOrWhiteSpace(RpcSshPassword))
		{
			return null;
		}
		try
		{
			string commandText = IsLinuxRpcExecutable(RpcServerPath)
				? script
				: $"powershell -NoProfile -EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(script))}";
			using SshClient client = new SshClient(RpcHost, 22, RpcSshUsername.Trim(), RpcSshPassword);
			client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
			await Task.Run(client.Connect);
			using SshCommand command = client.CreateCommand(commandText);
			string output = await Task.Run(command.Execute);
			client.Disconnect();
			if (command.ExitStatus == 0)
			{
				RpcSshConnected = true;
				return output;
			}
			return null;
		}
		catch (Exception ex)
		{
			_log.Warning("Remote GPU telemetry query failed: " + ex.Message, "Server");
			return null;
		}
	}

	private static GpuInfo? ParseRemoteGpuTelemetry(string? output)
	{
		string? line = output?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
		if (string.IsNullOrWhiteSpace(line))
		{
			return null;
		}
		string[] values = line.Split(',');
		if (values.Length < 13 || !double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double index))
		{
			return null;
		}
		double Number(int position)
		{
			return double.TryParse(values[position].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0.0;
		}
		GpuInfo gpu = new GpuInfo
		{
			Index = RemoteGpuIndex,
			IsRemote = true,
			RemoteDeviceId = "RPC:CUDA" + (int)index,
			Name = values[1].Trim() + " (RPC)",
			ComputeCapability = Number(2),
			TemperatureCelsius = Number(3),
			MemoryTemperatureCelsius = Number(4),
			GpuUtilization = Number(5),
			MemoryUsedGb = Number(6) / 1024.0,
			MemoryTotalGb = Number(7) / 1024.0,
			PowerDrawWatts = Number(8),
			PowerLimitWatts = Number(9),
			FanSpeed = (int)Number(10),
			ClockMhz = (int)Number(11),
			DriverVersion = values[12].Trim(),
			IsSelected = true
		};
		gpu.PerformanceScore = Math.Max(1.0, gpu.ComputeCapability > 0 ? gpu.ComputeCapability / 5.0 : gpu.MemoryTotalGb / 8.0);
		return gpu;
	}

	private async Task EnsureRpcServerOnStartupAsync()
	{
		if (!RpcEnabled || PassiveRpcMode || _rpcStartupAttempted || RpcConnectionVerified || Interlocked.Exchange(ref _rpcAutoStartInProgress, 1) != 0)
		{
			return;
		}
		_rpcStartupAttempted = true;

		try
		{
			if (!await CheckSshConnectionAsync())
			{
				return;
			}

			await CheckRpcConnectionAsync();
			// A reachable port is not enough: it may belong to a stale or duplicate
			// ggml-rpc-server. StartRpcServerAsync performs the single coordinated
			// cleanup/restart and verifies the endpoint again afterwards.
			await StartRpcServerAsync();
		}
		catch (Exception ex)
		{
			_log.Error(ex, "RPC startup check failed", "Server");
		}
		finally
		{
			Volatile.Write(ref _rpcAutoStartInProgress, 0);
		}
	}

	public async Task CheckRpcConnectionAsync()
	{
		if (PassiveRpcMode)
		{
			RpcConnectionVerified = false;
			RpcConnectionStatus = "Пассивный режим: RPC проверяется только самим llama-server.";
			await RefreshGpuAsync();
			return;
		}

		RpcConnectionVerified = false;
		SetRpcConnectingStatus();
		string endpoint = GetRpcEndpointForStorage();
		if (!string.Equals(RpcEndpoint, endpoint, StringComparison.Ordinal))
		{
			RpcEndpoint = endpoint;
		}
		int separator = endpoint.LastIndexOf(':');
		if (separator <= 0 || separator == endpoint.Length - 1 || !int.TryParse(endpoint[(separator + 1)..], out int port) || port is < 1 or > 65535)
		{
			SetRpcDisconnectedStatus();
			return;
		}

		string host = endpoint[..separator].Trim();
		if (string.IsNullOrWhiteSpace(host))
		{
			SetRpcDisconnectedStatus();
			return;
		}

		try
		{
			using TcpClient client = new TcpClient();
			try
			{
				await client.ConnectAsync(host, port).WaitAsync(TimeSpan.FromSeconds(5));
			}
			catch
			{
				SetRpcDisconnectedStatus();
				await RefreshGpuAsync();
				return;
			}
			RpcHost = host;
			RpcPort = port;
			RpcConnectionVerified = true;
			await RefreshGpuAsync();
			SetRpcConnectedStatus();
		}
		catch (Exception ex)
		{
			RpcConnectionVerified = false;
			await RefreshGpuAsync();
			_log.Error(ex, "RPC connection check failed", "Server");
			SetRpcDisconnectedStatus();
		}
	}

	public async Task CheckRpcSshAsync()
	{
		if (PassiveRpcMode)
		{
			RpcSshConnected = false;
			RpcSshStatus = "Пассивный режим: SSH-управление отключено для теста.";
			return;
		}

		if (System.Threading.Interlocked.Exchange(ref _rpcSshCheckInProgress, 1) != 0)
		{
			return;
		}

		try
		{
			RpcSshConnected = false;
			RpcSshStatus = _loc.T("server.rpc_status_ssh_checking");
			RpcSshConnected = await CheckSshConnectionAsync();
		}
		finally
		{
			System.Threading.Volatile.Write(ref _rpcSshCheckInProgress, 0);
		}
	}

	private async Task<bool> CheckSshConnectionAsync()
	{
		if (string.IsNullOrWhiteSpace(RpcHost) || string.IsNullOrWhiteSpace(RpcSshUsername) || string.IsNullOrWhiteSpace(RpcSshPassword))
		{
			RpcSshConnected = false;
			RpcSshStatus = _loc.T("server.rpc_status_credentials_missing");
			return false;
		}

		try
		{
			using SshClient client = new SshClient(RpcHost, 22, RpcSshUsername.Trim(), RpcSshPassword);
			client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
			await Task.Run(client.Connect);
			client.Disconnect();
			RpcSshConnected = true;
			RpcSshStatus = string.Empty;
			await RefreshGpuAsync();
			return true;
		}
		catch (Exception ex)
		{
			RpcSshConnected = false;
			RpcSshStatus = _loc.T("server.rpc_status_ssh_failed") + " " + ex.Message;
			return false;
		}
	}

	private string? GetSelectedRpcServerFolderName(string? profileVersion = null)
	{
		string versionKey = profileVersion ?? SelectedProfile?.SelectedServerVersion ?? _settings.ActiveLlamaCppVersion ?? string.Empty;
		VersionInfo? versionInfo = null;
		if (!string.IsNullOrWhiteSpace(versionKey))
		{
			versionInfo = _versionCache.GetByVersionKey(versionKey) ?? _versionCache.GetByVersion(versionKey);
		}

		if (versionInfo != null && !string.IsNullOrWhiteSpace(versionInfo.Directory))
		{
			return Path.GetFileName(versionInfo.Directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		}

		string[] parts = versionKey.Split('|', 2, StringSplitOptions.TrimEntries);
		if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
		{
			return parts[0] + "-" + parts[1].ToLowerInvariant();
		}

		return null;
	}

	private string GetDefaultRpcServerPath(string? profileVersion = null)
	{
		string? folder = GetSelectedRpcServerFolderName(profileVersion);
		return string.IsNullOrWhiteSpace(folder)
			? string.Empty
			: System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio", folder, "ggml-rpc-server.exe");
	}

	public async Task StartRpcServerAsync()
	{
		if (Interlocked.Exchange(ref _rpcStartInProgress, 1) != 0)
		{
			// A second request must wait for the existing remote start instead of
			// returning early and allowing llama-server to race the first request.
			for (int attempt = 0; attempt < 240 && Volatile.Read(ref _rpcStartInProgress) != 0; attempt++)
			{
				await Task.Delay(100);
			}
			return;
		}

		try
		{
			await StartRpcServerCoreAsync();
		}
		finally
		{
			Volatile.Write(ref _rpcStartInProgress, 0);
		}
	}

	private async Task<bool> WaitForRpcEndpointAsync(int attempts = 40)
	{
		for (int attempt = 0; attempt < attempts; attempt++)
		{
			await Task.Delay(250);
			using TcpClient client = new TcpClient();
			try
			{
				await client.ConnectAsync(RpcHost, RpcPort).WaitAsync(TimeSpan.FromSeconds(1));
				RpcConnectionVerified = true;
				await RefreshGpuAsync();
				SetRpcConnectedStatus();
				return true;
			}
			catch
			{
				// The remote process can need several seconds to bind its port.
			}
		}

		return false;
	}

	private async Task StartRpcServerCoreAsync()
	{
		SetRpcConnectingStatus();
		if (PassiveRpcMode)
		{
			RpcConnectionVerified = false;
			RpcConnectionStatus = "Пассивный режим: запустите ggml-rpc-server вручную.";
			return;
		}

		RpcConnectionVerified = false;
		SetRpcConnectingStatus();
		string endpoint = GetRpcEndpointForStorage();
		if (!string.Equals(RpcEndpoint, endpoint, StringComparison.Ordinal))
		{
			RpcEndpoint = endpoint;
		}
		if (!RpcEnabled || string.IsNullOrWhiteSpace(RpcHost) || RpcPort is < 1 or > 65535)
		{
			SetRpcDisconnectedStatus();
			return;
		}

		// The selected remote RPC process is deliberately restarted on application
		// startup. This removes stale duplicate processes and guarantees that the
		// remote backend matches the selected llama.cpp release.
		_rpcStartupAttempted = true;

		string remoteRpcExecutable = RpcServerPath.Trim();
		if (string.IsNullOrWhiteSpace(remoteRpcExecutable))
		{
			SetRpcDisconnectedStatus();
			return;
		}

		_log.Information($"Starting remote RPC server from selected release: {remoteRpcExecutable}", "Server");
		// A Linux endpoint is addressed with an absolute POSIX path.  It must be
		// launched by the remote shell, while Windows endpoints continue to use the
		// elevated scheduled-task path below.
		if (IsLinuxRpcExecutable(remoteRpcExecutable))
		{
			string rpcCacheArgumentLinux = RpcUseCache ? " --cache" : string.Empty;
			string executable = ShellQuote(remoteRpcExecutable);
			// Do not use "pkill -f <full path>" here: the SSH shell command itself
			// contains that path and would kill itself before it reaches nohup.
			string command =
				"pkill -x ggml-rpc-server 2>/dev/null || true; " +
				$"nohup {executable} --host {ShellQuote(RpcHost)} --port {RpcPort} --device CUDA0{rpcCacheArgumentLinux} " +
				$">/tmp/llama-studio-rpc-{RpcPort}.log 2>&1 </dev/null &";

			if (!await RunSshCommandAsync(command, "Linux RPC command"))
			{
				if (await WaitForRpcEndpointAsync())
				{
					RpcSshConnected = true;
				}
				else
				{
					SetRpcDisconnectedStatus();
				}
				return;
			}

			if (!await WaitForRpcEndpointAsync())
			{
				RpcConnectionVerified = false;
				SetRpcDisconnectedStatus();
			}
			return;
		}

		string rpcCacheArgument = RpcUseCache ? " --cache" : string.Empty;
		string script = $"$exe='{remoteRpcExecutable.Replace("'", "''")}'; " +
			"if (-not (Test-Path -LiteralPath $exe)) { throw 'RPC executable not found: ' + $exe }; " +
			// Serialize remote cleanup/start requests from multiple LlamaStudio copies.
			// The mutex is remote and process-wide, so two UI instances cannot kill and
			// relaunch ggml-rpc-server at the same time.
			"$mutex=New-Object System.Threading.Mutex($false,'Global\\LlamaStudio-RPC-Launch'); if (-not $mutex.WaitOne(0)) { Write-Output 'RPC launch already in progress'; exit 0 }; " +
			$"$listener=Get-NetTCPConnection -LocalPort {RpcPort} -State Listen -ErrorAction SilentlyContinue; if ($listener) {{ Write-Output 'existing RPC listener will be restarted' }}; " +
			"$base=Split-Path -Parent $exe; " +
			"$launchLog=Join-Path $base 'rpc-server.launch.log'; " +
			"$taskName='LlamaStudio-RPC-Server'; " +
			"('[' + (Get-Date -Format o) + '] launch requested: ' + $exe) | Set-Content -LiteralPath $launchLog -Encoding utf8; " +
			"Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue; " +
			"Unregister-ScheduledTask -TaskName $taskName -Confirm:\u0024false -ErrorAction SilentlyContinue; " +
			"Get-Process ggml-rpc-server -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; " +
			"Start-Sleep -Milliseconds 250; " +
			$"$action=New-ScheduledTaskAction -Execute $exe -Argument '--host {RpcHost} --port {RpcPort} --device CUDA0{rpcCacheArgument}'; " +
			"$principal=New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest; " +
			"Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Force | Out-Null; " +
			"Start-ScheduledTask -TaskName $taskName; " +
			$"for (`u0024i=0; `u0024i -lt 80; `u0024i++) {{ if (Get-NetTCPConnection -LocalPort {RpcPort} -State Listen -ErrorAction SilentlyContinue) {{ ('[' + (Get-Date -Format o) + '] RPC port is listening') | Add-Content -LiteralPath `u0024launchLog -Encoding utf8; exit 0 }}; Start-Sleep -Milliseconds 250 }}; " +
			"('[' + (Get-Date -Format o) + '] RPC port did not open after launch') | Add-Content -LiteralPath $launchLog -Encoding utf8; exit 1";
		if (!await RunSshScriptAsync(script))
		{
			// The remote scheduled-task command can report a transient non-zero
			// result while the RPC process is still starting. If the endpoint is
			// already listening, keep the user-facing state as connected instead
			// of exposing the implementation detail as a startup error.
			if (await WaitForRpcEndpointAsync())
			{
				RpcSshConnected = true;
			}
			else
			{
				SetRpcDisconnectedStatus();
			}
			return;
		}

		if (!await WaitForRpcEndpointAsync())
		{
			RpcConnectionVerified = false;
			SetRpcDisconnectedStatus();
		}
	}

	public async Task StopRpcServerAsync()
	{
		if (PassiveRpcMode)
		{
			RpcConnectionVerified = false;
			RpcConnectionStatus = "Пассивный режим: удалённый RPC не управляется LlamaStudio.";
			return;
		}

		if (IsLinuxRpcExecutable(RpcServerPath))
		{
			await RunSshCommandAsync("pkill -x ggml-rpc-server 2>/dev/null || true", "Linux RPC stop");
		}
		else
		{
			await RunSshScriptAsync("Unregister-ScheduledTask -TaskName 'LlamaStudio-RPC-Server' -Confirm:\u0024false -ErrorAction SilentlyContinue; Get-Process ggml-rpc-server -ErrorAction SilentlyContinue | Stop-Process -Force");
		}
		RpcConnectionVerified = false;
		await RefreshGpuAsync();
		await Task.Delay(1000);
		await CheckRpcConnectionAsync();
	}

	private async Task<bool> RunSshScriptAsync(string script)
	{
		string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
		return await RunSshCommandAsync($"powershell -NoProfile -EncodedCommand {encoded}", "Remote RPC command");
	}

	private async Task<bool> RunSshCommandAsync(string commandText, string operation)
	{
		if (string.IsNullOrWhiteSpace(RpcSshUsername) || string.IsNullOrWhiteSpace(RpcSshPassword))
		{
			RpcSshConnected = false;
			SetRpcDisconnectedStatus();
			return false;
		}

		try
		{
			using SshClient client = new SshClient(RpcHost, 22, RpcSshUsername.Trim(), RpcSshPassword);
			client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
			await Task.Run(client.Connect);
			using SshCommand command = client.CreateCommand(commandText);
			string output = await Task.Run(command.Execute);
			if (command.ExitStatus != 0)
			{
				// SSH authentication and transport succeeded. A non-zero remote
				// command means only that RPC management failed; keep SSH green.
				RpcSshConnected = true;
				string details = CleanSshCommandDetails(string.IsNullOrWhiteSpace(command.Error) ? output : command.Error);
				_log.Error($"{operation} failed (exit {command.ExitStatus}): {details}", "Server");
				SetRpcDisconnectedStatus();
				return false;
			}
			client.Disconnect();
			RpcSshConnected = true;
			return true;
		}
		catch (Exception ex)
		{
			RpcSshConnected = false;
			_log.Error(ex, $"{operation} failed", "Server");
			SetRpcDisconnectedStatus();
			return false;
		}
	}

	private static bool IsLinuxRpcExecutable(string? executable)
	{
		return !string.IsNullOrWhiteSpace(executable) && executable.TrimStart().StartsWith("/", StringComparison.Ordinal);
	}

	private static string ShellQuote(string value)
	{
		return "'" + value.Replace("'", "'\\\"'\\\"'") + "'";
	}

	private static string CleanSshCommandDetails(string details)
	{
		if (string.IsNullOrWhiteSpace(details))
		{
			return string.Empty;
		}

		int xmlStart = details.IndexOf("#< CLIXML", StringComparison.OrdinalIgnoreCase);
		if (xmlStart < 0)
		{
			return details.Trim();
		}

      string xml = details[(xmlStart + "#< CLIXML".Length)..].Trim();
		try
		{
			var document = XDocument.Parse(xml);
			string[] messages = document.Descendants("AV")
				.Select(node => node.Value.Trim())
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.Ordinal)
				.ToArray();
			return messages.Length == 0
				? "Удалённый PowerShell вернул служебный CLIXML без текста ошибки."
				: string.Join(" ", messages);
		}
		catch
		{
			return "Удалённый PowerShell вернул служебный CLIXML; подробный текст недоступен.";
		}
	}

	private void UpdateRpcEndpointFromParts()
	{
		if (string.IsNullOrWhiteSpace(RpcHost) || RpcPort is < 1 or > 65535)
		{
			return;
		}
		string endpoint = $"{RpcHost}:{RpcPort}";
		if (!string.Equals(_rpcEndpoint, endpoint, StringComparison.Ordinal))
		{
			_rpcEndpoint = endpoint;
			OnPropertyChanged("RpcEndpoint");
		}
	}

	private string GetRpcEndpointForStorage()
	{
		string endpoint = RpcEndpoint.Trim();
		int separator = endpoint.LastIndexOf(':');
		if (separator > 0 && separator < endpoint.Length - 1 && int.TryParse(endpoint[(separator + 1)..], out int endpointPort) && endpointPort is >= 1 and <= 65535)
		{
			return endpoint;
		}

		return !string.IsNullOrWhiteSpace(RpcHost) && RpcPort is >= 1 and <= 65535
			? $"{RpcHost.Trim()}:{RpcPort}"
			: string.Empty;
	}

	private void ParseRpcEndpointParts(string endpoint)
	{
		int separator = endpoint.LastIndexOf(':');
		if (separator > 0 && int.TryParse(endpoint[(separator + 1)..], out int port))
		{
			_rpcHost = endpoint[..separator].Trim();
			_rpcPort = Math.Clamp(port, 1, 65535);
			OnPropertyChanged("RpcHost");
			OnPropertyChanged("RpcPort");
		}
	}

	public double RamPercent => s_totalRamGb > 0.0 ? Math.Clamp(RamUsedGb / s_totalRamGb * 100.0, 0.0, 100.0) : 0.0;

	private static bool HasMissingModel(ServerProfile profile)
	{
		return !string.IsNullOrWhiteSpace(profile.ModelPath) && !File.Exists(profile.ModelPath);
	}

	public void SetGpuSelected(int index, bool isSelected)
	{
		GpuInfo gpu = GpuInfos.FirstOrDefault((GpuInfo gpuInfo) => gpuInfo.Index == index);
		if (gpu == null)
		{
			return;
		}
		if (!isSelected && GpuInfos.Count((GpuInfo gpuInfo) => gpuInfo.IsSelected) <= 1)
		{
			return;
		}
			gpu.IsSelected = isSelected;
		int[] presentSelected = GpuInfos.Where(gpuInfo => gpuInfo.IsSelected && gpuInfo.Index >= 0).Select(gpuInfo => gpuInfo.Index).ToArray();
		_pendingGpuSelection = _pendingGpuSelection
			.Where(index => !GpuInfos.Any(gpuInfo => gpuInfo.Index == index))
			.Concat(presentSelected)
			.Distinct()
			.ToArray();
		if (isSelected && !gpu.IsRemote && MainGpu < 0)
		{
			MainGpu = index;
		}
		if (!isSelected && MainGpu == index)
		{
			MainGpu = GpuInfos.FirstOrDefault((GpuInfo gpuInfo) => gpuInfo.IsSelected)?.Index ?? -1;
		}
		OnPropertyChanged("SelectedGpuDeviceText");
		OnPropertyChanged("RecommendedTensorSplitText");
		// r94: the card set changed — the stored tensor split is for the OLD set
		// (e.g. "33,33,34" for 3 cards). Recalculate it now, otherwise the command
		// keeps the stale -ts until the user manually flips the split mode, and the
		// profile saves the mismatched pair (2 cards + 3-way -ts).
		if (GpuSplitMode == GpuSplitMode.Tensor)
		{
			int selectedCount = GetOrderedSelectedGpus().Count;
			if (selectedCount <= 1)
			{
				TensorSplit = string.Empty;
			}
			else
			{
				int splitCount = TensorSplit.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
				if (splitCount != selectedCount)
				{
					TensorSplit = string.Join(",", CalculateRecommendedSplit(GetOrderedSelectedGpus()));
				}
			}
		}
		AutoSaveProfile();
		UpdateCommandLinePreview();
	}

	public void SetMainGpuFromSelection(int index)
	{
		if (GpuInfos.Any((GpuInfo gpu) => gpu.Index == index && gpu.IsSelected && !gpu.IsRemote))
		{
			MainGpu = index;
			AutoSaveProfile();
			UpdateCommandLinePreview();
		}
	}

	private void SaveProfileNow()
	{
		if (SelectedProfile != null && !_isApplyingProfile)
		{
			SyncSettingsToProfile();
			_profileManager.SaveProfileNow(SelectedProfile);
		}
	}

	public void ApplyAutomaticTensorSplit()
	{
		List<GpuInfo> selected = GpuInfos.Where((GpuInfo gpu) => gpu.IsSelected).ToList();
		if (selected.Count > 1)
		{
			// Let llama.cpp choose the split from the actual device memory and
			// fit settings. An explicit tensor split overrides that logic.
			TensorSplit = string.Empty;
			SaveProfileNow();
			UpdateCommandLinePreview();
		}
	}

	public void SetTensorSplitFromSlider(int firstGpuPercent)
	{
		List<GpuInfo> selected = GetOrderedSelectedGpus();
		if (selected.Count == 2)
		{
			int first = Math.Max(1, Math.Min(99, firstGpuPercent));
			TensorSplit = $"{first},{100 - first}";
			SaveProfileNow();
			UpdateCommandLinePreview();
		}
	}

	public void SetGpuSplitMode(GpuSplitMode mode)
	{
		GpuSplitMode = mode == GpuSplitMode.Tensor ? GpuSplitMode.Tensor : GpuSplitMode.Layer;
		// r68: tensor split cannot run with Auto (empty) GPU layers — llama.cpp
		// fails in llama_params_fit. Auto-promote to All.
		if (GpuSplitMode == GpuSplitMode.Tensor && (string.IsNullOrWhiteSpace(GpuLayers) || GpuLayers == "-1"))
		{
			GpuLayers = "all";
		}
		if (GpuSplitMode == GpuSplitMode.Tensor && GetOrderedSelectedGpus().Count > 1)
		{
			int selectedCount = GetOrderedSelectedGpus().Count;
			int splitCount = TensorSplit.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
			// r94: empty (fill it) OR stale (parts for a different card count — a
			// profile saved before the selection was changed).
			if (string.IsNullOrWhiteSpace(TensorSplit) || splitCount != selectedCount)
			{
				TensorSplit = string.Join(",", CalculateRecommendedSplit(GetOrderedSelectedGpus()));
			}
		}
		if (GpuSplitMode != GpuSplitMode.Tensor)
		{
			TensorSplit = string.Empty;
		}
		SaveProfileNow();
		UpdateCommandLinePreview();
	}

	public void SelectGpuDistributionMode(GpuSplitMode mode) => SetGpuSplitMode(mode);

	// The order here is the exact --device order. Tensor split entries must
	// follow it, including a remote RPC device appended after local devices.
	public List<GpuInfo> GetOrderedSelectedGpus() => GpuInfos.Where(gpu => gpu.IsSelected).ToList();

	public int[] GetEffectiveSplit()
	{
		List<GpuInfo> selected = GetOrderedSelectedGpus();
		if (selected.Count == 0) return Array.Empty<int>();
		try
		{
			int[] parsed = TensorSplit.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
			if (parsed.Length == selected.Count && parsed.All(value => value >= 0) && parsed.Sum() == 100) return parsed;
		}
		catch (FormatException) { }
		return CalculateRecommendedSplit(selected);
	}

	// Changes one selected device and shares the opposite change among all
	// other selected devices. Thus 3, 4 and more GPUs always total exactly 100%.
	public void AdjustTensorSplit(int position, int delta)
	{
		List<GpuInfo> selected = GetOrderedSelectedGpus();
		if (selected.Count < 2 || position < 0 || position >= selected.Count) return;
		TensorSplit = string.Join(",", ProportionalAdjust(GetEffectiveSplit(), position, delta));
		SaveProfileNow();
		UpdateCommandLinePreview();
	}

	public void ApplyVramTensorSplit()
	{
		List<GpuInfo> selected = GetOrderedSelectedGpus();
		if (selected.Count < 2) return;
		TensorSplit = string.Join(",", CalculateRecommendedSplit(selected));
		SaveProfileNow();
		UpdateCommandLinePreview();
	}

	public void ApplyEqualTensorSplit()
	{
		int count = GetOrderedSelectedGpus().Count;
		if (count < 2) return;
		int baseShare = 100 / count;
		int remainder = 100 % count;
		TensorSplit = string.Join(",", Enumerable.Range(0, count).Select(index => baseShare + (index < remainder ? 1 : 0)));
		SaveProfileNow();
		UpdateCommandLinePreview();
	}

	private static int[] ProportionalAdjust(int[] values, int position, int delta)
	{
		int[] result = (int[])values.Clone();
		int target = Math.Max(0, Math.Min(100, result[position] + delta));
		int applied = target - result[position];
		if (applied == 0) return result;
		int otherTotal = result.Where((_, index) => index != position).Sum();
		if (otherTotal <= 0) return result;
		result[position] = target;
		int total = target;
		for (int i = 0; i < result.Length; i++)
		{
			if (i == position) continue;
			result[i] = Math.Max(0, (int)Math.Round(values[i] - applied * ((double)values[i] / otherTotal), MidpointRounding.AwayFromZero));
			total += result[i];
		}
		int correction = 100 - total;
		int largestOther = Enumerable.Range(0, result.Length).Where(i => i != position).OrderByDescending(i => result[i]).First();
		result[largestOther] += correction;
		return result;
	}

	private void ApplyGpuSelection()
	{
		if (_gpuInfos.Count == 0)
		{
			return;
		}
		HashSet<int> requested = new HashSet<int>(_pendingGpuSelection);
		bool useAll = requested.Count == 0;
		foreach (GpuInfo gpu in _gpuInfos)
		{
		gpu.IsSelected = useAll || requested.Contains(gpu.Index);
		}
		if (MainGpu >= 0 && !_gpuInfos.Any((GpuInfo gpu) => gpu.Index == MainGpu && gpu.IsSelected && !gpu.IsRemote))
		{
			MainGpu = _gpuInfos.FirstOrDefault((GpuInfo gpu) => gpu.IsSelected && !gpu.IsRemote)?.Index ?? -1;
		}
		OnPropertyChanged("SelectedGpuDeviceText");
		OnPropertyChanged("RecommendedTensorSplitText");
	}

	private static int[] CalculateRecommendedSplit(IReadOnlyList<GpuInfo> selected)
	{
		if (selected.Count == 0)
		{
			return Array.Empty<int>();
		}
		if (selected.Count == 1)
		{
			return new[] { 100 };
		}
		// Tensor split is a memory-placement setting, not a performance
		// benchmark. Weighting by PerformanceScore can put too much of a large
		// model on the faster card and leave no room for KV cache or context.
		// Reserve a small amount on every card for runtime allocations.
		const double reservedVramGb = 2.0;
		double total = selected.Sum((GpuInfo gpu) => Math.Max(1.0, gpu.MemoryTotalGb - reservedVramGb));
		int[] result = selected.Select((GpuInfo gpu) => (int)Math.Round(Math.Max(1.0, gpu.MemoryTotalGb - reservedVramGb) / total * 100.0)).ToArray();
		result[result.Length - 1] += 100 - result.Sum();
		return result;
	}

	[RelayCommand]
	private async Task BrowseModel()
	{
		string selectedPath = await _dialog.SelectFileAsync("Select GGUF Model", ModelPath, "GGUF Files|*.gguf|All Files|*.*");
		if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
		{
			ModelPath = selectedPath;
			UpdateDisplayNames();
			_log.Information("Model selected: " + selectedPath, "Server");
		}
	}

	[RelayCommand]
	private async Task BrowseMmproj()
	{
		string selectedPath = await _dialog.SelectFileAsync("Select mmproj Model", MmprojPath, "GGUF Files|*.gguf|All Files|*.*");
		if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
		{
			MmprojPath = selectedPath;
			UpdateDisplayNames();
			_log.Information("mmproj selected: " + selectedPath, "Server");
		}
	}

	[RelayCommand]
	private async Task BrowseDraftModel()
	{
		string selectedPath = await _dialog.SelectFileAsync("Select Draft Model", DraftModelPath, "GGUF Files|*.gguf|All Files|*.*");
		if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
		{
			DraftModelPath = selectedPath;
			UpdateDisplayNames();
			_log.Information("Draft model selected: " + selectedPath, "Server");
		}
	}

	[RelayCommand]
	private async Task QuickScanModels()
	{
		string modelsDirectory = (string.IsNullOrWhiteSpace(_settings.ModelsDirectory) ? _settings.LlamaCppDirectory : _settings.ModelsDirectory);
		if (!Directory.Exists(modelsDirectory))
		{
			string selectedDirectory = await _dialog.SelectFolderAsync("Select Models Directory", modelsDirectory);
			if (string.IsNullOrEmpty(selectedDirectory) || !Directory.Exists(selectedDirectory))
			{
				return;
			}
			modelsDirectory = selectedDirectory;
		}
		IsScanningModels = true;
		ScannedModels.Clear();
		try
		{
			List<GgufModelInfo> foundModels = await _scanner.ScanDirectoryAsync(modelsDirectory);
			foreach (GgufModelInfo model in foundModels)
			{
				ScannedModels.Add(model);
			}
			_log.Information($"Quick scan: {foundModels.Count} models found", "Server");
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Quick scan failed", "Server");
		}
		finally
		{
			IsScanningModels = false;
		}
	}

	[RelayCommand]
	private void QuickSelectModel()
	{
		if (QuickSelectedModel != null)
		{
			ModelPath = QuickSelectedModel.Path;
			UpdateDisplayNames();
			_log.Information("Quick-selected model: " + QuickSelectedModel.FileName, "Server");
		}
	}

	[RelayCommand]
	private void QuickSelectMmproj()
	{
		if (QuickSelectedModel != null)
		{
			MmprojPath = QuickSelectedModel.Path;
			UpdateDisplayNames();
			_log.Information("Quick-selected mmproj: " + QuickSelectedModel.FileName, "Server");
		}
	}

	[RelayCommand]
	private void QuickSelectDraft()
	{
		if (QuickSelectedModel != null)
		{
			DraftModelPath = QuickSelectedModel.Path;
			UpdateDisplayNames();
			_log.Information("Quick-selected draft: " + QuickSelectedModel.FileName, "Server");
		}
	}

	[RelayCommand]
	public async Task StartServerAsync()
	{
		// r105: never start the server while a Benchmark test is running —
		// ENGINE-тест остановил сервер намеренно и вернёт его сам после теста.
		if (BenchmarkCenterViewModel.Current is { IsRunning: true })
		{
			ErrorMessage = "Идёт тест Benchmark (llama-bench) — сервер запустится автоматически после завершения теста. Остановите тест кнопкой «■ Стоп» на вкладке «Мониторинг», если нужно запустить сервер сейчас.";
			_log.Error("StartServer blocked: benchmark test in progress", "Server");
			State = ServerState.Error;
			return;
		}
		if (SelectedProfile == null)
		{
			ErrorMessage = _loc.T("server.no_profile_selected");
			State = ServerState.Error;
			return;
		}
		if (string.IsNullOrWhiteSpace(ModelPath))
		{
			ErrorMessage = _loc.T("server.no_model_selected");
			_log.Error(ErrorMessage, "Server");
			State = ServerState.Error;
			return;
		}
		string validationError = ValidateCustomArguments();
		if (validationError != null)
		{
			ErrorMessage = validationError;
			_log.Error(validationError, "Server");
			State = ServerState.Error;
			return;
		}
		string executablePath = ExecutablePath;
		if (!File.Exists(executablePath))
		{
			ErrorMessage = string.Format(_loc.T("server.exe_not_found"), executablePath);
			_log.Error(ErrorMessage, "Server");
			State = ServerState.Error;
			return;
		}
		SyncSettingsToProfile();
		SetCommandLinePreview(BuildCommandLinePreview(SelectedProfile));
		try
		{
			// RPC must be ready before llama-server starts loading the model. If
			// the local server starts first, its initial RPC allocation can race
			// the remote process and make the RPC backend drop the connection.
			if (RpcEnabled && !PassiveRpcMode)
			{
				// Application startup may still be restarting the remote RPC process.
				// Do not race it with a local llama-server launch.
				if (_rpcStartupTask != null)
				{
					try
					{
						await _rpcStartupTask;
					}
					catch (Exception ex)
					{
						_log.Error(ex, "RPC startup task failed before local server start.", "Server");
					}
					finally
					{
						if (_rpcStartupTask.IsCompleted)
						{
							_rpcStartupTask = null;
						}
					}
				}
				if (!await EnsureRpcReadyForLlamaServerAsync())
				{
					ErrorMessage = RpcConnectionStatus;
					_log.Error("RPC server is not ready; local llama-server start was cancelled.", "Server");
					State = ServerState.Error;
					return;
				}
			}
			_log.Information("Starting server with profile: " + SelectedProfile.Name, "Server");
			await _serverManager.StartAsync(SelectedProfile);
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			_log.Error(ex, "Failed to start server", "Server");
			State = ServerState.Error;
		}
	}

	[RelayCommand]
	private async Task StopServer()
	{
		try
		{
			_log.Information("Stopping server...", "Server");
			await _serverManager.StopAsync();
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			_log.Error(ex, "Failed to stop server", "Server");
			State = ServerState.Error;
		}
	}

	[RelayCommand]
	private async Task MeasureSpeed()
	{
		await _monitoringVm.CheckSpeedCommand.ExecuteAsync(null);
	}

	[RelayCommand]
	private async Task CreateNewProfile()
	{
		string name = await _dialog.ShowInputAsync(_loc.T("dialog.create_profile_title"), _loc.T("dialog.profile_name_prompt"), "New Profile");
		if (string.IsNullOrWhiteSpace(name))
		{
			return;
		}
		name = name.Trim();
		if (Profiles.Any((ServerProfile p) => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
			return;
		}
		ServerProfile profile = _profileManager.CreateProfile(name);
		await _profileManager.SaveProfileAsync(profile);
		LoadProfiles();
		SelectedProfile = Profiles.FirstOrDefault((ServerProfile p) => p.Id == profile.Id);
		_log.Information("Created profile: " + profile.Name, "Server");
	}

	[RelayCommand]
	private async Task SaveToProfile()
	{
		if (SelectedProfile == null)
		{
			_log.Error("No profile selected to save settings.", "Server");
			return;
		}
		SyncSettingsToProfile();
		await _profileManager.SaveProfileAsync(SelectedProfile);
		UpdateCommandLinePreview();
		_log.Information("Settings saved to profile: " + SelectedProfile.Name, "Server");
	}

	[RelayCommand]
	private async Task RenameProfile()
	{
		if (SelectedProfile == null)
		{
			await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
			return;
		}
		string name = await _dialog.ShowInputAsync(_loc.T("dialog.rename_title"), _loc.T("dialog.rename_prompt"), SelectedProfile.Name);
		if (!string.IsNullOrWhiteSpace(name))
		{
			name = name.Trim();
			if (Profiles.Any((ServerProfile p) => p.Id != SelectedProfile.Id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
			{
				await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
				return;
			}
			SelectedProfile.Name = name;
			await _profileManager.SaveProfileAsync(SelectedProfile);
			LoadProfiles();
			_log.Information("Renamed profile to: " + name, "Server");
		}
	}

	[RelayCommand]
	private async Task DeleteProfile()
	{
		if (SelectedProfile == null)
		{
			await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
		}
		else if (await _dialog.ShowConfirmationAsync(string.Format(_loc.T("dialog.confirm_delete_profile"), SelectedProfile.Name), _loc.T("dialog.delete_profile_title")))
		{
			await _profileManager.DeleteProfileAsync(SelectedProfile.Id);
			LoadProfiles();
			_log.Information("Deleted profile: " + SelectedProfile.Name, "Server");
		}
	}

	[RelayCommand]
	public async Task SetDefaultProfile()
	{
		if (SelectedProfile == null)
		{
			await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
			return;
		}
		await _profileManager.SetDefaultProfileAsync(SelectedProfile.Id);
		LoadProfiles();
		_log.Information("Set default profile: " + SelectedProfile.Name, "Server");
	}

	[RelayCommand]
	private async Task CopyFromProfile()
	{
		if (SelectedProfile == null)
		{
			await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
			return;
		}
		// r94: hidden profiles (model file removed) must be available as COPY
		// SOURCES — e.g. migrating a setup to a newer, bigger model file.
		// The regular profile selector keeps them hidden (LoadProfiles).
		List<ServerProfile> others = _profileManager.GetAllProfiles().Where((ServerProfile p) => p.Id != SelectedProfile.Id).ToList();
		if (others.Count == 0)
		{
			await _dialog.ShowErrorAsync(_loc.T("dialog.no_other_profiles"));
			return;
		}
		string Decorate(ServerProfile p) => HasMissingModel(p) ? p.Name + " (файлы модели удалены)" : p.Name;
		string selectedName = await _dialog.ShowProfileSelectAsync(_loc.T("dialog.copy_settings_title"), string.Format(_loc.T("server.copy_to_profile_fmt"), SelectedProfile.Name), others.Select(Decorate));
		if (selectedName != null)
		{
			ServerProfile serverProfile = others.FirstOrDefault((ServerProfile p) => Decorate(p) == selectedName);
			if (serverProfile != null)
			{
				// The source model file no longer exists: keep the target's own
				// model/mmproj/draft paths instead of pointing them at missing files.
				bool sourceModelMissing = HasMissingModel(serverProfile);
				string keepModel = sourceModelMissing ? ModelPath : string.Empty;
				string keepMmproj = sourceModelMissing ? MmprojPath : string.Empty;
				string keepDraft = sourceModelMissing ? DraftModelPath : string.Empty;
				ApplyProfileToSettings(serverProfile);
				if (sourceModelMissing)
				{
					ModelPath = keepModel;
					MmprojPath = keepMmproj;
					DraftModelPath = keepDraft;
					UpdateDisplayNames();
				}
				UpdateCommandLinePreview();
				_log.Information($"Copied settings from \"{serverProfile.Name}\" to \"{SelectedProfile.Name}\"", "Server");
				await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.copy_success"), serverProfile.Name));
			}
		}
	}

	[RelayCommand]
	private async Task CheckLlamaCppVersion()
	{
		if (!ExecutableFound)
		{
			LlamaVersionInfo = _loc.T("server.exe_not_found_short");
			CudaInfo = string.Empty;
			UnsupportedFlags.Clear();
			_helpInfo = null;
			return;
		}
		try
		{
			// r42: include recent prerelease builds (llama.cpp ships b#### as prerelease).
			List<LlamaCppRelease> releases = await _updater.FetchReleasesAsync(true);
			string installedVersion = DetectInstalledVersion();
			LlamaVersionInfo = installedVersion ?? _loc.T("server.unknown_version");
			if (releases != null && releases.Count > 0)
			{
				string tagName = releases[0].TagName;
				if (installedVersion != null && installedVersion != tagName)
				{
					LlamaVersionInfo = LlamaVersionInfo + " (latest: " + tagName + ")";
				}
			}
			CudaInfo = DetectCudaSupport();
			await ParseHelpAsync();
		}
		catch (Exception ex)
		{
			LlamaVersionInfo = string.Format(_loc.T("server.version_error"), ex.Message);
			_log.Error(ex, "Failed to check llama.cpp version", "Server");
		}
	}

	private async Task ParseHelpAsync()
	{
		if (!ExecutableFound)
		{
			return;
		}
		try
		{
			LlamaHelpInfo llamaHelpInfo = await _helpParser.ParseAsync(ExecutablePath);
			if (llamaHelpInfo != null)
			{
				_helpInfo = llamaHelpInfo;
				OnPropertyChanged(nameof(SpecTypeOptions));
				UpdateUnsupportedFlags();
				_log.Debug($"Help parsed: {llamaHelpInfo.SupportedFlags.Count} flags detected", "Server");
			}
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to parse --help output: " + ex.Message, "Server");
		}
	}

	private void UpdateUnsupportedFlags()
	{
		if (_helpInfo == null)
		{
			UnsupportedFlags.Clear();
			return;
		}
		List<string> usedFlags = new List<string>();
		if (!string.IsNullOrWhiteSpace(ModelPath))
		{
			usedFlags.Add("--model");
		}
		if (!string.IsNullOrWhiteSpace(MmprojPath))
		{
			usedFlags.Add("--mmproj");
		}
		if (!string.IsNullOrWhiteSpace(DraftModelPath) || SpeculativeDecoding)
		{
			usedFlags.Add("--draft-model");
		}
		usedFlags.Add("--host");
		usedFlags.Add("--port");
		usedFlags.Add("-to");
		if (Slots > 0)
		{
			usedFlags.Add("-np");
		}
		if (!string.IsNullOrWhiteSpace(GpuLayers))
		{
			usedFlags.Add("-ngl");
		}
		if (Threads > 0)
		{
			usedFlags.Add("-t");
		}
		if (ThreadsBatch > 0)
		{
			usedFlags.Add("--threads-batch");
		}
		if (MainGpu > 0)
		{
			usedFlags.Add("--main-gpu");
		}
		if (FlashAttention)
		{
			usedFlags.Add("--flash-attn");
		}
		if (!Mmap)
		{
			usedFlags.Add("--no-mmap");
		}
		if (Mlock)
		{
			usedFlags.Add("--mlock");
		}
		if (!KvOffloadEnabled)
		{
			usedFlags.Add("--no-kv-offload");
		}
		if (ContextSize > 0)
		{
			usedFlags.Add("-c");
		}
		if (BatchSize != 2048)
		{
			usedFlags.Add("-b");
		}
		if (UbatchSize != 512)
		{
			usedFlags.Add("--ubatch");
		}
		if (MaxTokens > 0)
		{
			usedFlags.Add("-n");
		}
		if (CacheTypeK != "f16")
		{
			usedFlags.Add("--cache-type-k " + CacheTypeK);
		}
		if (CacheTypeV != "f16")
		{
			usedFlags.Add("--cache-type-v " + CacheTypeV);
		}
		if (CacheRam >= 0)
		{
			usedFlags.Add($"--cache-ram {CacheRam}");
		}
		if (CacheReuse != 32)
		{
			usedFlags.Add($"--cache-reuse {CacheReuse}");
		}
		if (Math.Abs(Temperature - 0.8f) > 0.001f)
		{
			usedFlags.Add("--temp");
		}
		if (TopK != 40)
		{
			usedFlags.Add("--top-k");
		}
		if (Math.Abs(TopP - 0.95f) > 0.001f)
		{
			usedFlags.Add("--top-p");
		}
		if (MinP != 0.05f)
		{
			usedFlags.Add("--min-p");
		}
		if (TypicalP < 1f)
		{
			usedFlags.Add("--typical-p");
		}
		if (RepeatPenalty != 1.1f)
		{
			usedFlags.Add("--repeat-penalty");
		}
		if (RepeatLastN != -1)
		{
			usedFlags.Add("--repeat-last-n");
		}
		if (PresencePenalty != 0f)
		{
			usedFlags.Add("--presence-penalty");
		}
		if (FrequencyPenalty != 0f)
		{
			usedFlags.Add("--frequency-penalty");
		}
		if (MirostatMode > MirostatMode.Disabled)
		{
			usedFlags.Add("--mirostat");
		}
		if (MirostatTau != 5f)
		{
			usedFlags.Add("--mirostat-tau");
		}
		if (MirostatEta != 0.1f)
		{
			usedFlags.Add("--mirostat-learn-rate");
		}
		if (DryMultiplier > 0f)
		{
			usedFlags.Add("--dry-multiplier");
		}
		if (Math.Abs(DryBase - 1.75f) > 0.001f)
		{
			usedFlags.Add("--dry-base");
		}
		if (DynatempStddev > 0f)
		{
			usedFlags.Add("--dynatemp-range");
		}
		if (XtcProbability > 0f)
		{
			usedFlags.Add("--xtc-probability");
		}
		if (Math.Abs(XtcThreshold - 0.1f) > 0.001f)
		{
			usedFlags.Add("--xtc-threshold");
		}
		if (!string.IsNullOrWhiteSpace(SpecType) && !SpecType.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			usedFlags.Add("--spec-type");
		}
		if (PredictCount > 0)
		{
			usedFlags.Add("--predict");
		}
		if (!string.IsNullOrWhiteSpace(SpecDraftGpuLayers))
		{
			usedFlags.Add("-ngld");
		}
		if (SpecDraftNMax != 3)
		{
			usedFlags.Add("--spec-draft-n-max");
		}
		if (SpecDraftNMin > 0)
		{
			usedFlags.Add("--spec-draft-n-min");
		}
		if (!string.IsNullOrWhiteSpace(RopeScaling))
		{
			usedFlags.Add("--rope-scaling");
		}
		if (RopeFreqBase.HasValue)
		{
			usedFlags.Add("--rope-frequency-base");
		}
		if (RopeFreqScale.HasValue)
		{
			usedFlags.Add("--rope-frequency-scale");
		}
		if (YarnOriginalContext.HasValue)
		{
			usedFlags.Add("--yarn-orig-ctx");
		}
		if (YarnExtFactor.HasValue)
		{
			usedFlags.Add("--yarn-ext-factor");
		}
		if (YarnAttnFactor.HasValue)
		{
			usedFlags.Add("--yarn-attn-factor");
		}
		if (YarnBetaFast.HasValue)
		{
			usedFlags.Add("--yarn-beta-fast");
		}
		if (YarnBetaSlow.HasValue)
		{
			usedFlags.Add("--yarn-beta-slow");
		}
		if (Numa)
		{
			usedFlags.Add("--numa");
		}
		if (!CachePrompt)
		{
			usedFlags.Add("--cache-prompt");
		}
		if (!ContBatching)
		{
			usedFlags.Add("--cont-batching");
		}
		if (VerboseLogging)
		{
			usedFlags.Add("--verbose");
		}
		if (EnableWebUI)
		{
			usedFlags.Add("--ui");
		}
		if (EnableMetrics)
		{
			usedFlags.Add("--metrics");
		}
		if (Reasoning)
		{
			usedFlags.Add("-rea");
		}
		if (EmbeddingMode)
		{
			usedFlags.Add("--embedding");
		}
		UnsupportedFlags = _helpInfo.GetUnsupported(usedFlags);
		if (UnsupportedFlags.Count > 0)
		{
			_log.Debug("Unsupported flags: " + string.Join(", ", UnsupportedFlags), "Server");
		}
	}

	private void OnCliChangesDetected(object? sender, CliChangeReport report)
	{
		Dispatcher.UIThread.Post(delegate
		{
			CliChangeReport = report;
			List<string> summaryParts = new List<string> { "Version: " + report.OldVersion + " → " + report.NewVersion };
			if (report.HasCriticalChanges)
			{
				summaryParts.Add($"⚠ Critical: {report.CriticalChanges.Count} used flags removed!");
			}
			if (report.RemovedFlags.Count > 0)
			{
				summaryParts.Add($"Removed: {report.RemovedFlags.Count}");
			}
			if (report.ModifiedFlags.Count > 0)
			{
				summaryParts.Add($"Changed defaults: {report.ModifiedFlags.Count}");
			}
			if (report.AddedFlags.Count > 0)
			{
				summaryParts.Add($"+ New available: {report.AddedFlags.Count}");
			}
			ValidationSummary = " | " + string.Join(" | ", summaryParts);
			_log.Information("CLI changes detected: " + ValidationSummary, "Server");
		});
	}

	[RelayCommand]
	private async Task ValidateCliAsync()
	{
		if (!ExecutableFound)
		{
			_log.Warning("Cannot validate: llama-server.exe not found", "Server");
			return;
		}
		IsCliValidating = true;
		ValidationSummary = "Validating...";
		try
		{
			string version = _settings.ActiveLlamaCppVersion ?? "unknown";
			ValidationReport validationReport = (ValidationReport = await _cliValidator.ValidateAllFlagsAsync(ExecutablePath, version));
			if (validationReport.Status == ValidationStatus.Ok)
			{
				ValidationSummary = _loc.T("server.cli_all_valid");
				ValidationDetails = ((validationReport.NewAvailableFlags.Count > 0) ? string.Format(_loc.T("server.cli_new_flags_fmt"), validationReport.NewAvailableFlags.Count) : string.Empty);
				ValidationSummaryForeground = new SolidColorBrush(Color.Parse("#34D399"));
			}
			else
			{
				ValidationSummary = string.Format(_loc.T("server.cli_unsupported_fmt"), validationReport.RemovedFlags.Count);
				List<string> detailLines = new List<string>();
				foreach (RemovedFlag removedFlag in validationReport.RemovedFlags)
				{
					if (!string.IsNullOrEmpty(removedFlag.SuggestedReplacement))
					{
						detailLines.Add(string.Format(_loc.T("server.cli_replace_with_fmt"), removedFlag.OurFlag, removedFlag.SuggestedReplacement));
					}
					else
					{
						detailLines.Add(string.Format(_loc.T("server.cli_removed_fmt"), removedFlag.OurFlag));
					}
				}
				if (validationReport.NewAvailableFlags.Count > 0)
				{
					detailLines.Add(string.Format(_loc.T("server.cli_new_available_fmt"), validationReport.NewAvailableFlags.Count));
				}
				ValidationDetails = string.Join("\n", detailLines);
				ValidationSummaryForeground = new SolidColorBrush(Color.Parse("#F87171"));
			}
			_log.Information("CLI validation: " + ValidationSummary, "Server");
		}
		catch (Exception ex)
		{
			ValidationSummary = "Error: " + ex.Message;
			ValidationSummaryForeground = new SolidColorBrush(Color.Parse("#F87171"));
			_log.Error(ex, "CLI validation failed", "Server");
		}
		finally
		{
			IsCliValidating = false;
		}
	}

	[RelayCommand]
	private async Task SelectLlamaCppDirectory()
	{
		string path = await _dialog.SelectFolderAsync("Select llama.cpp Base Directory", _settings.LlamaCppBaseDirectory);
		if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
		{
			string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string rootPath = (Path.GetPathRoot(fullPath) ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase))
			{
				await _dialog.ShowErrorAsync("Выберите папку с версиями llama.cpp, а не корень диска.");
				return;
			}
			_settings.LlamaCppBaseDirectory = path;
			LlamaCppDirectory = path;
			await _settings.SaveAsync();
			_versionCache.Clear();
			await _versionCache.ScanAsync(path);
			ScanServersAsync();
			UpdateExecutablePath();
			OnPropertyChanged("LlamaCppDirectoryDisplay");
			_log.Information("llama.cpp base directory set: " + path, "Server");
		}
	}

	[RelayCommand]
	private void ScanServersAsync()
	{
		AvailableServers.Clear();
		IReadOnlyList<VersionInfo> all = _versionCache.GetAll();
		foreach (VersionInfo version in all)
		{
			if (Directory.Exists(version.Directory) && File.Exists(Path.Combine(version.Directory, LlamaServerExecutableName)))
			{
				AvailableServers.Add(version.Display);
			}
		}
		if (!string.IsNullOrEmpty(_settings.ActiveLlamaCppVersion))
		{
			VersionInfo versionInfo = all.FirstOrDefault((VersionInfo v) => v.VersionKey == _settings.ActiveLlamaCppVersion);
			if (versionInfo != null)
			{
				SelectedServer = versionInfo.Display;
			}
		}
		else if (AvailableServers.Count == 1)
		{
			SelectServer(AvailableServers[0]);
		}
	}

	[RelayCommand]
	private async Task SelectServer(string serverName)
	{
		if (string.IsNullOrWhiteSpace(serverName))
		{
			return;
		}
		IReadOnlyList<VersionInfo> all = _versionCache.GetAll();
		VersionInfo versionInfo = all.FirstOrDefault((VersionInfo v) => v.Display == serverName);
		if (versionInfo == null)
		{
			string version = serverName.Split('(')[0].Trim();
			versionInfo = all.FirstOrDefault((VersionInfo v) => v.Version == version);
		}
		if (versionInfo != null && Directory.Exists(versionInfo.Directory))
		{
			bool changed = !string.Equals(_settings.ActiveLlamaCppVersion, versionInfo.VersionKey, StringComparison.OrdinalIgnoreCase);
			// SettingsChanged refreshes the selector after another page changes the
			// active build. Do not save the same version again: that would emit
			// another SettingsChanged event and create a selection loop.
			if (!changed)
			{
				UpdateExecutablePath();
				return;
			}
			ServerStatus status = await _serverManager.GetStatusAsync();
			bool wasRunning = status.State == ServerState.Running || status.State == ServerState.Starting;
			if (changed && wasRunning)
			{
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
			_settings.ActiveLlamaCppVersion = versionInfo.VersionKey;
			_settings.LlamaCppDirectory = versionInfo.Directory;
			await _settings.SaveAsync();
			UpdateExecutablePath();
			if (SelectedProfile != null)
			{
				SelectedProfile.SelectedServerVersion = versionInfo.VersionKey;
				SyncSettingsToProfile();
				_profileManager.SaveProfileNow(SelectedProfile);
				SetCommandLinePreview(BuildCommandLinePreview(SelectedProfile));
				if (changed && wasRunning && !string.IsNullOrWhiteSpace(SelectedProfile.ModelPath))
				{
					await _serverManager.StartAsync(SelectedProfile);
				}
			}
		}
	}

	private string? DetectInstalledVersion()
	{
		if (!ExecutableFound)
		{
			return null;
		}
		string directoryName = Path.GetDirectoryName(ExecutablePath);
		VersionInfo byPath = _versionCache.GetByPath(directoryName);
		if (byPath != null)
		{
			return byPath.Display;
		}
		string path = Path.Combine(directoryName, "ggml-version.txt");
		if (File.Exists(path))
		{
			return File.ReadAllText(path).Trim();
		}
		FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ExecutablePath);
		if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
		{
			return versionInfo.ProductVersion;
		}
		return _loc.T("server.installed_unknown");
	}

	private string DetectCudaSupport()
	{
		if (!ExecutableFound)
		{
			return "N/A";
		}
		string? directoryName = Path.GetDirectoryName(ExecutablePath);
		string[] cudartFiles = Directory.GetFiles(directoryName, OperatingSystem.IsWindows() ? "cudart*.dll" : "libcudart.so*");
		string[] cublasFiles = Directory.GetFiles(directoryName, OperatingSystem.IsWindows() ? "cublas*.dll" : "libggml-cuda.so*");
		if (cudartFiles.Length != 0 || cublasFiles.Length != 0)
		{
			return OperatingSystem.IsWindows() ? $"CUDA DLLs found: {cudartFiles.Length + cublasFiles.Length} files" : $"CUDA + RPC build detected: {cudartFiles.Length + cublasFiles.Length} libraries";
		}
		string executableName = Path.GetFileName(ExecutablePath).ToLower();
		if (executableName.Contains("cuda") || executableName.Contains("cuBLAS"))
		{
			return "CUDA build (by executable name)";
		}
		return "CPU build (no CUDA DLLs detected)";
	}

	[RelayCommand]
	private async Task ExportToBat()
	{
		if (SelectedProfile == null)
		{
			await _dialog.ShowErrorAsync(_loc.T("server.select_profile_first"));
			return;
		}
		SyncSettingsToProfile();
		string commandArgs = BuildCommandLinePreview(SelectedProfile);
		string executableDirectory = Path.GetDirectoryName(ExecutablePath) ?? ".";
		string executableFileName = Path.GetFileName(ExecutablePath) ?? "llama-server.exe";
		string batContent = $"@echo off\r\ntitle Llama Studio - {EscapeBatString(SelectedProfile.Name)}\r\ncd /d \"{EscapeBatString(executableDirectory)}\"\r\ncall {executableFileName} {commandArgs}\r\npause";
		string targetDirectory = await _dialog.SelectFolderAsync(_loc.T("server.bat_save_folder_title"));
		if (!string.IsNullOrEmpty(targetDirectory))
		{
			string path = SelectedProfile.Name.Replace(" ", "_") + ".bat";
			string batFilePath = Path.Combine(targetDirectory, path);
			UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
			File.WriteAllText(batFilePath, batContent, encoding);
			_log.Information("Exported bat to: " + batFilePath, "Server");
			await _dialog.ShowSuccessAsync(string.Format(_loc.T("server.bat_saved_fmt"), batFilePath));
		}
	}

	private static string EscapeBatString(string input)
	{
		return input.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
			.Replace("<", "^<")
			.Replace(">", "^>")
			.Replace("(", "^(")
			.Replace(")", "^)")
			.Replace("!", "^!")
			.Replace("%", "%%");
	}

	[RelayCommand]
	private async Task CopyCommandLine()
	{
		if (SelectedProfile == null)
		{
			return;
		}
		SyncSettingsToProfile();
		string commandLineArgs = BuildCommandLinePreview(SelectedProfile);
		string fullCommand = "\"" + ExecutablePath + "\" " + commandLineArgs;
		try
		{
			Window window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
			if (window != null)
			{
				await window.Clipboard.SetTextAsync(fullCommand);
				await _dialog.ShowSuccessAsync(_loc.T("server.command_copied"));
			}
		}
		catch
		{
			_log.Information("Command line: " + fullCommand, "Server");
		}
	}

	[RelayCommand]
	private async Task ImportFromBat()
	{
		string path = await _dialog.SelectFileAsync(_loc.T("server.bat_import_title"), null, "Batch Files|*.bat|All Files|*.*");
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		try
		{
			string[] batLines = await File.ReadAllLinesAsync(path);
			string commandLine = null;
			string[] lines = batLines;
			for (int i = 0; i < lines.Length; i++)
			{
				string trimmedLine = lines[i].Trim();
				if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("@") && !trimmedLine.StartsWith("REM ") && !trimmedLine.StartsWith("rem ") && !(trimmedLine == "pause") && !trimmedLine.StartsWith("set ") && !trimmedLine.StartsWith("title ") && !trimmedLine.StartsWith("cd "))
				{
					string candidateCommand = trimmedLine;
					if (candidateCommand.StartsWith("call "))
					{
						candidateCommand = candidateCommand.Substring(5).Trim();
					}
					if (candidateCommand.Contains("llama-server", StringComparison.OrdinalIgnoreCase) || candidateCommand.Contains("llama_server", StringComparison.OrdinalIgnoreCase))
					{
						commandLine = candidateCommand;
						break;
					}
				}
			}
			if (string.IsNullOrEmpty(commandLine))
			{
				await _dialog.ShowErrorAsync(_loc.T("server.no_command_in_bat"));
				return;
			}
			string executableFromBat = null;
			string argsStr = commandLine;
			if (commandLine.StartsWith("\""))
			{
				int quoteIndex = commandLine.IndexOf('"', 1);
				if (quoteIndex > 0)
				{
					executableFromBat = commandLine.Substring(1, quoteIndex - 1);
					argsStr = commandLine.Substring(quoteIndex + 1).Trim();
				}
			}
			else
			{
				int spaceIndex = commandLine.IndexOf(' ');
				if (spaceIndex > 0)
				{
					executableFromBat = commandLine.Substring(0, spaceIndex);
					argsStr = commandLine.Substring(spaceIndex + 1).Trim();
				}
			}
			if (!string.IsNullOrEmpty(executableFromBat) && File.Exists(executableFromBat))
			{
				string directoryName = Path.GetDirectoryName(executableFromBat);
				if (!string.IsNullOrEmpty(directoryName))
				{
					LlamaCppDirectory = directoryName;
				}
				ExecutablePath = executableFromBat;
			}
			List<(string, string)> parsedArgs = ParseBatArguments(argsStr);
			if (SelectedProfile == null)
			{
				await _dialog.ShowErrorAsync(_loc.T("server.select_profile_first"));
				return;
			}
			ApplyParsedArgsToProfile(SelectedProfile, parsedArgs);
			ApplyProfileToSettings(SelectedProfile);
			UpdateCommandLinePreview();
			int count = parsedArgs.Count;
			_log.Information($"Imported {count} arguments from {path}", "Server");
			await _dialog.ShowSuccessAsync(string.Format(_loc.T("server.imported_fmt"), count));
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to import from bat", "Server");
			await _dialog.ShowErrorAsync(string.Format(_loc.T("server.import_error_fmt"), ex.Message));
		}
	}

	public async Task<int> ImportBatTextAsync(string content, string? profileId)
	{
		if (string.IsNullOrWhiteSpace(content))
			throw new InvalidOperationException("BAT-файл пуст.");

		string? command = null;
	foreach (string rawLine in content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
		{
			string line = rawLine.Trim();
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith("@") || line.StartsWith("REM ", StringComparison.OrdinalIgnoreCase) || line.Equals("pause", StringComparison.OrdinalIgnoreCase) || line.StartsWith("set ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("title ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
				continue;
			if (line.StartsWith("call ", StringComparison.OrdinalIgnoreCase))
				line = line.Substring(5).Trim();
			if (line.Contains("llama-server", StringComparison.OrdinalIgnoreCase) || line.Contains("llama_server", StringComparison.OrdinalIgnoreCase))
			{
				command = line;
				break;
			}
		}
		if (string.IsNullOrWhiteSpace(command))
			throw new InvalidOperationException("В BAT-файле не найдена команда llama-server.");

		string args = command;
		if (args.StartsWith('"'))
		{
			int end = args.IndexOf('"', 1);
			if (end > 0) args = args.Substring(end + 1).Trim();
		}
		else
		{
			int separator = args.IndexOf(' ');
			if (separator > 0) args = args.Substring(separator + 1).Trim();
		}
		List<(string Flag, string? Value)> parsed = ParseBatArguments(args);
		ServerProfile? profile = !string.IsNullOrWhiteSpace(profileId) ? await _profileManager.GetProfileAsync(profileId) : SelectedProfile;
		profile ??= SelectedProfile;
		if (profile == null)
			throw new InvalidOperationException("Профиль не выбран.");
		ApplyParsedArgsToProfile(profile, parsed);
		ApplyProfileToSettings(profile);
		await _profileManager.SaveProfileAsync(profile);
		SelectedProfile = profile;
		UpdateCommandLinePreview();
		return parsed.Count;
	}

	private static List<(string Flag, string? Value)> ParseBatArguments(string argsStr)
	{
		List<(string, string)> parsedArgs = new List<(string, string)>();
		Dictionary<string, ArgumentMapping> knownArgumentsLookup = CommandLineParser.KnownArgumentsLookup;
		List<string> tokens = new List<string>();
		int i = 0;
		while (i < argsStr.Length)
		{
			for (; i < argsStr.Length && char.IsWhiteSpace(argsStr[i]); i++)
			{
			}
			if (i >= argsStr.Length)
			{
				break;
			}
			if (argsStr[i] == '"')
			{
				int quoteEnd = argsStr.IndexOf('"', i + 1);
				if (quoteEnd > 0)
				{
					tokens.Add(argsStr.Substring(i + 1, quoteEnd - i - 1));
					i = quoteEnd + 1;
				}
				else
				{
					tokens.Add(argsStr.Substring(i + 1));
					i = argsStr.Length;
				}
			}
			else
			{
				int tokenStart = i;
				for (; i < argsStr.Length && !char.IsWhiteSpace(argsStr[i]); i++)
				{
				}
				tokens.Add(argsStr.Substring(tokenStart, i - tokenStart));
			}
		}
		for (int j = 0; j < tokens.Count; j++)
		{
			string token = tokens[j];
			if (token.StartsWith("-"))
			{
				bool nextIsFlag = j + 1 < tokens.Count && tokens[j + 1].StartsWith("-");
				string key = CommandLineParser.NormalizeSpecialCharacters(token.TrimStart('-'));
				ArgumentMapping valueOrDefault = knownArgumentsLookup.GetValueOrDefault(key);
				bool isFlag = (valueOrDefault != null && valueOrDefault.ArgType == ArgType.Flag) || (valueOrDefault != null && valueOrDefault.ArgType == ArgType.BoolOnOff);
				if (nextIsFlag || isFlag)
				{
					parsedArgs.Add((token, null));
				}
				else if (j + 1 < tokens.Count)
				{
					parsedArgs.Add((token, tokens[j + 1]));
					j++;
				}
				else
				{
					parsedArgs.Add((token, null));
				}
			}
		}
		return parsedArgs;
	}

	private void ApplyParsedArgsToProfile(ServerProfile profile, List<(string Flag, string? Value)> parsed)
	{
		Dictionary<string, ArgumentMapping> knownArgumentsLookup = CommandLineParser.KnownArgumentsLookup;
		foreach (var parsedArg in parsed)
		{
			string flag = parsedArg.Flag;
			string value = parsedArg.Value;
			if (string.Equals(flag, "--rpc", StringComparison.OrdinalIgnoreCase) || string.Equals(flag, "-rpc", StringComparison.OrdinalIgnoreCase))
			{
				profile.RpcEnabled = !string.IsNullOrWhiteSpace(value);
				profile.RpcEndpoint = value ?? string.Empty;
				continue;
			}
			string key = CommandLineParser.NormalizeSpecialCharacters(flag.TrimStart('-'));
			ArgumentMapping valueOrDefault = knownArgumentsLookup.GetValueOrDefault(key);
			if (valueOrDefault == null)
			{
				string argKey = (flag.StartsWith("-") ? flag : ("--" + flag));
				if (value != null)
				{
					profile.CustomArguments[argKey] = value;
				}
				else
				{
					profile.CustomArguments[argKey] = string.Empty;
				}
				continue;
			}
			switch (valueOrDefault.Canonical)
			{
			case "--model":
			case "-m":
				profile.ModelPath = value;
				break;
			case "--mmproj":
				profile.MmprojPath = value;
				break;
			case "-hf":
				profile.HfRepo = value;
				break;
			case "-hff":
				profile.HfFile = value;
				break;
			case "--offline":
				profile.HfOffline = true;
				break;
			case "-hfd":
				profile.HfRepoDraft = value;
				break;
			case "-md":
				profile.DraftModelPath = value;
				break;
			case "-ngl":
			case "--gpu-layers":
				profile.GpuLayers = value ?? "all";
				break;
			case "-t":
			case "--threads":
			{
				if (int.TryParse(value, out var threadsValue))
				{
					profile.Threads = threadsValue;
				}
				break;
			}
			case "-tb":
			case "--threads-batch":
			{
				if (int.TryParse(value, out var threadsBatchValue))
				{
					profile.ThreadsBatch = threadsBatchValue;
				}
				break;
			}
			case "-mg":
			case "--main-gpu":
			{
				if (int.TryParse(value, out var mainGpuValue))
				{
					profile.MainGpu = mainGpuValue;
				}
				break;
			}
			case "-sm":
			case "--gpu-split":
			{
				if (Enum.TryParse<GpuSplitMode>(value, ignoreCase: true, out var gpuSplitModeValue))
				{
					profile.GpuSplitMode = gpuSplitModeValue;
				}
				break;
			}
			case "-ts":
			case "--tensor-split":
				if (!string.IsNullOrEmpty(value))
				{
					try
					{
						profile.TensorSplit = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
					}
					catch
					{
					}
				}
				break;
			case "-fa":
			case "--flash-attn":
				profile.FlashAttention = value != "off" && value != "false";
				break;
			case "--memory-map":
			case "-mmap":
				profile.Mmap = value != "off" && value != "false";
				break;
			case "--mlock":
				profile.Mlock = true;
				break;
			case "--no-kv-offload":
			case "-nkvo":
				profile.KvOffloadEnabled = false;
				break;
			case "-c":
			case "--ctx-size":
			{
				if (int.TryParse(value, out var contextSizeValue))
				{
					profile.ContextSize = contextSizeValue;
				}
				break;
			}
			case "--batch":
			case "-b":
			{
				if (int.TryParse(value, out var batchSizeValue))
				{
					profile.BatchSize = batchSizeValue;
				}
				break;
			}
			case "--ubatch":
			case "-ub":
			{
				if (int.TryParse(value, out var ubatchSizeValue))
				{
					profile.UbatchSize = ubatchSizeValue;
				}
				break;
			}
			case "-n":
			case "--max-tokens":
			{
				if (int.TryParse(value, out var maxTokensValue))
				{
					profile.MaxTokens = maxTokensValue;
				}
				break;
			}
			case "-ctk":
			case "--cache-type-k":
				profile.CacheTypeK = ParseCacheTypeKEnum(value);
				break;
			case "-ctv":
			case "--cache-type-v":
				profile.CacheTypeV = ParseCacheTypeVEnc(value);
				break;
			case "--cache-ram":
			{
				if (int.TryParse(value, out var cacheRamValue))
				{
					profile.CacheRam = cacheRamValue;
				}
				break;
			}
			case "--cache-reuse":
			{
				if (int.TryParse(value, out var cacheReuseValue))
				{
					profile.CacheReuse = cacheReuseValue;
				}
				break;
			}
			case "--temp":
			{
				if (float.TryParse(value, out var temperatureValue))
				{
					profile.Temperature = temperatureValue;
				}
				break;
			}
			case "--top-k":
			{
				if (int.TryParse(value, out var topKValue))
				{
					profile.TopK = topKValue;
				}
				break;
			}
			case "--top-p":
			{
				if (float.TryParse(value, out var topPValue))
				{
					profile.TopP = topPValue;
				}
				break;
			}
			case "--min-p":
			{
				if (float.TryParse(value, out var minPValue))
				{
					profile.MinP = minPValue;
				}
				break;
			}
			case "--typical-p":
			{
				if (float.TryParse(value, out var typicalPValue))
				{
					profile.TypicalP = typicalPValue;
				}
				break;
			}
			case "--repeat-penalty":
			{
				if (float.TryParse(value, out var repeatPenaltyValue))
				{
					profile.RepeatPenalty = repeatPenaltyValue;
				}
				break;
			}
			case "--repeat-last-n":
			{
				if (int.TryParse(value, out var repeatLastNValue))
				{
					profile.RepeatLastN = repeatLastNValue;
				}
				break;
			}
			case "--presence-penalty":
			{
				if (float.TryParse(value, out var presencePenaltyValue))
				{
					profile.PresencePenalty = presencePenaltyValue;
				}
				break;
			}
			case "--frequency-penalty":
			{
				if (float.TryParse(value, out var frequencyPenaltyValue))
				{
					profile.FrequencyPenalty = frequencyPenaltyValue;
				}
				break;
			}
			case "--dry-multiplier":
			{
				if (float.TryParse(value, out var dryMultiplierValue))
				{
					profile.DryMultiplier = dryMultiplierValue;
				}
				break;
			}
			case "--dry-base":
			{
				if (float.TryParse(value, out var dryBaseValue))
				{
					profile.DryBase = dryBaseValue;
				}
				break;
			}
			case "--dynatemp-range":
			{
				if (float.TryParse(value, out var dynatempStddevValue))
				{
					profile.DynatempStddev = dynatempStddevValue;
				}
				break;
			}
			case "-s":
			case "--seed":
			{
				if (int.TryParse(value, out var seedValue))
				{
					profile.Seed = seedValue;
				}
				break;
			}
			case "--mirostat":
			{
				if (int.TryParse(value, out var mirostatValue))
				{
					profile.Mirostat = (MirostatMode)mirostatValue;
				}
				break;
			}
			case "-mt":
			case "--mirostat-ent":
			case "--mirostat-tau":
			{
				if (float.TryParse(value, out var mirostatTauValue))
				{
					profile.MirostatTau = mirostatTauValue;
				}
				break;
			}
			case "--mirostat-lr":
			{
				if (float.TryParse(value, out var mirostatEtaValue))
				{
					profile.MirostatEta = mirostatEtaValue;
				}
				break;
			}
			case "--xtc-probability":
			{
				if (float.TryParse(value, out var xtcProbabilityValue))
				{
					profile.XtcProbability = xtcProbabilityValue;
				}
				break;
			}
			case "--xtc-threshold":
			{
				if (float.TryParse(value, out var xtcThresholdValue))
				{
					profile.XtcThreshold = xtcThresholdValue;
				}
				break;
			}
			case "--spec-type":
				profile.SpecType = value ?? string.Empty;
				break;
			case "--predict":
			{
				if (int.TryParse(value, out var predictCountValue))
				{
					profile.PredictCount = predictCountValue;
				}
				break;
			}
			case "--speculative":
				profile.SpeculativeDecoding = value != "off" && value != "false";
				break;
			case "--ngld":
				profile.SpecDraftGpuLayers = value ?? "auto";
				break;
			case "--spec-draft-device":
				profile.DraftModelLocalOnly = !string.IsNullOrWhiteSpace(value) && !value.StartsWith("RPC", StringComparison.OrdinalIgnoreCase);
				break;
			case "--spec-draft-n-max":
			{
				if (int.TryParse(value, out var specDraftNMaxValue))
				{
					profile.SpecDraftNMax = specDraftNMaxValue;
				}
				break;
			}
			case "--spec-draft-n-min":
			{
				if (int.TryParse(value, out var specDraftNMinValue))
				{
					profile.SpecDraftNMin = specDraftNMinValue;
				}
				break;
			}
			case "--spec-draft-p-split":
			{
				if (float.TryParse(value, out var specDraftPSplitValue))
				{
					profile.SpecDraftPSplit = specDraftPSplitValue;
				}
				break;
			}
			case "--spec-draft-p-min":
			{
				if (float.TryParse(value, out var specDraftPMinValue))
				{
					profile.SpecDraftPMin = specDraftPMinValue;
				}
				break;
			}
			case "--rope-scaling":
				profile.RopeScaling = value ?? string.Empty;
				break;
			case "--rope-freq-base":
			{
				if (double.TryParse(value, out var ropeFreqBaseValue))
				{
					profile.RopeFreqBase = ropeFreqBaseValue;
				}
				break;
			}
			case "--rope-freq-scale":
			{
				if (double.TryParse(value, out var ropeFreqScaleValue))
				{
					profile.RopeFreqScale = ropeFreqScaleValue;
				}
				break;
			}
			case "--yarn-orig-ctx":
			{
				if (int.TryParse(value, out var yarnOriginalContextValue))
				{
					profile.YarnOriginalContext = yarnOriginalContextValue;
				}
				break;
			}
			case "--yarn-ext-factor":
			{
				if (double.TryParse(value, out var yarnExtFactorValue))
				{
					profile.YarnExtFactor = yarnExtFactorValue;
				}
				break;
			}
			case "--yarn-attn-factor":
			{
				if (double.TryParse(value, out var yarnAttnFactorValue))
				{
					profile.YarnAttnFactor = yarnAttnFactorValue;
				}
				break;
			}
			case "--yarn-beta-fast":
			{
				if (double.TryParse(value, out var yarnBetaFastValue))
				{
					profile.YarnBetaFast = yarnBetaFastValue;
				}
				break;
			}
			case "--yarn-beta-slow":
			{
				if (double.TryParse(value, out var yarnBetaSlowValue))
				{
					profile.YarnBetaSlow = yarnBetaSlowValue;
				}
				break;
			}
			case "--host":
				profile.Host = value ?? "127.0.0.1";
				break;
			case "--port":
			{
				if (int.TryParse(value, out var portValue))
				{
					profile.Port = portValue;
				}
				break;
			}
			case "-to":
			case "--timeout":
			{
				if (int.TryParse(value, out var timeoutValue))
				{
					profile.Timeout = timeoutValue;
				}
				break;
			}
			case "--no-slots":
				profile.EnableSlots = false;
				break;
			case "--slots":
			case "-np":
			{
				if (int.TryParse(value, out var slotsValue))
				{
					profile.Slots = slotsValue;
				}
				break;
			}
			case "--numa":
				profile.Numa = true;
				break;
			case "--no-cache-prompt":
				profile.CachePrompt = false;
				break;
			case "--no-context-shift":
				profile.ContextShift = false;
				break;
			case "--nocb":
			case "--no-cont-batching":
				profile.ContBatching = false;
				break;
			case "--verbose":
				profile.VerboseLogging = true;
				break;
			case "--no-ui":
				profile.EnableWebUI = false;
				break;
			case "--metrics":
				profile.EnableMetrics = true;
				break;
			case "-rea":
			case "--reasoning":
				profile.Reasoning = value == null || value == "on" || value == "true";
				break;
			case "--reasoning-budget":
			{
				if (int.TryParse(value, out var reasoningBudgetValue))
				{
					profile.ReasoningBudget = reasoningBudgetValue;
				}
				break;
			}
			case "--embedding":
				profile.EmbeddingMode = true;
				break;
			case "--pooling":
				profile.PoolingType = value ?? string.Empty;
				break;
			case "--api-key":
				profile.ApiKey = value;
				break;
			case "--alias":
				profile.Alias = value;
				break;
			}
		}
	}

	private static CacheTypeK ParseCacheTypeKEnum(string? value)
	{
		return Enum.TryParse<LlamaStudio.Core.Enums.CacheTypeK>(value, ignoreCase: true, out var result)
			? result
			: LlamaStudio.Core.Enums.CacheTypeK.F16;
	}

	[RelayCommand]
	private async Task CleanupMissingModelProfiles()
	{
		List<ServerProfile> missing = _profileManager.GetAllProfiles().Where(HasMissingModel).ToList();
		if (missing.Count == 0)
		{
			await _dialog.ShowMessageAsync(_loc.Language == "en" ? "No profiles with missing model files were found." : "Профили с отсутствующими файлами моделей не найдены.", _loc.Language == "en" ? "Profile cleanup" : "Очистка профилей");
			return;
		}
		string names = string.Join(", ", missing.Select((ServerProfile p) => p.Name));
		string message = _loc.Language == "en"
			? $"The following profiles point to missing model files:\n{names}\n\nDelete these profiles?"
			: $"Следующие профили ссылаются на отсутствующие файлы моделей:\n{names}\n\nУдалить эти профили?";
		if (await _dialog.ShowConfirmationAsync(message, _loc.Language == "en" ? "Profile cleanup" : "Очистка профилей"))
		{
			string backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio", "profile_cleanup_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
			Directory.CreateDirectory(backupDirectory);
			foreach (ServerProfile profile in missing)
			{
				string profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio", "profiles", profile.Id + ".json");
				if (File.Exists(profilePath))
				{
					File.Copy(profilePath, Path.Combine(backupDirectory, profile.Id + ".json"), overwrite: true);
				}
				await _profileManager.DeleteProfileAsync(profile.Id);
			}
			LoadProfiles();
			_log.Information($"Removed {missing.Count} profiles with missing model files", "Server");
		}
	}

	private async Task<bool> EnsureRpcReadyForLlamaServerAsync()
	{
		if (!RpcEnabled || PassiveRpcMode)
		{
			return true;
		}

		// A local llama-server must never start against a stale or missing RPC
		// endpoint. Recheck SSH and TCP for every explicit local start; only try
		// the remote launch when the endpoint is unavailable.
		if (!await CheckSshConnectionAsync())
		{
			RpcConnectionVerified = false;
			return false;
		}

		await CheckRpcConnectionAsync();
		if (!RpcConnectionVerified)
		{
			await StartRpcServerAsync();
		}

		return RpcConnectionVerified;
	}

	private static CacheTypeV ParseCacheTypeVEnc(string? value)
	{
		return Enum.TryParse<LlamaStudio.Core.Enums.CacheTypeV>(value, ignoreCase: true, out var result)
			? result
			: LlamaStudio.Core.Enums.CacheTypeV.F16;
	}

	private void UpdateCommandLinePreview()
	{
		if (SelectedProfile == null)
		{
			SetCommandLinePreview(string.Empty);
			return;
		}
		SyncSettingsToProfile();
		SetCommandLinePreview(BuildCommandLinePreview(SelectedProfile));
	}

	private string BuildCommandLinePreview(ServerProfile profile)
	{
		profile.DeviceBackendPrefix = DetectDeviceBackendPrefix();
		return profile.BuildReferenceArgsString();
	}

	private string DetectDeviceBackendPrefix()
	{
		string path = (ExecutablePath ?? string.Empty).ToLowerInvariant();
		if (path.Contains("vulkan"))
		{
			return "Vulkan";
		}
		if (path.Contains("sycl"))
		{
			return "SYCL";
		}
		if (path.Contains("metal"))
		{
			return "METAL";
		}
		if (path.Contains("rpc"))
		{
			return "RPC";
		}
		if (path.Contains("cpu"))
		{
			return string.Empty;
		}
		if (path.Contains("openvino"))
		{
			return string.Empty;
		}
		return "CUDA";
	}

	[RelayCommand]
	private async Task OpenArgPicker()
	{
		HashSet<string> excludedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"model", "m", "mmproj", "mm", "gpu-layers", "n-gpu-layers", "ngl", "threads", "t", "threads-batch",
			"tb", "flash-attn", "fa", "mmap", "no-mmap", "mlock", "kv-offload", "no-kv-offload", "kvo", "nkvo",
			"main-gpu", "mg", "tensor-split", "ts", "ctx-size", "c", "batch-size", "b", "ubatch-size", "ub",
			"predict", "n", "temp", "top-k", "top-p", "min-p", "typical-p", "typical", "repeat-penalty", "rep-pen",
			"repeat-last-n", "rep-last-n", "presence-penalty", "pres-pen", "frequency-penalty", "freq-pen", "seed", "s", "mirostat", "mirostat-tau",
			"mirostat-ent", "mirostat-eta", "mirostat-lr", "dry-multiplier", "dry-base", "dynatemp-range", "dynatemp-stddev", "xtc-probability", "xtc-threshold", "cache-type-k",
			"ctk", "cache-ram", "cram", "cache-type-v", "ctv", "cache-reuse", "spec-draft-model", "md", "model-draft", "spec-type",
			"spec-draft-ngl", "ngld", "spec-draft-n-max", "spec-draft-n-min", "spec-draft-p-split", "draft-p-split", "spec-draft-p-min", "draft-p-min", "rope-scaling", "rope-freq-base",
			"rope-freq-scale", "yarn-orig-ctx", "yarn-ext-factor", "yarn-attn-factor", "yarn-beta-fast", "yarn-beta-slow", "host", "port", "timeout", "to",
			"parallel", "np", "numa", "cache-prompt", "no-cache-prompt", "cont-batching", "no-cont-batching", "cb", "ncb", "context-shift",
			"no-context-shift", "verbose", "lv", "slots", "no-slots", "ui", "no-ui", "webui", "no-webui", "metrics",
			"reasoning", "rea", "embedding", "embeddings", "device", "dev", "split-mode", "sm", "rpc", "fit", "fit-target", "fit-ctx",
			"threads-http", "sse-ping-interval", "priority", "prio"
		};
		if (_helpInfo == null)
		{
			await _dialog.ShowInfoAsync(_loc.T("server.check_version_first"));
			return;
		}
		ArgPickerViewModel vm = new ArgPickerViewModel(_loc);
		vm.LoadFlags(_helpInfo, excludedFlags, AdditionalArgs);
		ArgPickerWindow win = new ArgPickerWindow
		{
			DataContext = vm
		};
		await win.ShowDialog(GetMainWindow());
		if (!win.IsConfirmed)
		{
			return;
		}
		List<string> selectedArgs = vm.GetSelectedArgs().Args;
		if (selectedArgs.Count <= 0)
		{
			return;
		}
		string existingArgs = AdditionalArgs.Trim();
		List<string> argLines = new List<string>();
		if (!string.IsNullOrEmpty(existingArgs))
		{
			argLines.AddRange(existingArgs.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		}
		foreach (string selectedArg in selectedArgs)
		{
			string argName = selectedArg.Split(' ')[0];
			int existingIndex = argLines.FindIndex((string l) => l.Split(' ')[0] == argName);
			if (existingIndex >= 0)
			{
				argLines[existingIndex] = selectedArg;
			}
			else
			{
				argLines.Add(selectedArg);
			}
		}
		AdditionalArgs = string.Join("\n", argLines);
		_log.Information($"Added {selectedArgs.Count} CLI args from picker", "Server");
	}

	private static MainWindow? GetMainWindow()
	{
		return (Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;
	}

	private void ApplyCommandLineEdits(ServerProfile profile)
	{
		string currentCommandLine = BuildCommandLinePreview(profile);
		string editedCommandLine = CommandLinePreview?.Trim() ?? string.Empty;
		if (editedCommandLine == currentCommandLine)
		{
			return;
		}
		Dictionary<string, string> currentArgs = ParseCommandLineArgs(currentCommandLine);
		Dictionary<string, string?> editedArgs = ParseCommandLineArgs(editedCommandLine);
		Dictionary<string, string> updatedCustomArgs = new Dictionary<string, string>(profile.CustomArguments);
		foreach (KeyValuePair<string, string> editedArg in editedArgs)
		{
			editedArg.Deconstruct(out var key, out var value);
			string argKey = key;
			string editedValue = value;
			string valueOrDefault = currentArgs.GetValueOrDefault(argKey);
			if (valueOrDefault == null)
			{
				updatedCustomArgs[argKey] = editedValue;
			}
			else if (valueOrDefault != editedValue)
			{
				updatedCustomArgs[argKey] = editedValue;
			}
			else if (updatedCustomArgs.ContainsKey(argKey))
			{
				updatedCustomArgs.Remove(argKey);
			}
		}
		profile.CustomArguments = updatedCustomArgs;
	}

	private static Dictionary<string, string?> ParseCommandLineArgs(string cmdline)
	{
		Dictionary<string, string> args = new Dictionary<string, string>();
		int i = 0;
		while (i < cmdline.Length)
		{
			for (; i < cmdline.Length && char.IsWhiteSpace(cmdline[i]); i++)
			{
			}
			if (i >= cmdline.Length)
			{
				break;
			}
			string flag;
			if (cmdline[i] == '"')
			{
				i++;
				int quotedStart = i;
				for (; i < cmdline.Length && cmdline[i] != '"'; i++)
				{
				}
				flag = cmdline.Substring(quotedStart, i - quotedStart);
				i++;
			}
			else
			{
				int plainStart = i;
				for (; i < cmdline.Length && !char.IsWhiteSpace(cmdline[i]); i++)
				{
				}
				flag = cmdline.Substring(plainStart, i - plainStart);
			}
			if (!flag.StartsWith("-"))
			{
				continue;
			}
			for (; i < cmdline.Length && char.IsWhiteSpace(cmdline[i]); i++)
			{
			}
			if (i < cmdline.Length)
			{
				string flagValue;
				if (cmdline[i] == '"')
				{
					i++;
					int quotedValueStart = i;
					for (; i < cmdline.Length && cmdline[i] != '"'; i++)
					{
					}
					flagValue = cmdline.Substring(quotedValueStart, i - quotedValueStart);
					i++;
				}
				else
				{
					int plainValueStart = i;
					for (; i < cmdline.Length && !char.IsWhiteSpace(cmdline[i]); i++)
					{
					}
					flagValue = cmdline.Substring(plainValueStart, i - plainValueStart);
				}
				args[flag] = flagValue;
			}
			else
			{
				args[flag] = string.Empty;
			}
		}
		return args;
	}

	public void SyncSettingsToProfile()
	{
		if (_profileReady && !_isInitializing && !_isApplyingProfile && SelectedProfile != null)
		{
			SyncSettingsToProfileTarget(SelectedProfile);
		}
	}

	private void AutoSaveProfile()
	{
		if (_profileReady && !_isInitializing && !_isApplyingProfile && SelectedProfile != null)
		{
			SyncSettingsToProfile();
			_profileManager.SaveProfileNow(SelectedProfile);
		}
	}

	private void PerformAutoSaveProfile()
	{
		if (_profileReady && !_isInitializing && !_isApplyingProfile && SelectedProfile != null)
		{
			SyncSettingsToProfile();
			_profileManager.SaveProfileAsync(SelectedProfile);
		}
	}

	private void SyncSettingsToProfileTarget(ServerProfile profile)
	{
		profile.ModelPath = (string.IsNullOrWhiteSpace(ModelPath) ? null : ModelPath);
		profile.MmprojPath = (string.IsNullOrWhiteSpace(MmprojPath) ? null : MmprojPath);
		profile.DraftModelPath = (string.IsNullOrWhiteSpace(DraftModelPath) ? null : DraftModelPath);
		profile.CustomArguments = new Dictionary<string, string>();
		profile.HfRepo = (string.IsNullOrWhiteSpace(HfRepo) ? null : HfRepo);
		profile.HfFile = (string.IsNullOrWhiteSpace(HfFile) ? null : HfFile);
		profile.HfOffline = HfOffline;
		profile.HfRepoDraft = (string.IsNullOrWhiteSpace(HfRepoDraft) ? null : HfRepoDraft);
		profile.Host = Host;
		profile.Port = Port;
		profile.RpcEnabled = RpcEnabled;
		profile.RpcEndpoint = GetRpcEndpointForStorage();
		profile.RpcSshUsername = RpcSshUsername.Trim();
		profile.RpcSavePassword = RpcSavePassword;
		profile.RpcSshPassword = RpcSavePassword ? RpcSshPassword : string.Empty;
		profile.RpcUseCache = RpcUseCache;
		profile.RpcRemoteTelemetry = RemoteTelemetryEnabled;
		profile.RpcServerPath = RpcServerPath.Trim();
		profile.Timeout = Timeout;
		profile.Slots = Slots;
		profile.GpuLayers = GpuLayers;
		profile.Threads = Threads;
		profile.ThreadsBatch = ThreadsBatch;
		profile.FlashAttention = FlashAttention;
		profile.Mmap = Mmap;
		profile.Mlock = Mlock;
		profile.KvOffloadEnabled = KvOffloadEnabled;
		profile.MainGpu = MainGpu;
		profile.SelectedGpuIndices = GpuInfos.Where((GpuInfo gpu) => gpu.IsSelected && gpu.Index >= 0).Select((GpuInfo gpu) => gpu.Index).ToArray();
		profile.GpuSplitMode = GpuSplitMode;
		profile.TensorSplit = (string.IsNullOrWhiteSpace(TensorSplit) ? Array.Empty<int>() : TensorSplit.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray());
		profile.ContextSize = ContextSize;
		profile.BatchSize = BatchSize;
		profile.UbatchSize = UbatchSize;
		profile.MaxTokens = MaxTokens;
		profile.CacheTypeK = ParseCacheTypeK(CacheTypeK);
		profile.CacheTypeV = ParseCacheTypeV(CacheTypeV);
		profile.CacheRam = CacheRam;
		profile.CacheReuse = CacheReuse;
		profile.CachePrompt = CachePrompt;
		profile.Temperature = Temperature;
		profile.TopK = TopK;
		profile.TopP = TopP;
		profile.MinP = MinP;
		profile.TypicalP = TypicalP;
		profile.RepeatPenalty = RepeatPenalty;
		profile.RepeatLastN = RepeatLastN;
		profile.PresencePenalty = PresencePenalty;
		profile.FrequencyPenalty = FrequencyPenalty;
		profile.Seed = Seed;
		profile.Mirostat = MirostatMode;
		profile.MirostatTau = MirostatTau;
		profile.MirostatEta = MirostatEta;
		profile.DryMultiplier = DryMultiplier;
		profile.DryBase = DryBase;
		profile.DynatempStddev = DynatempStddev;
		profile.XtcProbability = XtcProbability;
		profile.XtcThreshold = XtcThreshold;
		profile.SpecType = ((SpecType == "none" || string.IsNullOrWhiteSpace(SpecType)) ? string.Empty : SpecType);
		profile.PredictCount = PredictCount;
		profile.SpeculativeDecoding = SpeculativeDecoding;
		profile.Jinja = Jinja;
		profile.DraftModelLocalOnly = DraftModelLocalOnly;
		profile.SpecDraftGpuLayers = SpecDraftGpuLayers;
		profile.SpecDraftNMax = SpecDraftNMax;
		profile.SpecDraftNMin = SpecDraftNMin;
		profile.SpecDraftPSplit = SpecDraftPSplit;
		profile.SpecDraftPMin = SpecDraftPMin;
		profile.PriorityHigh = PriorityHigh;
		profile.EmbeddingMode = EmbeddingMode;
		profile.PoolingType = PoolingType;
		profile.Numa = Numa;
		profile.ProcessPriority = ProcessPriority;
		profile.ContBatching = ContBatching;
		profile.ContextShift = ContextShift;
		profile.VerboseLogging = VerboseLogging;
		profile.EnableWebUI = EnableWebUI;
		profile.EnableSlots = EnableSlots;
		profile.EnableMetrics = EnableMetrics;
		profile.Reasoning = Reasoning;
		profile.ReasoningBudget = ReasoningBudget;
		profile.RopeScaling = RopeScaling;
		profile.RopeFreqBase = RopeFreqBase;
		profile.RopeFreqScale = RopeFreqScale;
		profile.YarnOriginalContext = YarnOriginalContext;
		profile.YarnExtFactor = YarnExtFactor;
		profile.YarnAttnFactor = YarnAttnFactor;
		profile.YarnBetaFast = YarnBetaFast;
		profile.YarnBetaSlow = YarnBetaSlow;
		profile.LlamaCppBaseDirectory = (string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory) ? null : _settings.LlamaCppBaseDirectory);
		profile.SelectedServerVersion = (string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion) ? null : _settings.ActiveLlamaCppVersion);
		profile.ApiKey = (string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey);
		profile.Alias = (string.IsNullOrWhiteSpace(Alias) ? null : Alias);
		if (!string.IsNullOrWhiteSpace(AdditionalArgs))
		{
			profile.CustomArguments = (from line in AdditionalArgs.Split(new char[2] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				select line.Split(new char[2] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToDictionary((string[] parts) => parts[0], (string[] parts) => (parts.Length <= 1) ? string.Empty : parts[1]);
		}
		else
		{
			profile.CustomArguments = new Dictionary<string, string>();
		}
		profile.UpdatedAt = DateTime.Now;
	}

	private static CacheTypeK ParseCacheTypeK(string value)
	{
		return Enum.TryParse<LlamaStudio.Core.Enums.CacheTypeK>(value, ignoreCase: true, out var result)
			? result
			: LlamaStudio.Core.Enums.CacheTypeK.F16;
	}

	private static CacheTypeV ParseCacheTypeV(string value)
	{
		return Enum.TryParse<LlamaStudio.Core.Enums.CacheTypeV>(value, ignoreCase: true, out var result)
			? result
			: LlamaStudio.Core.Enums.CacheTypeV.F16;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRamUsedGbChanged(double value)
	{
		OnPropertyChanged("RamUsedText");
		OnPropertyChanged("RamPercent");
	}

	private void OnRamCommittedGbChanged(double value)
	{
		OnPropertyChanged("RamUsedText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTokensPerSecondChanged(double value)
	{
		OnPropertyChanged("TokensPerSecondText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnPromptTokensPerSecondChanged(double value)
	{
		OnPropertyChanged("PromptTokensPerSecondText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedProfileChanged(ServerProfile? value)
	{
		if (value != null)
		{
			ApplyProfileToSettings(value);
			SetCommandLinePreview(BuildCommandLinePreview(value));
			_settings.LastSelectedProfileId = value.Id.ToString();
			OnPropertyChanged("IsDefaultProfile");
			OnPropertyChanged("CanSetDefault");
			_settings.SaveAsync();
			string profileId = value.Id.ToString();
			if (!_isInitializing && _previousSelectedProfileId != profileId)
			{
				CheckServerAndOfferRestart(value);
			}
			_previousSelectedProfileId = profileId;
		}
		else
		{
			SetCommandLinePreview(string.Empty);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHfRepoChanged(string value)
	{
		RecommendedModel recommendedModel = RecommendedModels.FirstOrDefault((RecommendedModel m) => m.Repo == value);
		if (recommendedModel != null)
		{
			ModelDescription = recommendedModel.Description;
			ModelSizeInfo = "~" + recommendedModel.Size;
			ModelParamsInfo = recommendedModel.Params;
		}
		else
		{
			ModelDescription = string.Empty;
			ModelSizeInfo = string.Empty;
			ModelParamsInfo = string.Empty;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnPortChanged(int value)
	{
		OnPropertyChanged("PortText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTimeoutChanged(int value)
	{
		OnPropertyChanged("TimeoutText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSlotsChanged(int value)
	{
		OnPropertyChanged("SlotsText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnThreadsChanged(int value)
	{
		OnPropertyChanged("ThreadsText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnThreadsBatchChanged(int value)
	{
		OnPropertyChanged("ThreadsBatchText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMainGpuChanged(int value)
	{
		OnPropertyChanged("MainGpuText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnContextSizeChanged(int value)
	{
		OnPropertyChanged("ContextSizeText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnBatchSizeChanged(int value)
	{
		OnPropertyChanged("BatchSizeText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnUbatchSizeChanged(int value)
	{
		OnPropertyChanged("UbatchSizeText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMaxTokensChanged(int value)
	{
		OnPropertyChanged("MaxTokensText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnCacheRamChanged(int value)
	{
		OnPropertyChanged("CacheRamText");
		OnPropertyChanged("RamUsedText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnCacheReuseChanged(int value)
	{
		OnPropertyChanged("CacheReuseText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTemperatureChanged(float value)
	{
		OnPropertyChanged("TemperatureText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTopKChanged(int value)
	{
		OnPropertyChanged("TopKText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTopPChanged(float value)
	{
		OnPropertyChanged("TopPText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMinPChanged(float value)
	{
		OnPropertyChanged("MinPText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTypicalPChanged(float value)
	{
		OnPropertyChanged("TypicalPText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRepeatPenaltyChanged(float value)
	{
		OnPropertyChanged("RepeatPenaltyText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRepeatLastNChanged(int value)
	{
		OnPropertyChanged("RepeatLastNText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnPresencePenaltyChanged(float value)
	{
		OnPropertyChanged("PresencePenaltyText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnFrequencyPenaltyChanged(float value)
	{
		OnPropertyChanged("FrequencyPenaltyText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSeedChanged(int value)
	{
		OnPropertyChanged("SeedText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMirostatTauChanged(float value)
	{
		OnPropertyChanged("MirostatTauText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMirostatEtaChanged(float value)
	{
		OnPropertyChanged("MirostatEtaText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnPredictCountChanged(int value)
	{
		OnPropertyChanged("PredictCountText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void ApplyMuseDflashRecommendation()
	{
		if (string.IsNullOrWhiteSpace(ModelPath) || string.IsNullOrWhiteSpace(DraftModelPath))
			return;
		if (ModelPath.Contains("Muse-Glimmer", StringComparison.OrdinalIgnoreCase) && DraftModelPath.Contains("dflash", StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(SpecType) || SpecType.Equals("none", StringComparison.OrdinalIgnoreCase)))
		{
			SpecType = "draft-dflash";
			SpecDraftNMax = 12;
		}
	}

	private void OnSpecDraftNMaxChanged(int value)
	{
		OnPropertyChanged("SpecDraftNMaxText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSpecDraftNMinChanged(int value)
	{
		OnPropertyChanged("SpecDraftNMinText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSpecDraftPSplitChanged(float value)
	{
		OnPropertyChanged("SpecDraftPSplitText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSpecDraftPMinChanged(float value)
	{
		OnPropertyChanged("SpecDraftPMinText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRopeFreqBaseChanged(double? value)
	{
		OnPropertyChanged("RopeFreqBaseText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRopeFreqScaleChanged(double? value)
	{
		OnPropertyChanged("RopeFreqScaleText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnYarnOriginalContextChanged(int? value)
	{
		OnPropertyChanged("YarnOriginalContextText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnYarnExtFactorChanged(double? value)
	{
		OnPropertyChanged("YarnExtFactorText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnYarnAttnFactorChanged(double? value)
	{
		OnPropertyChanged("YarnAttnFactorText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnYarnBetaFastChanged(double? value)
	{
		OnPropertyChanged("YarnBetaFastText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnYarnBetaSlowChanged(double? value)
	{
		OnPropertyChanged("YarnBetaSlowText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAdditionalArgsChanged(string value)
	{
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLlamaCppDirectoryChanged(string value)
	{
		_settings.LlamaCppDirectory = value;
		UpdateExecutablePath();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedServerChanged(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			SelectServer(value);
		}
	}
}
