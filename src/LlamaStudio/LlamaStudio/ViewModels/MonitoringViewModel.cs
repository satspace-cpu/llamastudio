using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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

public class MonitoringViewModel : ObservableObject, IDisposable
{
	private IReadOnlyList<GpuInfo> _gpuInfos = Array.Empty<GpuInfo>();

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
			OnPropertyChanged();
			OnPropertyChanged(nameof(ShowSingleGpuMonitor));
			OnPropertyChanged(nameof(AllGpuVramText));
			OnPropertyChanged(nameof(AllGpuLoadText));
			OnPropertyChanged(nameof(AllGpuPowerText));
		}
	}

	public bool ShowSingleGpuMonitor => GpuInfos.Count <= 1;

	public string AllGpuVramText => GpuInfos.Count == 0 ? "—" : $"{GpuInfos.Sum((GpuInfo gpu) => gpu.MemoryUsedGb):F1} / {GpuInfos.Sum((GpuInfo gpu) => gpu.MemoryTotalGb):F1} GB";

	public string AllGpuLoadText => GpuInfos.Count == 0 ? "—" : string.Join("  ", GpuInfos.Select((GpuInfo gpu) => $"GPU{gpu.Index} {gpu.GpuUtilization:F0}%"));

	public string AllGpuPowerText => GpuInfos.Count == 0 ? "—" : $"{GpuInfos.Sum((GpuInfo gpu) => gpu.PowerDrawWatts):F0} W";
	private GpuInfo? _remoteGpuInfo;

	public void SetRemoteGpuInfo(GpuInfo? remoteGpu)
	{
		_remoteGpuInfo = remoteGpu;
		List<GpuInfo> combined = GpuInfos.Where(gpu => !gpu.IsRemote).ToList();
		if (remoteGpu != null)
		{
			combined.Add(remoteGpu);
		}
		GpuInfos = combined;
		GpuInfo = combined.FirstOrDefault();
		OnPropertyChanged("VramUsedText");
		OnPropertyChanged("VramPercent");
		OnPropertyChanged("GpuTempText");
		OnPropertyChanged("GpuTempBrush");
		OnPropertyChanged("GpuTempPercent");
		OnPropertyChanged("PowerDrawText");
		OnPropertyChanged("PowerPercent");
		OnPropertyChanged("GpuCoreText");
		OnPropertyChanged("GpuCorePercent");
		OnPropertyChanged("FanSpeedText");
		OnPropertyChanged("FanSpeedPercent");
		OnPropertyChanged("ShowSingleGpuMonitor");
		OnPropertyChanged("AllGpuVramText");
		OnPropertyChanged("AllGpuLoadText");
		OnPropertyChanged("AllGpuPowerText");
	}

	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly IGpuMonitor _gpuMonitor;

	private readonly ILocalizationService _loc;
	/// <summary>r114: localization accessor (used by BenchmarkCenterViewModel).</summary>
	public ILocalizationService Loc => _loc;

	private readonly IDialogService _dialog;

	private readonly DispatcherTimer _gpuTimer;
	private bool _gpuRefreshInProgress;

	private readonly HttpClient _benchmarkClient;

	[ObservableProperty]
	private ServerStatus _serverStatus = new ServerStatus();

	[ObservableProperty]
	private GpuInfo? _gpuInfo;

	private double _lastTps;

	private double _lastPromptTps;

	[ObservableProperty]
	private bool _isMonitoringActive = true;

	[ObservableProperty]
	private double _windowOpacity = 0.92;

	[ObservableProperty]
	private bool _isAlwaysOnTop = true;

	[ObservableProperty]
	private MonitorStyle _displayStyle;

	[ObservableProperty]
	private bool _showTps = true;

	[ObservableProperty]
	private bool _showVram = true;

	[ObservableProperty]
	private bool _showRam = true;

	[ObservableProperty]
	private bool _showGpuTemp = true;

	[ObservableProperty]
	private bool _showPowerDraw = true;

	[ObservableProperty]
	private bool _showGpuCore = true;

	[ObservableProperty]
	private bool _showFanSpeed = true;

	[ObservableProperty]
	private bool _isBenchmarking;

	[ObservableProperty]
	private string _benchmarkResult = "";

	private static readonly double s_totalRamGb = (double)GetTotalPhysicalMemory() / 1073741824.0;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? checkSpeedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? startServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? stopServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? restartServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleMonitoringCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<string?>? setStyleCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? openFloatingWindowCommand;

	public string TpsText => $"{_lastTps:F1}";

	public string PromptTpsText => $"{_lastPromptTps:F1}";

	public double TokensPerSecond => _lastTps;

	public double PromptTokensPerSecond => _lastPromptTps;

	public double TotalRamGb => s_totalRamGb;

	public bool IsSpeedBenchmarking => IsBenchmarking;

	public string SpeedBenchmarkResult => BenchmarkResult;

	public double TpsPercent => Math.Min(_lastTps / 300.0 * 100.0, 100.0);

	public SolidColorBrush TpsBrush
	{
		get
		{
			if (_lastTps <= 50.0)
			{
				return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 16, 185, 129));
			}
			if (_lastTps <= 150.0)
			{
				return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 251, 191, 36));
			}
			return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 239, 68, 68));
		}
	}

	public string VramUsedText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "—";
			}
			return $"{GpuInfo.MemoryUsedGb:F2} / {GpuInfo.MemoryTotalGb:F1} GB";
		}
	}

	public double VramPercent => GpuInfo?.MemoryPercent ?? 0.0;

	public string RamUsedText
	{
		get
		{
			string ramUsedText;
			if (ServerStatus.RamUsedGb > 0.0)
			{
				ramUsedText = $"{ServerStatus.RamUsedGb:F1} / {s_totalRamGb:F1} GB";
				if (ServerStatus.RamCommittedGb > 0.0)
				{
					ramUsedText += $" · выделено: {ServerStatus.RamCommittedGb:F1} GB";
				}
			}
			else
			{
				ramUsedText = $"Сервер не запущен / {s_totalRamGb:F1} GB";
			}
			if (ServerStatus.CacheRamLimitGb > 0.0)
			{
				ramUsedText += $" · кэш: до {ServerStatus.CacheRamLimitGb:F1} GB";
			}
			return ramUsedText;
		}
	}

	public double RamPercent
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

	public string GpuTempText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "—";
			}
			return $"{GpuInfo.TemperatureCelsius:F0}°C";
		}
	}

	public SolidColorBrush GpuTempBrush => GetTempColor(GpuInfo?.TemperatureCelsius ?? 0.0);

	public double GpuTempPercent
	{
		get
		{
			if (GpuInfo == null)
			{
				return 0.0;
			}
			return Math.Min(GpuInfo.TemperatureCelsius / 95.0 * 100.0, 100.0);
		}
	}

	public string PowerDrawText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "—";
			}
			return $"{GpuInfo.PowerDrawWatts:F0} W / {GpuInfo.PowerLimitWatts:F0} W";
		}
	}

	public double PowerPercent => GpuInfo?.PowerPercent ?? 0.0;

	public string GpuCoreText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "—";
			}
			return $"{GpuInfo.GpuUtilization:F0}%";
		}
	}

	public double GpuCorePercent => GpuInfo?.GpuUtilization ?? 0.0;

	public string FanSpeedText
	{
		get
		{
			if (GpuInfo == null)
			{
				return "—";
			}
			return $"{GpuInfo.FanSpeed}%";
		}
	}

	public double FanSpeedPercent => GpuInfo?.FanSpeed ?? 0;

	public string ModelName
	{
		get
		{
			if (ServerStatus.ModelName == null)
			{
				return "—";
			}
			return Path.GetFileName(ServerStatus.ModelName);
		}
	}

	public bool IsRunning
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

	public bool IsStopped => ServerStatus.State == ServerState.Stopped;

	public bool ShowBarsStyle => DisplayStyle == MonitorStyle.Bars;

	public bool ShowCirclesStyle => DisplayStyle == MonitorStyle.Circles;

	public bool BarsVisible => DisplayStyle == MonitorStyle.Bars;

	public bool CirclesVisible => DisplayStyle == MonitorStyle.Circles;

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

	public string GpuMemTempLabel => _loc.T("mon.mem_temperature");

	public string PowerLabel => _loc.T("mon.power");

	public string AllGpusLabel => _loc.T("mon.all_gpus");

	public string GpuLoadLabel => _loc.T("mon.gpu_load");

	public string TotalGpuPowerLabel => _loc.T("mon.total_gpu_power");

	public string ClockLabel => _loc.T("mon.clock");

	public string ComputeCapabilityLabel => _loc.T("mon.compute_capability");

	public string GpuMonitoringLabel => _loc.T("mon.gpu_monitoring");

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

	public string MonitoringToggleText
	{
		get
		{
			if (!IsMonitoringActive)
			{
				return _loc.T("mon.monitoring_off");
			}
			return _loc.T("mon.monitoring_on");
		}
	}

	public string CheckSpeedBtn => _loc.T("mon.check_speed");

	public string CheckSpeedBtnRunning => _loc.T("mon.check_speed_running");

	public SolidColorBrush MonitoringIndicatorBrush
	{
		get
		{
			if (!IsMonitoringActive)
			{
				return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 239, 68, 68));
			}
			return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 16, 185, 129));
		}
	}

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
	public bool IsMonitoringActive
	{
		get
		{
			return _isMonitoringActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isMonitoringActive, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsMonitoringActive);
				_isMonitoringActive = value;
				OnIsMonitoringActiveChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsMonitoringActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double WindowOpacity
	{
		get
		{
			return _windowOpacity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_windowOpacity, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.WindowOpacity);
				_windowOpacity = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorOpacity = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.WindowOpacity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsAlwaysOnTop
	{
		get
		{
			return _isAlwaysOnTop;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isAlwaysOnTop, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsAlwaysOnTop);
				_isAlwaysOnTop = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorAlwaysOnTop = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsAlwaysOnTop);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public MonitorStyle DisplayStyle
	{
		get
		{
			return _displayStyle;
		}
		set
		{
			if (!EqualityComparer<MonitorStyle>.Default.Equals(_displayStyle, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DisplayStyle);
				_displayStyle = value;
				OnDisplayStyleChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DisplayStyle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowTps
	{
		get
		{
			return _showTps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showTps, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowTps);
				_showTps = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowTps = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowTps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowVram
	{
		get
		{
			return _showVram;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showVram, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowVram);
				_showVram = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowVram = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowVram);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowRam
	{
		get
		{
			return _showRam;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showRam, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowRam);
				_showRam = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowRam = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowRam);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowGpuTemp
	{
		get
		{
			return _showGpuTemp;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showGpuTemp, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowGpuTemp);
				_showGpuTemp = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowGpuTemp = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowGpuTemp);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowPowerDraw
	{
		get
		{
			return _showPowerDraw;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showPowerDraw, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowPowerDraw);
				_showPowerDraw = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowPowerDraw = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowPowerDraw);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowGpuCore
	{
		get
		{
			return _showGpuCore;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showGpuCore, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowGpuCore);
				_showGpuCore = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowGpuCore = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowGpuCore);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowFanSpeed
	{
		get
		{
			return _showFanSpeed;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showFanSpeed, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowFanSpeed);
				_showFanSpeed = value;
				// r86: persist floating-window options between launches.
				_settings.MonitorShowFanSpeed = value;
				_settings.Save();
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowFanSpeed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsBenchmarking
	{
		get
		{
			return _isBenchmarking;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isBenchmarking, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsBenchmarking);
				_isBenchmarking = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsBenchmarking);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string BenchmarkResult
	{
		get
		{
			return _benchmarkResult;
		}
		[MemberNotNull("_benchmarkResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_benchmarkResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.BenchmarkResult);
				_benchmarkResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.BenchmarkResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckSpeedCommand => checkSpeedCommand ?? (checkSpeedCommand = new AsyncRelayCommand(CheckSpeedAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncRelayCommand(StartServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncRelayCommand(StopServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestartServerCommand => restartServerCommand ?? (restartServerCommand = new AsyncRelayCommand(RestartServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleMonitoringCommand => toggleMonitoringCommand ?? (toggleMonitoringCommand = new RelayCommand(ToggleMonitoring));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> SetStyleCommand => setStyleCommand ?? (setStyleCommand = new RelayCommand<string>(SetStyle));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenFloatingWindowCommand => openFloatingWindowCommand ?? (openFloatingWindowCommand = new RelayCommand(OpenFloatingWindow));

	private static long GetTotalPhysicalMemory()
	{
		try
		{
			nint memoryStatusBuffer = Marshal.AllocHGlobal(64);
			try
			{
				Marshal.WriteInt32(memoryStatusBuffer, 64);
				if (GlobalMemoryStatusEx(memoryStatusBuffer))
				{
					return Marshal.ReadInt64(memoryStatusBuffer, 8);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(memoryStatusBuffer);
			}
		}
		catch
		{
		}
		return 68719476736L;
	}

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(nint lpBuffer);

	// r101: shared service accessors for the Benchmark Center (same singletons, no duplication).
	public IServerManager ServerManager { get; }
	public IProfileManager ProfileManager { get; }
	public ISettings BenchmarkSettings { get; }
	public ILogService BenchmarkLog { get; }
	public IGpuMonitor GpuMonitorService { get; }
	public ILocalizationService LocService { get; }
	public IDialogService DialogService { get; }

	public MonitoringViewModel(IServerManager serverManager, IProfileManager profileManager, ISettings settings, ILogService log, IGpuMonitor gpuMonitor, ILocalizationService loc, IDialogService dialog)
	{
		_serverManager = serverManager;
		_profileManager = profileManager;
		_settings = settings;
		_log = log;
		_gpuMonitor = gpuMonitor;
		_loc = loc;
		_dialog = dialog;
		ServerManager = serverManager;
		ProfileManager = profileManager;
		BenchmarkSettings = settings;
		BenchmarkLog = log;
		GpuMonitorService = gpuMonitor;
		LocService = loc;
		DialogService = dialog;
		ApplySettings();
		_gpuTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.5)
		};
		_gpuTimer.Tick += delegate
		{
			RefreshGpuAsync();
		};
		_gpuTimer.Start();
		_ = RefreshGpuAsync();
		_benchmarkClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(120.0)
		};
		serverManager.StatusChanged += delegate(object? _, ServerStatus status)
		{
			ServerStatus = status;
		};
		loc.OnLanguageChanged += OnLanguageChanged;
	}

	private void ApplySettings()
	{
		WindowOpacity = _settings.MonitorOpacity;
		IsAlwaysOnTop = _settings.MonitorAlwaysOnTop;
		// Keep the stable bar interface enabled for now. The circle implementation
		// remains in the source and can be re-enabled later without rebuilding the UI.
		DisplayStyle = MonitorStyle.Bars;
		_settings.MonitorDisplayStyle = "Bars";
		_settings.Save();
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

	public void Dispose()
	{
		_gpuTimer.Stop();
		_benchmarkClient.Dispose();
	}

	// r101: Benchmark Center updates the two big TPS numbers after its own quick test.
	public void SetBenchmarkTps(double generationTps, double promptTps)
	{
		_lastTps = generationTps;
		_lastPromptTps = promptTps;
		OnPropertyChanged("TpsText");
		OnPropertyChanged("PromptTpsText");
		OnPropertyChanged("TpsPercent");
		OnPropertyChanged("TpsBrush");
	}

	[RelayCommand]
	private async Task CheckSpeedAsync()
	{
		if (IsBenchmarking)
		{
			return;
		}
		ServerProfile serverProfile = await GetActiveProfileAsync();
		if (serverProfile == null)
		{
			BenchmarkResult = _loc.T("mon.benchmark_no_profile");
			return;
		}
		string host = (string.IsNullOrWhiteSpace(serverProfile.Host) ? (_settings.DefaultHost ?? "localhost") : serverProfile.Host);
		int port = ((serverProfile.Port > 0) ? serverProfile.Port : ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080));
		if (host == "0.0.0.0" || host == "::")
		{
			host = "127.0.0.1";
		}
		string serverUrl = $"http://{host}:{port}";
		try
		{
			IsBenchmarking = true;
			BenchmarkResult = _loc.T("mon.benchmark_running");
			OnPropertyChanged("CheckSpeedBtn");
			StringBuilder stringBuilder = new StringBuilder();
			string prompt = string.Empty;
			if (false)
			{
			stringBuilder.AppendLine("You are an expert storyteller. Below is a comprehensive reference document about fairy tales, their history, themes, and structures. Read it carefully, then write a detailed fairy tale in Russian (about 500 words). Start with 'Жили-были...' and continue until the story is complete.");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("=== REFERENCE DOCUMENT: THE ART OF FAIRY TALES ===");
			stringBuilder.AppendLine();
			string[] referenceSections = new string[12]
			{
				"In the ancient world, storytellers gathered around fires to share tales of wonder and adventure. These stories passed down through generations carried important lessons about courage, kindness, and wisdom. The best tales transported listeners to magical realms where anything was possible, creating a shared cultural heritage that bound communities together across time and space.", "The forest was a common setting for fairy tales. Deep within its ancient trees lived creatures of all kinds - talking animals, mischievous sprites, and wise old owls who held the secrets of the world. The forest was both a place of danger and wonder, where heroes found both obstacles and allies on their journeys, learning to navigate the complexities of life through their encounters with the natural world.", "Magic in fairy tales took many forms. Spells could be cast with words, objects could come to life, and transformations were common. A frog could become a prince, a pumpkin could become a carriage, and a simple wish could change the course of events. But magic always had a price, and those who used it recklessly often found themselves in trouble, learning valuable lessons about responsibility and the consequences of their actions.", "The heroes of fairy tales were often ordinary people - a young farmer, a clever girl, or a kind-hearted child. They were not born with great power or wealth, but they possessed qualities that made them special: courage in the face of danger, kindness toward those in need, and the wisdom to learn from their mistakes. These qualities allowed them to overcome seemingly insurmountable odds and achieve greatness through perseverance and good character.", "Villains in fairy tales served as obstacles that heroes had to overcome. Wicked witches, greedy kings, and cunning tricksters each presented different challenges. The witch might cast a spell that could only be broken by an act of true love. The greedy king might hoard all the wealth of the land, leaving his people to starve. The trickster might try to steal the hero's hard-earned rewards through clever deception, testing the hero's wit and resourcefulness.", "The journey of a fairy tale hero typically followed a pattern. The hero began in an ordinary world, faced a call to adventure, encountered helpers and obstacles along the way, faced a great trial or battle, and returned transformed with wisdom or treasure to share with their community. This pattern appeared in tales from every culture, suggesting that these stories spoke to something universal in the human experience of growth and self-discovery.", "Love was a common theme in fairy tales. It could be romantic love between a prince and princess, the love between siblings, the love of a parent for a child, or the love between friends. True love was often portrayed as having magical properties - it could break curses, defeat evil, and bring about happy endings. Love in fairy tales was not just an emotion but a powerful force that could transform the world and bring people together in times of need.", "The setting of fairy tales was often a kingdom or village. Castles rose above the landscape, their towers visible for miles. Villages nestled in valleys, their cottages surrounded by gardens and farms. Markets bustled with activity, and festivals brought people together to celebrate the seasons and mark important occasions. These settings provided a rich backdrop for the adventures and challenges that heroes faced, grounding the magical elements in a recognizable world.", "Animals played important roles in fairy tales. They could be loyal companions, wise advisors, or magical helpers. A faithful dog might guide its owner through a dark forest. A talking bird might reveal hidden truths. A horse might carry its rider across impossible distances. Animals in fairy tales often possessed human qualities and spoke in human language, serving as mirrors to human nature and offering guidance to those who were willing to listen.", "The passage of time was an important element. Seasons changed, characters grew older, and events unfolded over days, months, or years. Sometimes a character would sleep for many years, only to wake up to a changed world. Other times, a single day might feel like an eternity as events unfolded in rapid succession. The manipulation of time in fairy tales allowed storytellers to explore themes of patience, urgency, and the fleeting nature of life.",
				"Moral lessons were woven into fairy tales. Stories taught children about the importance of honesty, bravery, and kindness. They warned against greed, pride, and laziness. The good characters were rewarded, while the wicked ones faced consequences for their actions. These lessons helped shape the values of generations of listeners, providing a framework for understanding right and wrong in a world that could often seem confusing and unpredictable.", "The ending of a fairy tale was often satisfying. After all the trials and adventures, the hero achieved their goal. The curse was broken, the kingdom was saved, or the lost treasure was found. Characters found happiness, whether through marriage, friendship, or personal growth. The phrase 'and they lived happily ever after' signaled that the story had reached its proper conclusion, offering a sense of closure and hope to the audience."
			};
			for (int i = 0; i < 12; i++)
			{
				StringBuilder builder = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, builder);
				handler.AppendLiteral("--- SECTION ");
				handler.AppendFormatted(i + 1);
				handler.AppendLiteral(" ---");
				builder.AppendLine(ref handler);
				string[] sections = referenceSections;
				foreach (string section in sections)
				{
					stringBuilder.AppendLine(section);
				}
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("=== END OF REFERENCE DOCUMENT ===");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Now, using all the themes, elements, and ideas from the reference material above, write a detailed fairy tale in Russian (about 500 words). Start with 'Жили-были...' and continue until the story is complete.");
			prompt = "Сгенерируй непрерывный связный текст на русском языке примерно на 1200 слов. Не используй списки, не завершай ответ раньше времени и продолжай генерацию до достижения лимита. Это тест скорости генерации модели.";
			}
			double measuredPromptTps = 0.0;
			// First request measures prompt processing only. Generation is limited to one
			// token, so its response speed cannot contaminate the prompt measurement.
			string promptProbe = "Use this context only to measure prompt processing speed.\n" + string.Concat(Enumerable.Repeat("Это нейтральный контекст для измерения скорости обработки входного prompt. Не отвечай подробно. ", 900));
			using (StringContent promptProbeContent = new StringContent(JsonSerializer.Serialize(new
			{
				model = "local-model",
				prompt = promptProbe,
				max_tokens = 1,
				stream = false,
				cache_prompt = false
			}), Encoding.UTF8, "application/json"))
			{
				using HttpResponseMessage promptProbeResponse = await _benchmarkClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, serverUrl + "/v1/completions") { Content = promptProbeContent }, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
				if (promptProbeResponse.IsSuccessStatusCode)
				{
					string promptProbeJson = await promptProbeResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
					using JsonDocument promptProbeDocument = JsonDocument.Parse(promptProbeJson);
					if (promptProbeDocument.RootElement.TryGetProperty("timings", out var probeTimings) && probeTimings.TryGetProperty("prompt_per_second", out var probeSpeed) && probeSpeed.TryGetDouble(out var promptSpeed))
					{
						measuredPromptTps = promptSpeed;
					}
				}
			}
			// Second request follows the real workload used by the user.
			prompt = "Build a single-file HTML visualization of this screenshot using three.js. Include OrbitControls. Create an accurate, detailed representation. Make the edges of most furniture visibly rounded, to make the objects look smoother and more realistic. Use high-quality lighting while ensuring visibility like in the screenshot. Prevent Z-fighting. Ensure code correctness.";
			StringContent content = new StringContent(JsonSerializer.Serialize(new
			{
				model = "local-model",
				prompt = prompt,
				temperature = 0.7,
				top_p = 0.9,
				max_tokens = 1536,
				stream = true,
				cache_prompt = false,
				stream_options = new { include_usage = true }
			}), Encoding.UTF8, "application/json");
			Stopwatch stopwatch = Stopwatch.StartNew();
			DateTime requestStartTime = DateTime.UtcNow;
			using HttpResponseMessage response = await _benchmarkClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, serverUrl + "/v1/completions") { Content = content }, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				string errorBody = await response.Content.ReadAsStringAsync();
				if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
				{
					BenchmarkResult = _loc.T("mon.benchmark_server_loading");
					IsBenchmarking = false;
					OnPropertyChanged("CheckSpeedBtn");
					return;
				}
				throw new Exception($"{_loc.T("mon.benchmark_error")}: {response.StatusCode} - {errorBody.Substring(0, Math.Min(200, errorBody.Length))}");
			}
			int generatedTokens = 0;
			DateTime? firstTokenTime = null;
			int promptTokens = 0;
			int chunkCount = 0;
			double? serverPromptTps = null;
			double? serverGenerationTps = null;
			using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
			using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
			while (!reader.EndOfStream)
			{
				string line = await reader.ReadLineAsync().ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
				{
					continue;
				}
				string data = line.Substring(6).Trim();
				if (data == "[DONE]")
				{
					break;
				}
				try
				{
					using JsonDocument jsonDocument = JsonDocument.Parse(data);
					JsonElement rootElement = jsonDocument.RootElement;
					if (rootElement.TryGetProperty("usage", out var usage))
					{
						if (usage.TryGetProperty("prompt_tokens", out var promptTokensElement))
						{
							promptTokens = promptTokensElement.GetInt32();
						}
						if (usage.TryGetProperty("completion_tokens", out var completionTokensElement))
						{
							generatedTokens = Math.Max(generatedTokens, completionTokensElement.GetInt32());
						}
					}
					if (rootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
					{
						foreach (JsonElement choice in choices.EnumerateArray())
						{
							if (choice.ValueKind != JsonValueKind.Undefined && choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var deltaContent) && !string.IsNullOrEmpty(deltaContent.GetString()))
							{
								generatedTokens++;
								if (!firstTokenTime.HasValue)
								{
									firstTokenTime = DateTime.UtcNow;
								}
							}
							else if (choice.ValueKind != JsonValueKind.Undefined && choice.TryGetProperty("text", out var completionText) && !string.IsNullOrEmpty(completionText.GetString()))
							{
								generatedTokens++;
								if (!firstTokenTime.HasValue)
								{
									firstTokenTime = DateTime.UtcNow;
								}
							}
						}
					}
					chunkCount++;
					// A fast streamed response can complete many reads synchronously.
					// Yield periodically so Avalonia can continue processing the window.
					if ((chunkCount & 15) == 0)
					{
						await Task.Delay(1).ConfigureAwait(false);
					}
					// llama-server includes authoritative timings in the final SSE event.
					// Prefer these values over counting chunks, since one chunk may contain
					// more than one token and OpenAI-compatible usage is optional.
					if (rootElement.TryGetProperty("timings", out var timings) && timings.ValueKind == JsonValueKind.Object)
					{
						if (timings.TryGetProperty("prompt_per_second", out var promptPerSecond) && promptPerSecond.TryGetDouble(out var promptSpeed))
						{
							serverPromptTps = promptSpeed;
						}
						if (timings.TryGetProperty("predicted_per_second", out var predictedPerSecond) && predictedPerSecond.TryGetDouble(out var generationSpeed))
						{
							serverGenerationTps = generationSpeed;
						}
					}
					if (chunkCount <= 3)
					{
						_log.Information($"Benchmark chunk #{chunkCount}: {data.Substring(0, Math.Min(200, data.Length))}", "Monitoring");
					}
				}
				catch (Exception ex)
				{
					if (chunkCount <= 3)
					{
						_log.Warning("Benchmark chunk parse error: " + ex.Message + " | Line: " + line.Substring(0, Math.Min(200, line.Length)), "Monitoring");
					}
				}
			}
			stopwatch.Stop();
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
			_log.Information($"Benchmark: chunks={chunkCount}, firstToken={firstTokenTime.HasValue}, promptMs_raw={(firstTokenTime.HasValue ? (firstTokenTime.Value - requestStartTime).TotalMilliseconds : 0.0):F0}, totalMs={elapsedMilliseconds:F0}", "Monitoring");
			double generationElapsedMs = 0.0;
			double promptElapsedMs;
			if (firstTokenTime.HasValue)
			{
				promptElapsedMs = (firstTokenTime.Value - requestStartTime).TotalMilliseconds;
				generationElapsedMs = (double)elapsedMilliseconds - promptElapsedMs;
			}
			else
			{
				promptElapsedMs = elapsedMilliseconds;
			}
			if (promptTokens <= 0)
			{
				promptTokens = Math.Max(1, (int)((double)prompt.Length / 3.8));
			}
			if (generatedTokens <= 0)
			{
				generatedTokens = 2048;
			}
		double promptTps = (measuredPromptTps > 0.0) ? measuredPromptTps : (serverPromptTps ?? ((promptElapsedMs > 0.0) ? ((double)promptTokens / (promptElapsedMs / 1000.0)) : 0.0));
			double generationTps = serverGenerationTps ?? ((generationElapsedMs > 0.0) ? ((double)generatedTokens / (generationElapsedMs / 1000.0)) : 0.0);
			double totalElapsedSeconds = (double)elapsedMilliseconds / 1000.0;
			if (generatedTokens > 0)
			{
				_ = generationElapsedMs / (double)generatedTokens;
			}
			_log.Information($"Benchmark streaming: prompt={promptTps:F1} tok/s ({promptTokens} tok, {promptElapsedMs:F0}ms), gen={generationTps:F1} tok/s ({generatedTokens} tok, {generationElapsedMs:F0}ms), total={totalElapsedSeconds:F1}s", "Monitoring");
			_lastTps = generationTps;
			_lastPromptTps = promptTps;
			OnPropertyChanged("TpsText");
			OnPropertyChanged("PromptTpsText");
			OnPropertyChanged("TpsPercent");
			OnPropertyChanged("TpsBrush");
			BenchmarkResult = "";
			// r107: feed the Benchmark Center with this quick test — the charts
			// (bars) and the history dropdown get the result, nothing is empty.
			BenchmarkCenterViewModel.Current?.RecordQuickTest(generationTps, promptTps, promptTokens, generatedTokens, promptElapsedMs, totalElapsedSeconds);
		}
		catch (Exception ex2)
		{
			BenchmarkResult = _loc.T("mon.benchmark_error") + ": " + ex2.Message;
			_log.Error(ex2, "Speed benchmark failed", "Monitoring");
		}
		finally
		{
			IsBenchmarking = false;
			OnPropertyChanged("CheckSpeedBtn");
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
			IReadOnlyList<GpuInfo> localGpuInfos = await _gpuMonitor.GetGpuInfosAsync();
			List<GpuInfo> gpuInfos = localGpuInfos.ToList();
			if (_remoteGpuInfo != null)
			{
				gpuInfos.Add(_remoteGpuInfo);
			}
			if (gpuInfos.Count > 0)
			{
				GpuInfos = gpuInfos;
				GpuInfo = gpuInfos[0];
				OnPropertyChanged("VramUsedText");
				OnPropertyChanged("VramPercent");
				OnPropertyChanged("GpuTempText");
				OnPropertyChanged("GpuTempBrush");
				OnPropertyChanged("GpuTempPercent");
				OnPropertyChanged("PowerDrawText");
				OnPropertyChanged("PowerPercent");
				OnPropertyChanged("GpuCoreText");
				OnPropertyChanged("GpuCorePercent");
				OnPropertyChanged("FanSpeedText");
				OnPropertyChanged("FanSpeedPercent");
				OnPropertyChanged("ShowSingleGpuMonitor");
				OnPropertyChanged("AllGpuVramText");
				OnPropertyChanged("AllGpuLoadText");
				OnPropertyChanged("AllGpuPowerText");
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

	public Task RefreshNowAsync() => RefreshGpuAsync();

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

	[RelayCommand]
	private async Task StopServerAsync()
	{
		await _serverManager.StopAsync();
	}

	[RelayCommand]
	private async Task RestartServerAsync()
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
		ServerProfile serverProfile = await GetActiveProfileAsync();
		if (serverProfile != null && !string.IsNullOrWhiteSpace(serverProfile.ModelPath))
		{
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
	private void ToggleMonitoring()
	{
		IsMonitoringActive = !IsMonitoringActive;
	}

	[RelayCommand]
	private void SetStyle(string? styleName)
	{
		DisplayStyle = styleName switch
		{
			"Bars" => MonitorStyle.Bars, 
			"Circles" => MonitorStyle.Circles, 
			"Compact" => MonitorStyle.Compact, 
			_ => DisplayStyle, 
		};
	}

	[RelayCommand]
	private void OpenFloatingWindow()
	{
		try
		{
			if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
			{
				Window window = classicDesktopStyleApplicationLifetime.Windows.FirstOrDefault((Window w) => w is MonitoringWindow);
				if (window != null)
				{
					window.Show();
					window.Activate();
				}
				else
				{
					new MonitoringWindow(this).Show();
				}
			}
		}
		catch
		{
		}
	}

	private static SolidColorBrush GetTempColor(double temp)
	{
		byte r;
		byte g;
		byte b;
		if (temp <= 55.0)
		{
			r = 16;
			g = 185;
			b = 129;
		}
		else if (temp <= 70.0)
		{
			double midFactor = (temp - 55.0) / 15.0;
			r = (byte)(16.0 + 235.0 * midFactor);
			g = (byte)(185.0 + 6.0 * midFactor);
			b = (byte)(129.0 + -93.0 * midFactor);
		}
		else
		{
			double highFactor = Math.Min((temp - 70.0) / 20.0, 1.0);
			r = (byte)(251.0 + -12.0 * highFactor);
			g = (byte)(191.0 + -123.0 * highFactor);
			b = (byte)(36.0 + 32.0 * highFactor);
		}
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, r, g, b));
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] propertyNames = new string[33]
		{
			"Title", "ServerControlsLabel", "ModelLabel", "StartBtn", "StopBtn", "RestartBtn", "TpsLabel", "TpsPromptLabel", "GenLabel", "VramLabel",
			"RamLabel", "GpuTempLabel", "GpuMemTempLabel", "PowerLabel", "GpuCoreLabel", "FanLabel", "SettingsTitle", "OpacityLabel", "AlwaysOnTopLabel", "VisibleMetricsLabel", "StyleTitle",
			"StyleBars", "StyleCircles", "OpenFloatingBtn", "MonitoringToggleText", "MonitoringIndicatorBrush", "CheckSpeedBtn", "AllGpusLabel", "GpuLoadLabel",
			"TotalGpuPowerLabel", "ClockLabel", "ComputeCapabilityLabel", "GpuMonitoringLabel"
		};
		foreach (string propertyName in propertyNames)
		{
			OnPropertyChanged(propertyName);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnServerStatusChanged(ServerStatus value)
	{
		_lastTps = value.TokensPerSecond;
		// The speed benchmark obtains prompt throughput directly from the
		// completion response. A subsequent live-status refresh commonly has
		// no prompt timing (0), which used to erase that benchmark result in
		// the Web UI immediately after it was displayed.
		if (value.PromptTokensPerSecond > 0.0)
		{
			_lastPromptTps = value.PromptTokensPerSecond;
		}
		OnPropertyChanged("TpsText");
		OnPropertyChanged("PromptTpsText");
		OnPropertyChanged("TpsPercent");
		OnPropertyChanged("TpsBrush");
		OnPropertyChanged("RamUsedText");
		OnPropertyChanged("RamPercent");
		OnPropertyChanged("ModelName");
		OnPropertyChanged("IsRunning");
		OnPropertyChanged("IsStopped");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsMonitoringActiveChanged(bool value)
	{
		if (value)
		{
			_gpuTimer.Start();
		}
		else
		{
			_gpuTimer.Stop();
		}
		OnPropertyChanged("MonitoringToggleText");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnDisplayStyleChanged(MonitorStyle value)
	{
		_settings.MonitorDisplayStyle = value.ToString();
		_settings.Save();
		OnPropertyChanged("ShowBarsStyle");
		OnPropertyChanged("ShowCirclesStyle");
		OnPropertyChanged("BarsVisible");
		OnPropertyChanged("CirclesVisible");
	}
}
