using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Core.Services;
using LlamaStudio.Infrastructure.Llama;

namespace LlamaStudio.ViewModels;

/// <summary>
/// r101: Benchmark Center view model.
/// Orchestrates server benchmarks (HTTP to the running llama-server, never
/// restarting it) and engine benchmarks (llama-bench from the same build,
/// with the stop-server → bench → always-restore flow). Reuses the single
/// IServerManager/IGpuMonitor/ISettings/ILogService/IDialogService singletons
/// from the MonitoringViewModel — no second managers.
/// NOTE: source generators are disabled in the LS build, so observable
/// properties and relay commands are hand-written (decompiled style).
/// </summary>

/// <summary>r114: status severity (language-independent; the UI colors by this).</summary>
public enum BenchmarkStatusKind { Ok, Warning, Error }

public class BenchmarkCenterViewModel : ObservableObject
{
	/// <summary>
	/// r105: process-wide reference to the (DI singleton) benchmark VM.
	/// Controls and the Server page use it so a running test is visible
	/// everywhere (tabs, server start button) even though pages are rebuilt
	/// on navigation. The DI singleton is constructed at startup, so the
	/// first instance created wins.
	/// </summary>
	public static BenchmarkCenterViewModel? Current { get; private set; }

	/// <summary>r105: raised on the UI thread whenever a test starts/stops.</summary>
	public static event Action? InstanceRunningChanged;

	private readonly MonitoringViewModel _monitor;
	private readonly BenchmarkRunner _runner;
	private CancellationTokenSource? _cts;

	// ==================== observable state ====================

	private bool _isOpen;
	public bool IsOpen
	{
		get => _isOpen;
		set { if (_isOpen != value) { _isOpen = value; OnPropertyChanged(nameof(IsOpen)); } }
	}

	private bool _isRunning;
	public bool IsRunning
	{
		get => _isRunning;
		set { if (_isRunning != value) { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); } }
	}

	private int _preset; // 0 Quick, 1 Standard, 2 Thorough
	public int Preset
	{
		get => _preset;
		set { if (_preset != value) { _preset = value; OnPropertyChanged(nameof(Preset)); } }
	}

	private bool _manualPointsMode;
	public bool ManualPointsMode
	{
		get => _manualPointsMode;
		set { if (_manualPointsMode != value) { _manualPointsMode = value; OnPropertyChanged(nameof(ManualPointsMode)); } }
	}

	private string _manualPointsText = "16K, 32K, 64K, 128K";
	public string ManualPointsText
	{
		get => _manualPointsText;
		set { if (_manualPointsText != value) { _manualPointsText = value; OnPropertyChanged(nameof(ManualPointsText)); } }
	}

	private bool _warmupEnabled = true;
	public bool WarmupEnabled
	{
		get => _warmupEnabled;
		set { if (_warmupEnabled != value) { _warmupEnabled = value; OnPropertyChanged(nameof(WarmupEnabled)); } }
	}

	private string _progressText = string.Empty;
	public string ProgressText
	{
		get => _progressText;
		set { if (_progressText != value) { _progressText = value; OnPropertyChanged(nameof(ProgressText)); } }
	}

	private double _progressPercent;
	public double ProgressPercent
	{
		get => _progressPercent;
		set { if (Math.Abs(_progressPercent - value) > 0.001) { _progressPercent = value; OnPropertyChanged(nameof(ProgressPercent)); } }
	}

	private bool _pointRunning;
	/// <summary>True while a single measurement point is in flight (bar shows animated state).</summary>
	public bool PointRunning
	{
		get => _pointRunning;
		set { if (_pointRunning != value) { _pointRunning = value; OnPropertyChanged(nameof(PointRunning)); } }
	}

	private string _statusText = string.Empty;
	public string StatusText
	{
		get => _statusText;
		set { if (_statusText != value) { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }
	}

	/// <summary>r114: language-independent status severity (the control colors
	/// the status line by this, not by RU string prefixes).</summary>
	private BenchmarkStatusKind _statusKind = BenchmarkStatusKind.Ok;
	public BenchmarkStatusKind StatusKind
	{
		get => _statusKind;
		private set { if (_statusKind != value) { _statusKind = value; OnPropertyChanged(nameof(StatusKind)); } }
	}

	/// <summary>r114: localization passthrough (the VM exposes its strings via
	/// keys so the UI follows the app language).</summary>
	public string T(string key) => _monitor.Loc.T(key);

	private string _capabilitiesText = string.Empty;
	public string CapabilitiesText
	{
		get => _capabilitiesText;
		set { if (_capabilitiesText != value) { _capabilitiesText = value; OnPropertyChanged(nameof(CapabilitiesText)); } }
	}

	private string _environmentSummary = string.Empty;
	public string EnvironmentSummary
	{
		get => _environmentSummary;
		set { if (_environmentSummary != value) { _environmentSummary = value; OnPropertyChanged(nameof(EnvironmentSummary)); } }
	}

	private string _mtpText = "—";
	public string MtpText
	{
		get => _mtpText;
		set { if (_mtpText != value) { _mtpText = value; OnPropertyChanged(nameof(MtpText)); } }
	}

	private string _quickResultExtra = string.Empty;
	public string QuickResultExtra
	{
		get => _quickResultExtra;
		set { if (_quickResultExtra != value) { _quickResultExtra = value; OnPropertyChanged(nameof(QuickResultExtra)); } }
	}



	private string _errorText = string.Empty;
	public string ErrorText
	{
		get => _errorText;
		set { if (_errorText != value) { _errorText = value; OnPropertyChanged(nameof(ErrorText)); } }
	}

	private BenchmarkRun? _selectedRun;
	public BenchmarkRun? SelectedRun
	{
		get => _selectedRun;
		set { if (!ReferenceEquals(_selectedRun, value)) { _selectedRun = value; OnPropertyChanged(nameof(SelectedRun)); } }
	}

	public ObservableCollection<string> CompareSelection { get; } = new();
	public ObservableCollection<BenchmarkPoint> CurrentPoints { get; } = new();
	public ObservableCollection<BenchmarkRun> HistoryRuns { get; } = new();
	public ObservableCollection<CompareRowViewModel> CompareRows { get; } = new();

	public string PresetName => Preset switch { 0 => "Quick", 1 => "Standard", 2 => "Thorough", _ => "Standard" };

	private int _selectedTest;
	/// <summary>r103: currently selected test (0 Quick, 1 Prefill, 2 Depth, 3 Engine, 4 Full). Selection only — Start launches it.</summary>
	public int SelectedTest
	{
		get => _selectedTest;
		set { if (_selectedTest != value) { _selectedTest = value; OnPropertyChanged(nameof(SelectedTest)); } }
	}

	public string SelectedTestName => SelectedTest switch { 0 => "Prefill", 1 => "Depth", 2 => "Engine", 3 => "Full", _ => "—" };

	// ==================== commands ====================

	private IAsyncRelayCommand? _runQuickCommand;
	public IAsyncRelayCommand RunQuickCommand => _runQuickCommand ?? (_runQuickCommand = new AsyncRelayCommand(RunQuickAsync));

	private IAsyncRelayCommand? _runPrefillCommand;
	public IAsyncRelayCommand RunPrefillCommand => _runPrefillCommand ?? (_runPrefillCommand = new AsyncRelayCommand(RunPrefillAsync));

	private IAsyncRelayCommand? _runDepthCommand;
	public IAsyncRelayCommand RunDepthCommand => _runDepthCommand ?? (_runDepthCommand = new AsyncRelayCommand(RunDepthAsync));

	private IAsyncRelayCommand? _runEngineCommand;
	public IAsyncRelayCommand RunEngineCommand => _runEngineCommand ?? (_runEngineCommand = new AsyncRelayCommand(RunEngineAsync));

	private IAsyncRelayCommand? _runFullCommand;
	public IAsyncRelayCommand RunFullCommand => _runFullCommand ?? (_runFullCommand = new AsyncRelayCommand(RunFullAsync));

	private IRelayCommand? _stopCommand;
	public IRelayCommand StopCommand => _stopCommand ?? (_stopCommand = new RelayCommand(Stop));

	private IRelayCommand<BenchmarkRun>? _selectRunCommand;
	public IRelayCommand<BenchmarkRun> SelectRunCommand => _selectRunCommand ?? (_selectRunCommand = new RelayCommand<BenchmarkRun>(SelectRun));

	private IRelayCommand<BenchmarkRun>? _toggleCompareCommand;
	public IRelayCommand<BenchmarkRun> ToggleCompareCommand => _toggleCompareCommand ?? (_toggleCompareCommand = new RelayCommand<BenchmarkRun>(ToggleCompare));

	private IRelayCommand? _clearCompareCommand;
	public IRelayCommand ClearCompareCommand => _clearCompareCommand ?? (_clearCompareCommand = new RelayCommand(ClearCompare));

	private IRelayCommand<int>? _selectTestCommand;
	public IRelayCommand<int> SelectTestCommand => _selectTestCommand ?? (_selectTestCommand = new RelayCommand<int>(SelectTest));

	private IAsyncRelayCommand? _startCommand;
	public IAsyncRelayCommand StartCommand => _startCommand ?? (_startCommand = new AsyncRelayCommand(StartAsync));

	public BenchmarkCenterViewModel(MonitoringViewModel monitor)
	{
		_monitor = monitor;
		_runner = new BenchmarkRunner(monitor.BenchmarkSettings, monitor.BenchmarkLog, monitor.GpuMonitorService);
		Current ??= this;
		RefreshHistory();
	}

	// ==================== helpers ====================

	private (string Host, int Port) GetServerTarget(ServerProfile profile)
	{
		string host = string.IsNullOrWhiteSpace(profile.Host)
			? (_monitor.BenchmarkSettings.DefaultHost ?? "127.0.0.1")
			: profile.Host;
		int port = profile.Port > 0 ? profile.Port : (_monitor.BenchmarkSettings.DefaultPort > 0 ? _monitor.BenchmarkSettings.DefaultPort : 8080);
		if (host == "0.0.0.0" || host == "::")
		{
			host = "127.0.0.1";
		}
		return (host, port);
	}

	public async Task RefreshEnvironmentSummaryAsync()
	{
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			EnvironmentSummary = T("bench.env_no_profile");
			return;
		}
		int safeMax = BenchmarkContextPointGenerator.ComputeSafeMaxContext(profile.ContextSize, 1024);
		EnvironmentSummary = string.Join(" · ", new[]
		{
			profile.Name,
			Path.GetFileName(profile.ModelPath ?? ""),
			T("bench.env_context") + (profile.ContextSize / 1024) + "K",
			T("bench.env_safe_max") + BenchmarkContextPointGenerator.FormatPoint(safeMax),
			T("bench.env_gpu_layers") + profile.GetEffectiveGpuLayers(),
			"B" + profile.BatchSize + "/u" + profile.UbatchSize,
			profile.FlashAttention ? "FA" : "noFA"
		});
	}

	// ==================== run lifecycle ====================

	private void BeginRun(string stage)
	{
		IsRunning = true;
		IsOpen = true; // show progress/charts immediately
		ErrorText = string.Empty;
		QuickResultExtra = string.Empty;
		CurrentPoints.Clear();
		ProgressText = stage;
		ProgressPercent = 0;
		_cts = new CancellationTokenSource();
		RaiseRunningChanged();
	}

	/// <summary>r105: notify other surfaces (server page, banners) that a test started/stopped.</summary>
	private static void RaiseRunningChanged()
	{
		var handlers = InstanceRunningChanged;
		if (handlers == null)
		{
			return;
		}
		try
		{
			Avalonia.Threading.Dispatcher.UIThread.Post(() => handlers.Invoke());
		}
		catch
		{
			// non-UI caller and no dispatcher — fire inline
			handlers.Invoke();
		}
	}

	private async Task<BenchmarkRun> NewRunAsync(BenchmarkType type, string name)
	{
		var profile = await _monitor.GetActiveProfileAsync();
		ServerProfile profileRef = profile!;
		string? serverExe = null;
		try
		{
			var status = await _monitor.ServerManager.GetStatusAsync();
			serverExe = _runner.ResolveServerExecutableAsync(profileRef, status.ProcessId);
		}
		catch
		{
			serverExe = _runner.ResolveServerExecutableAsync(profileRef, null);
		}
		string? benchExe = serverExe == null ? null : BenchmarkPaths.ResolveBenchPath(serverExe, BenchmarkRunner.IsWindows);
		List<BenchmarkGpuSnapshot> gpus = new();
		try
		{
			await using var sampler = _runner.StartGpuSampling(TimeSpan.FromMilliseconds(700));
			await Task.Delay(800);
			gpus = sampler.Snapshot();
		}
		catch
		{
			// GPU data best-effort
		}
		var env = _runner.BuildEnvironmentSnapshot(profileRef, serverExe, benchExe, gpus);
		return new BenchmarkRun
		{
			DisplayName = name,
			Type = type,
			Environment = env,
			Settings = new BenchmarkRunSettings
			{
				Preset = PresetName,
				Repetitions = Preset switch { 0 => 1, 1 => 3, 2 => 5, _ => 3 },
				OutputTokens = 1024,
				Warmup = WarmupEnabled,
				UseAutoPoints = !ManualPointsMode,
				ManualPoints = ManualPointsText
			}
		};
	}

	private async Task EndRun(BenchmarkRun run, bool cancelled = false, string? extraStatus = null)
	{
		bool allErrors = run.Points.Count > 0 && run.Points.All(p => p.Tag.StartsWith(T("bench.error_prefix"), StringComparison.Ordinal));
		run.Status = cancelled
			? (allErrors ? BenchmarkStatus.Failed : (run.Points.Count > 0 ? BenchmarkStatus.Partial : BenchmarkStatus.Cancelled))
			: (allErrors ? BenchmarkStatus.Failed : (run.Points.Count > 0 ? BenchmarkStatus.Completed : BenchmarkStatus.Failed));
		try
		{
			_runner.SaveRun(run);
			RefreshHistory();
			SelectedRun = run;
			BuildCompareRows();
		}
		catch (Exception ex)
		{
			_monitor.BenchmarkLog.Error("Failed to save benchmark run: " + ex.Message, "Benchmark");
		}
		StatusText = extraStatus ?? run.Status switch
		{
			BenchmarkStatus.Completed => string.Format(T("bench.status_completed"), run.Points.Count),
			BenchmarkStatus.Partial => string.Format(T("bench.status_partial"), run.Points.Count),
			BenchmarkStatus.Cancelled => run.Points.Count > 0
				? string.Format(T("bench.status_cancelled_points"), run.Points.Count)
				: T("bench.status_cancelled"),
			_ => string.Format(T("bench.status_failed"), run.Error ?? T("bench.err_journal"))
		};
		// r114: language-independent severity for the UI status color.
		StatusKind = run.Status switch
		{
			BenchmarkStatus.Completed => BenchmarkStatusKind.Ok,
			BenchmarkStatus.Partial => BenchmarkStatusKind.Warning,
			BenchmarkStatus.Cancelled => BenchmarkStatusKind.Warning,
			_ => BenchmarkStatusKind.Error
		};
		if (extraStatus != null)
		{
			StatusKind = BenchmarkStatusKind.Error;
		}
		IsRunning = false;
		PointRunning = false;
		ProgressText = string.Empty;
		RaiseRunningChanged();
	}

	// ==================== r103: select + start ====================

	private void SelectTest(int test)
	{
		if (IsRunning)
		{
			return;
		}
		SelectedTest = test;
		ErrorText = string.Empty;
	}

	/// <summary>r103: launches the SELECTED test. Server tests require a running llama-server.</summary>
	private async Task StartAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		// r107: 4 selectable tests — Quick moved to the Monitoring speed card
		// (it still feeds the charts + history via RecordQuickTest).
		int t = SelectedTest;
		if (t != 2) // Engine runs llama-bench standalone — server state irrelevant
		{
			ServerState state;
			try
			{
				var status = await _monitor.ServerManager.GetStatusAsync();
				state = status.State;
			}
			catch
			{
				state = ServerState.Stopped;
			}
			if (state != ServerState.Running)
			{
				ErrorText = T("bench.err_server_not_running");
				return;
			}
		}
		switch (t)
		{
			case 0: await RunPrefillAsync(); break;
			case 1: await RunDepthAsync(); break;
			case 2: await RunEngineAsync(); break;
			case 3: await RunFullAsync(); break;
		}
	}

	/// <summary>r103: per-point timeout so a hung request cannot stall the whole sweep.</summary>
	private CancellationToken PointCancellationToken()
	{
		var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token);
		linked.CancelAfter(TimeSpan.FromMinutes(15));
		return linked.Token;
	}

	// ==================== QUICK (server) ====================

	private async Task RunQuickAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		var (host, port) = GetServerTarget(profile);
		BeginRun(T("bench.quick_running"));
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var run = await NewRunAsync(BenchmarkType.QuickServer, string.Format(T("bench.quick_done"), profile.Name));
		try
		{
			await using var sampler = _runner.StartGpuSampling(TimeSpan.FromMilliseconds(500));
			PointRunning = true;
			// r106: the SAME flow as the Monitoring quick test — prefill probe on a
			// large context + streamed generation; both PP and TG end up in the result.
			var point = await _runner.ServerQuickMonitorAsync(host, port, profile.Name, PointCancellationToken());
			var (mtpPercent, mtpAcc, mtpGen, mtpLen) = _runner.ReadMtpAcceptanceFromLogs(_monitor.BenchmarkLog.GetRecentLogs(200));
			point.MtpAcceptancePercent = mtpPercent;
			point.MtpAccepted = mtpAcc;
			point.MtpGeneratedDraft = mtpGen;
			point.MtpMeanLen = mtpLen;
			AttachGpuToPoints(new[] { point }, sampler.Snapshot());
			MtpText = FormatMtp(mtpPercent, mtpAcc, mtpGen, mtpLen);
			// Update the two big numbers on the monitoring card (spec §1: keep them).
			_monitor.SetBenchmarkTps(point.GenerationTps, point.PrefillTps);
			run.Points.Add(point);
			run.DurationS = sw.Elapsed.TotalSeconds;
			QuickResultExtra = string.Format(CultureInfo.InvariantCulture,
				"PP {0:0.#} tok/s (проба {1} ток.) · TG {2:0.#} tok/s · TTFT {3:0} мс · {4} токенов · {5:0.##} с",
				point.PrefillTps, point.PromptTokensActual, point.GenerationTps, point.TtftMs, point.GeneratedTokens, point.WallTimeS);
			CurrentPoints.Add(point);
			await EndRun(run);
		}
		catch (OperationCanceledException)
		{
			run.Error = T("bench.status_cancelled");
			await EndRun(run, cancelled: true);
		}
		catch (Exception ex)
		{
			run.Error = ex.Message;
			_monitor.BenchmarkLog.Error("Quick benchmark failed: " + ex, "Benchmark");
			ErrorText = FriendlyError(ex);
			await EndRun(run);
		}
	}

	// ==================== shared server sweep ====================

	private async Task<bool> RunServerSweepAsync(ServerProfile profile, string title, bool isDepth)
	{
		int outputReserve = isDepth ? 512 : 1024;
		int safeMax = BenchmarkContextPointGenerator.ComputeSafeMaxContext(profile.ContextSize, outputReserve);
		int[] points;
		if (ManualPointsMode)
		{
			var (parsed, errors) = BenchmarkContextPointGenerator.ParseManualPoints(ManualPointsText, safeMax);
			if (errors.Count > 0)
			{
				ErrorText = string.Join(" · ", errors);
				return false;
			}
			points = parsed;
		}
		else
		{
			points = BenchmarkContextPointGenerator.GenerateAutoPoints(safeMax);
		}
		var (host, port) = GetServerTarget(profile);
		BeginRun(title + ": " + points.Length + " точек");
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var run = await NewRunAsync(isDepth ? BenchmarkType.ContextDepth : BenchmarkType.PrefillSweep, (isDepth ? "Depth — " : "Prefill — ") + profile.Name);
		run.Settings.RequestedPoints = points.ToList();
		bool ok = true;
		try
		{
			await using var sampler = _runner.StartGpuSampling(TimeSpan.FromMilliseconds(500));
			for (int i = 0; i < points.Length; i++)
			{
				int target = points[i];
				ProgressText = isDepth
					? string.Format(T("bench.progress_point"), i + 1, points.Length, BenchmarkContextPointGenerator.FormatPoint(target)) + T("bench.progress_depth_suffix")
					: string.Format(T("bench.progress_point"), i + 1, points.Length, BenchmarkContextPointGenerator.FormatPoint(target));
				ProgressPercent = 100.0 * i / points.Length;
				PointRunning = true;
				try
				{
					var point = isDepth
						? await _runner.ServerDepthPointAsync(host, port, profile.Name, target, 256, PointCancellationToken())
						: await _runner.ServerPrefillPointAsync(host, port, profile.Name, target, PointCancellationToken());
					AttachGpuToPoints(new[] { point }, sampler.Snapshot());
					sampler.Reset();
					run.Points.Add(point);
					CurrentPoints.Add(point);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					ok = false;
					PointRunning = false;
					_monitor.BenchmarkLog.Warning((isDepth ? "Depth" : "Prefill") + " point " + target + " failed: " + ex.Message, "Benchmark");
					string err = ex.Message ?? string.Empty;
					run.Points.Add(new BenchmarkPoint
					{
						Label = BenchmarkContextPointGenerator.FormatPoint(target),
						ContextRequested = target,
						Tag = T("bench.error_prefix") + err.Substring(0, Math.Min(80, err.Length))
					});
					if (IsConnectionLost(err))
					{
						// Don't grind through the remaining points against a dead server.
						ErrorText = "llama-server недоступен — тест остановлен. Запустите сервер и повторите.";
						break;
					}
				}
				PointRunning = false;
				ProgressPercent = 100.0 * (i + 1) / points.Length;
			}
			run.DurationS = sw.Elapsed.TotalSeconds;
			await EndRun(run);
		}
		catch (OperationCanceledException)
		{
			run.DurationS = sw.Elapsed.TotalSeconds;
			ok = false;
			await EndRun(run, cancelled: true);
		}
		catch (Exception ex)
		{
			run.Error = ex.Message;
			run.DurationS = sw.Elapsed.TotalSeconds;
			ok = false;
			_monitor.BenchmarkLog.Error((isDepth ? "Depth" : "Prefill") + " sweep failed: " + ex, "Benchmark");
			ErrorText = FriendlyError(ex);
			await EndRun(run);
		}
		return ok;
	}

	private async Task RunPrefillAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		await RunServerSweepAsync(profile, "Prefill-тест", isDepth: false);
	}

	private async Task RunDepthAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		await RunServerSweepAsync(profile, "Context Depth", isDepth: true);
	}

	// ==================== ENGINE (llama-bench) ====================

	private async Task RunEngineAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		var status = await _monitor.ServerManager.GetStatusAsync();
		string? serverExe = _runner.ResolveServerExecutableAsync(profile, status.ProcessId);
		string? benchPath = serverExe == null ? null : BenchmarkPaths.ResolveBenchPath(serverExe, BenchmarkRunner.IsWindows);
		if (benchPath == null)
		{
			ErrorText = string.Format(T("bench.engine_bench_not_found"), serverExe == null ? T("bench.no_directory") : Path.GetDirectoryName(serverExe));
			return;
		}
		bool serverRunning = status.State == ServerState.Running;
		if (serverRunning)
		{
			bool confirmed = await _monitor.DialogService.ShowConfirmationAsync(
				T("bench.engine_confirm") + T("bench.continue_q"), T("bench.engine_confirm_title"));
			if (!confirmed)
			{
				return;
			}
		}
		BeginRun(T("bench.engine_caps_stage"));
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var run = await NewRunAsync(BenchmarkType.EnginePpTg, "Engine — " + profile.Name);
		bool restored = false;
		string? abortStatus = null;
		try
		{
			var caps = await _runner.DetectCapabilitiesAsync(benchPath, _cts!.Token);
			CapabilitiesText = string.Format(T("bench.engine_caps"), caps.Summary);
			_monitor.BenchmarkLog.Information("llama-bench capabilities: " + caps.Summary, "Benchmark");
			if (!caps.SupportsModel || !caps.SupportsPrompt || !caps.SupportsGen)
			{
				throw new InvalidOperationException(T("bench.engine_caps_missing"));
			}
			await _runner.ReadLlamaBenchVersionAsync(benchPath, _cts.Token);
			if (serverRunning)
			{
				ProgressText = T("bench.engine_stopping");
				await _monitor.ServerManager.StopAsync(_cts.Token);
				await WaitForGpuFreeAsync(_cts.Token);
			}
			ProgressText = "ENGINE: загрузка модели в llama-bench (может занять минуты)…";
			ProgressPercent = 5;
			await using var sampler = _runner.StartGpuSampling(TimeSpan.FromMilliseconds(500));
			int safeMax = BenchmarkContextPointGenerator.ComputeSafeMaxContext(profile.ContextSize, 128);
			// r108: 4 points (12.5/25/50/75% of the safe max) instead of 6 — the
			// 6.25% micro-point is flat/useless and the 90% point alone burns an
			// hour+ of GPU on a 27B model. The user's 6-point ENGINE run measured
			// 8464 s (~2.4 h) and returned ZERO numbers (parser bug, fixed here);
			// ~40 min with a real curve is the target, not hours.
			int[] points = BenchmarkContextPointGenerator.GenerateAutoPoints(safeMax, 4, 1);
			int done = 0;
			foreach (int target in points)
			{
				ProgressText = $"ENGINE {done + 1} из {points.Length} · контекст {BenchmarkContextPointGenerator.FormatPoint(target)} · PP+TG";
				ProgressPercent = 5 + 90.0 * done / points.Length;
				PointRunning = true;
				var req = new BenchmarkRunner.EngineBenchRequest
				{
					BenchPath = benchPath,
					ModelPath = profile.ModelPath ?? string.Empty,
					Caps = caps,
					PromptTokens = target,
					GenTokens = 128,
					// r108: NO depth pre-fill. With -d <target> llama-bench prefills
					// the whole context TWICE per test (once to fill the KV, once
					// for the pp prompt) — the 6-point run measured 8464 s because
					// of that. Clean PP = prefill from an empty KV (official
					// llama-bench semantics); TG-at-depth is the Depth test's job.
					Depth = null,
					Repetitions = Preset switch { 0 => 1, 1 => 3, 2 => 5, _ => 3 },
					Warmup = WarmupEnabled,
					GpuLayers = int.TryParse(profile.GetEffectiveGpuLayers(), out int ngl) && ngl > 0 ? ngl : (int?)null,
					FlashAttn = profile.FlashAttention,
					SplitMode = profile.GpuSplitMode.ToString(),
					MainGpu = profile.MainGpu,
					TensorSplit = string.Join(",", profile.TensorSplit),
					Threads = profile.Threads,
					BatchSize = profile.BatchSize,
					UbatchSize = profile.UbatchSize,
					CacheTypeK = profile.CacheTypeK.ToString(),
					CacheTypeV = profile.CacheTypeV.ToString()
				};
				var result = await _runner.RunLlamaBenchAsync(req, null, _cts.Token);
				if (result.ProcessExitCode is int code && code != 0)
				{
					// r105: the llama-bench process DIED (killed by the user, crash, OOM-killer).
					// Do NOT spawn the next point — abort the whole run with a clear status.
					_monitor.BenchmarkLog.Warning("llama-bench exited with code " + code + " at point " + target + " — aborting ENGINE test (no respawn)", "Benchmark");
					abortStatus = "llama-bench остановлен (код " + code + ") — ENGINE-тест остановлен";
					ErrorText = abortStatus;
					break;
				}
				if (result.Results.Count == 0)
				{
					// r108: log a RAW tail of the llama-bench output — the user's
					// 6-point run produced "—" everywhere and EMPTY logs, because
					// the old parser silently dropped the new JSON schema. The
					// raw tail makes any future format drift visible.
					string raw = result.RawOutput ?? string.Empty;
					string tail = raw.Length > 1500 ? "…[" + raw.Length + " байт] …" + raw[^1500..] : raw;
					_monitor.BenchmarkLog.Warning("llama-bench: no parsable results at " + target + ": " + result.Error + "\n--- raw output tail ---\n" + tail, "Benchmark");
				}
				var pp = result.Find("pp", target, null);
				var tg = result.Find("tg", 128, null);
				// r108: per-point visibility — every point's numbers in the log.
				_monitor.BenchmarkLog.Information($"ENGINE point {BenchmarkContextPointGenerator.FormatPoint(target)}: PP {(pp == null ? "—" : pp.TokensPerSecond.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " tok/s")}, TG {(tg == null ? "—" : tg.TokensPerSecond.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " tok/s")} (llama-bench: {string.Join("; ", result.Results.Select(r => r.Test + "=" + r.TokensPerSecond.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)))})", "Benchmark");
				var point = new BenchmarkPoint
				{
					Label = BenchmarkContextPointGenerator.FormatPoint(target),
					Tag = "ENGINE",
					ContextRequested = target,
					PromptTokensActual = target,
					GeneratedTokens = 128,
					PrefillTps = pp?.TokensPerSecond ?? 0,
					GenerationTps = tg?.TokensPerSecond ?? 0,
					Mean = pp?.Mean,
					Median = pp?.Median
				};
				AttachGpuToPoints(new[] { point }, sampler.Snapshot());
				sampler.Reset();
				run.Points.Add(point);
				CurrentPoints.Add(point);
				done++;
				PointRunning = false;
			}
		}
		catch (OperationCanceledException)
		{
			// Stop pressed (or per-point timeout) — the finally below still restores the server.
		}
		catch (Exception ex)
		{
			// r105: previously this propagated out of the method and EndRun was
			// NEVER called → IsRunning stayed true forever (the «тест в фоне,
			// приложение зависло» bug). Now the run always ends.
			_monitor.BenchmarkLog.Error("ENGINE benchmark failed: " + ex, "Benchmark");
			abortStatus ??= T("bench.error_prefix") + (ex.Message ?? ex.GetType().Name);
		}
		finally
		{
			// ALWAYS restore the server — even on error/cancel (spec §24).
			// r105: a FRESH token — the run CTS is cancelled on Stop, and the
			// cancelled token made the restore fail instantly (server stuck
			// "not started" after the test).
			if (serverRunning && !restored)
			{
				using var restoreCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
				try
				{
					ProgressText = T("bench.engine_restoring");
					await _monitor.ServerManager.StartAsync(profile, restoreCts.Token);
					var (host, port) = GetServerTarget(profile);
					await _monitor.ServerManager.HealthCheckAsync(host, port);
					restored = true;
				}
				catch (Exception ex)
				{
					_monitor.BenchmarkLog.Error("CRITICAL: failed to restore llama-server after engine benchmark: " + ex, "Benchmark");
					ErrorText = T("bench.engine_restore_warn");
				}
			}
		}
		run.DurationS = sw.Elapsed.TotalSeconds;
		bool wasCancelled = _cts != null && _cts.IsCancellationRequested;
		string restoreNote = serverRunning ? (restored ? T("bench.engine_restored_note") : T("bench.engine_not_restored_note")) : string.Empty;
		string? endStatus = (serverRunning && !restored) ? T("bench.engine_restore_err") : abortStatus;
		await EndRun(run, cancelled: wasCancelled, extraStatus: endStatus);
		if (restoreNote.Length > 0)
		{
			StatusText = StatusText + restoreNote;
			if (!restored)
			{
				StatusKind = BenchmarkStatusKind.Error;
			}
		}
	}

	/// <summary>Waits until GPU memory usage drops (server released the model), max ~60 s.</summary>
	private async Task WaitForGpuFreeAsync(CancellationToken ct)
	{
		for (int i = 0; i < 30; i++)
		{
			try
			{
				var gpus = await _monitor.GpuMonitorService.GetGpuInfosAsync();
				if (gpus.All(g => g.MemoryUsedGb < 2.0))
				{
					return;
				}
			}
			catch
			{
				return;
			}
			await Task.Delay(2000, ct);
		}
	}

	// ==================== FULL ====================

	private async Task RunFullAsync()
	{
		if (IsRunning)
		{
			return;
		}
		var profile = await _monitor.GetActiveProfileAsync();
		if (profile == null)
		{
			ErrorText = T("bench.err_no_profile");
			return;
		}
		bool confirmed = await _monitor.DialogService.ShowConfirmationAsync(
			"FULL-тест выполнит последовательность из 6 блоков:\n" +
			"1) Quick · 2) Prefill sweep · 3) Context Depth · 4-5) ENGINE PP/TG · 6) контрольный Quick.\n" +
			"ENGINE-блок остановит llama-server и вернёт его после.\n\nПродолжить?", "Benchmark: полный тест");
		if (!confirmed)
		{
			return;
		}
		await RunQuickAsync();
		if (ErrorText == string.Empty)
		{
			await RunPrefillAsync();
		}
		if (ErrorText == string.Empty)
		{
			await RunDepthAsync();
		}
		if (ErrorText == string.Empty)
		{
			await RunEngineAsync();
		}
		if (ErrorText == string.Empty)
		{
			await RunQuickAsync(); // 6) контрольный замер
		}
	}

	// ==================== STOP ====================

	private void Stop()
	{
		_cts?.Cancel();
		ProgressText = T("bench.stop_killing");
	}

	/// <summary>
	/// r107: called by the Monitoring speed card's quick test (the green
	/// «Быстрый тест» button at the top of the page). Fills the Benchmark
	/// Center charts/results with the quick point and saves the run to
	/// history — so the history dropdown is never empty and the charts are
	/// populated when the user runs the quick test from the card.
	/// Thread-safe: hops to the UI thread; ignored while a benchmark runs.
	/// </summary>
	public void RecordQuickTest(double generationTps, double promptTps, int promptTokens, int generatedTokens, double ttftMs, double wallTimeS)
	{
		if (generationTps <= 0 && promptTps <= 0)
		{
			return;
		}
		try
		{
			Avalonia.Threading.Dispatcher.UIThread.Post(() =>
			{
				if (IsRunning)
				{
					return; // a benchmark is in progress — don't pollute its points
				}
				var point = new BenchmarkPoint
				{
					Label = "SERVER",
					Tag = "SERVER",
					ContextRequested = promptTokens,
					PromptTokensActual = promptTokens,
					GeneratedTokens = generatedTokens,
					PrefillTps = promptTps,
					GenerationTps = generationTps,
					E2eTps = generatedTokens > 0 && wallTimeS > 0 ? generatedTokens / wallTimeS : 0,
					TtftMs = ttftMs,
					WallTimeS = wallTimeS,
					MsPerToken = generationTps > 0 ? 1000.0 / generationTps : 0
				};
				CurrentPoints.Add(point);
				var run = new BenchmarkRun
				{
					DisplayName = T("bench.quick_run_title"),
					Type = BenchmarkType.QuickServer,
					CreatedAt = DateTime.Now,
					DurationS = wallTimeS
				};
				run.Points.Add(point);
				try
				{
					_runner.SaveRun(run);
					RefreshHistory();
				}
				catch (Exception ex)
				{
					_monitor.BenchmarkLog.Error("Failed to save quick-test run: " + ex.Message, "Benchmark");
				}
			});
		}
		catch
		{
			// no dispatcher available — best effort, ignore
		}
	}

	// ==================== history ====================

	public void RefreshHistory()
	{
		HistoryRuns.Clear();
		foreach (var run in _runner.LoadRuns())
		{
			HistoryRuns.Add(run);
		}
	}

	private void SelectRun(BenchmarkRun run)
	{
		SelectedRun = run;
		CurrentPoints.Clear();
		foreach (var p in run.Points)
		{
			CurrentPoints.Add(p);
		}
		BuildCompareRows();
	}

	public void RenameRun(BenchmarkRun run, string newName)
	{
		if (string.IsNullOrWhiteSpace(newName))
		{
			return;
		}
		_runner.RenameRun(run, newName.Trim());
		RefreshHistory();
	}

	public async Task DeleteRunAsync(BenchmarkRun run)
	{
		bool ok = await _monitor.DialogService.ShowConfirmationAsync(string.Format(T("bench.delete_confirm"), run.DisplayName), T("bench.dialog_delete"));
		if (!ok)
		{
			return;
		}
		_runner.DeleteRun(run);
		RefreshHistory();
	}

	public async Task ExportRunAsync(BenchmarkRun run)
	{
		string? path = await _monitor.DialogService.SaveFileAsync(T("bench.dialog_export"),
			Path.Combine(BenchmarkRunner.BenchmarkDirectory(), run.Id + ".json"), "JSON|*.json");
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		try
		{
			_runner.ExportRun(run, path);
			await _monitor.DialogService.ShowSuccessAsync(string.Format(T("bench.exported"), path));
		}
		catch (Exception ex)
		{
			await _monitor.DialogService.ShowErrorAsync(string.Format(T("bench.export_error"), ex.Message));
		}
	}

	public async Task ImportRunAsync()
	{
		string? path = await _monitor.DialogService.SelectFileAsync(T("bench.dialog_import"), null, "JSON|*.json");
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		try
		{
			var run = _runner.ImportRun(path);
			RefreshHistory();
			await _monitor.DialogService.ShowSuccessAsync(string.Format(T("bench.imported_mark"), run.DisplayName));
		}
		catch (Exception ex)
		{
			await _monitor.DialogService.ShowErrorAsync(string.Format(T("bench.import_error"), ex.Message));
		}
	}

	// ==================== r116: web API (dialog-free variants) ====================

	/// <summary>r116: delete a run without the confirmation dialog (the web UI
	/// asks in the browser).</summary>
	public void DeleteRunDirect(BenchmarkRun run)
	{
		_runner.DeleteRun(run);
		RefreshHistory();
	}

	/// <summary>r116: import a run from a path without the file dialog (web).
	/// Returns the imported run's display name.</summary>
	public string ImportRunFromPath(string path)
	{
		var run = _runner.ImportRun(path);
		RefreshHistory();
		return run.DisplayName;
	}

	/// <summary>r116: export a run to a temp file (web sends it as a download).
	/// Returns the file path.</summary>
	public string ExportRunToTempFile(BenchmarkRun run)
	{
		string path = Path.Combine(Path.GetTempPath(), "llamastudio-bench-" + run.Id + ".json");
		_runner.ExportRun(run, path);
		return path;
	}

	private void ToggleCompare(BenchmarkRun run)
	{
		if (CompareSelection.Contains(run.Id))
		{
			CompareSelection.Remove(run.Id);
		}
		else
		{
			if (CompareSelection.Count >= 4)
			{
				CompareSelection.RemoveAt(0);
			}
			CompareSelection.Add(run.Id);
		}
		BuildCompareRows();
	}

	private void ClearCompare()
	{
		CompareSelection.Clear();
		CompareRows.Clear();
	}

	/// <summary>
	/// r112: the compare selection is driven by the A–D slot dropdowns.
	/// <paramref name="orderedIds"/> — slot order (slot A = baseline). No-op
	/// when the ordered list is unchanged.
	/// </summary>
	public void SetCompareRuns(IList<string> orderedIds)
	{
		var clean = orderedIds
			.Where(id => !string.IsNullOrEmpty(id) && HistoryRuns.Any(r => r.Id == id))
			.Distinct()
			.Take(4)
			.ToList();
		if (clean.Count == CompareSelection.Count
			&& clean.Select((id, i) => id == CompareSelection.ElementAtOrDefault(i)).All(x => x))
		{
			return;
		}
		CompareSelection.Clear();
		foreach (var id in clean)
		{
			CompareSelection.Add(id);
		}
		BuildCompareRows();
	}

	/// <summary>
	/// Compare table: per common context label across the selected runs (2–4) —
	/// prefill/gen per run with Δ% and ×N vs the FIRST selected run.
	/// Factual numbers only — no "winner" labels.
	/// </summary>
	private void BuildCompareRows()
	{
		CompareRows.Clear();
		var selected = CompareSelection
			.Select(id => HistoryRuns.FirstOrDefault(r => r.Id == id))
			.Where(r => r != null)
			.Cast<BenchmarkRun>()
			.ToList();
		if (selected.Count < 2)
		{
			return;
		}
		var labels = selected
			.SelectMany(r => r.Points.Select(p => p.Label))
			.Where(l => !string.IsNullOrEmpty(l))
			.Distinct()
			.OrderBy(l => l, StringComparer.Ordinal)
			.ToList();
		foreach (string label in labels)
		{
			var values = selected.Select(r => r.Points.FirstOrDefault(p => p.Label == label)).ToList();
			var first = values[0];
			if (first == null)
			{
				continue;
			}
			var row = new CompareRowViewModel { Label = label };
			foreach (var p in values)
			{
				row.Cells.Add(new CompareCellViewModel
				{
					Prefill = p?.PrefillTps ?? 0,
					Gen = p?.GenerationTps ?? 0,
					PrefillDiff = BenchmarkMath.FormatPercentDiff(first.PrefillTps, p?.PrefillTps ?? 0),
					GenDiff = BenchmarkMath.FormatPercentDiff(first.GenerationTps, p?.GenerationTps ?? 0),
					PrefillRatio = BenchmarkMath.FormatRatio(first.PrefillTps, p?.PrefillTps ?? 0),
					GenRatio = BenchmarkMath.FormatRatio(first.GenerationTps, p?.GenerationTps ?? 0)
				});
			}
			CompareRows.Add(row);
		}
	}

	// ==================== utils ====================

	private static void AttachGpuToPoints(BenchmarkPoint[] points, List<BenchmarkGpuSnapshot> gpus)
	{
		if (gpus.Count == 0)
		{
			return;
		}
		double powerAvg = gpus.Where(g => g.PowerAvgW > 0).DefaultIfEmpty(new BenchmarkGpuSnapshot()).Average(g => g.PowerAvgW);
		double powerPeak = gpus.Where(g => g.PowerPeakW > 0).DefaultIfEmpty(new BenchmarkGpuSnapshot()).Max(g => g.PowerPeakW);
		double peakVram = gpus.Max(g => g.VramUsedPeakGb);
		foreach (var p in points)
		{
			p.PowerAvgW = powerAvg;
			p.PowerPeakW = powerPeak;
			p.PeakVramGb = peakVram;
		}
	}

	private static string FormatMtp(double? percent, int? accepted, int? generated, double? meanLen)
	{
		if (percent == null)
		{
			return "—";
		}
		return string.Format(CultureInfo.InvariantCulture, "{0:0.0}% ({1}/{2}), mean len {3:0.##}", percent, accepted, generated, meanLen);
	}

	private static bool IsConnectionLost(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return false;
		}
		foreach (string marker in new[] { "Connect", "refused", "aborted", "reset", "unavailable", "broken pipe", "Connection" })
		{
			if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private string FriendlyError(Exception ex)
	{
		string msg = ex.Message ?? ex.GetType().Name;
		if (msg.Contains("Connect", StringComparison.OrdinalIgnoreCase) || msg.Contains("refused", StringComparison.OrdinalIgnoreCase))
		{
			return T("bench.err_unavailable");
		}
		if (msg.Contains("404", StringComparison.Ordinal))
		{
			return T("bench.err_404");
		}
		if (msg.Contains("400", StringComparison.Ordinal) || msg.Contains("context", StringComparison.OrdinalIgnoreCase))
		{
			return T("bench.err_ctx_too_big");
		}
		if (msg.Contains("Таймаут", StringComparison.OrdinalIgnoreCase) || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase))
		{
			return T("bench.err_timeout");
		}
		return msg;
	}
}

public class CompareRowViewModel
{
	public string Label { get; set; } = string.Empty;
	public ObservableCollection<CompareCellViewModel> Cells { get; set; } = new();
}

public class CompareCellViewModel
{
	public double Prefill { get; set; }
	public double Gen { get; set; }
	public string PrefillDiff { get; set; } = "—";
	public string GenDiff { get; set; } = "—";
	public string PrefillRatio { get; set; } = "—";
	public string GenRatio { get; set; } = "—";
}
