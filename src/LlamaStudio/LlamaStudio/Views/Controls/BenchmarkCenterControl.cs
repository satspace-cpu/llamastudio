using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaStudio.Views.Controls;

/// <summary>
/// r101–r103: Benchmark Center on the Monitoring page (code-built, dark LS theme).
/// r103: the VM is a DI singleton (a running test survives page navigation,
/// Stop always works); tests are SELECTED and launched with a green Start
/// button (red Stop, always enabled); the panel and charts are always open;
/// charts are interactive (hover guide with values); fonts enlarged;
/// server tests refuse to start without a running llama-server.
/// </summary>
public sealed class BenchmarkCenterControl : UserControl
{
	// Soft palette from the existing dark theme (values from App.cs).
	private static IBrush Res(string key) => key switch
	{
		"ThemeTextPrimary" => TextPrimary,
		"ThemeTextSecondary" => TextSecondary,
		"ThemeTextTertiary" => TextTertiary,
		_ => TextSecondary
	};
	private static readonly IBrush TextPrimary = new ImmutableSolidColorBrush(Color.Parse("#F8FAFC"));
	private static readonly IBrush TextSecondary = new ImmutableSolidColorBrush(Color.Parse("#94A3B8"));
	private static readonly IBrush TextTertiary = new ImmutableSolidColorBrush(Color.Parse("#64748B"));
	private static readonly IBrush GenLine = new ImmutableSolidColorBrush(Color.Parse("#10B981"));    // soft green (ProgressCore)
	private static readonly IBrush PrefillLine = new ImmutableSolidColorBrush(Color.Parse("#3B82F6")); // soft blue (ProgressVram)
	private static readonly IBrush GridLine = new ImmutableSolidColorBrush(Color.Parse("#334155"));    // ThemeBorder
	private static readonly IBrush SoftRed = new ImmutableSolidColorBrush(Color.Parse("#EF4444"));
	private static readonly IBrush OkGreen = new ImmutableSolidColorBrush(Color.Parse("#34D399"));
	private static readonly IBrush WarnAmber = new ImmutableSolidColorBrush(Color.Parse("#FDBA74"));
	private static readonly IBrush ErrRed = new ImmutableSolidColorBrush(Color.Parse("#F87171"));
	private static readonly IBrush GenFill = new ImmutableSolidColorBrush(Color.Parse("#10B981"), 0.14);
	private static readonly IBrush PrefillFill = new ImmutableSolidColorBrush(Color.Parse("#3B82F6"), 0.14);

	// r103.1: expressive button states (Старт/Стоп). r112: the test-selection
	// chip palette is gone (segmented buttons replaced by a dropdown).
	private static readonly IBrush TestSelFg = new ImmutableSolidColorBrush(Color.Parse("#052E22"));
	private static readonly IBrush TestDisabledBg = new ImmutableSolidColorBrush(Color.Parse("#151F2C"));
	private static readonly IBrush TestDisabledFg = new ImmutableSolidColorBrush(Color.Parse("#4B5B70"));
	private static readonly IBrush TestDisabledBorder = new ImmutableSolidColorBrush(Color.Parse("#223045"));
	private static readonly IBrush White = new ImmutableSolidColorBrush(Color.Parse("#FFFFFF"));

	// r114: full localization. The control resolves the app-wide localization
	// service (it builds its UI before the VM is attached) and rebuilds the
	// whole UI when the app language changes.
	private ILocalizationService? _loc;
	private string T(string key) => _loc?.T(key) ?? key;

	/// <summary>r114: BarChart is a separate class (no VM/loc) — it reads this
	/// for the "no data" caption; refreshed in the ctor and on language change.</summary>
	public static string ChartEmptyText { get; set; } = "нет данных — запустите тест";

	// r115: NOT readonly — a language change replaces it with a fresh panel
	// (the old one stays parented inside the old _card until Content swaps).
	private StackPanel _root = new StackPanel { Spacing = 12 };
	private Border _card = null!;
	private ProgressBar _progressBar = null!;
	private TextBlock _progressText = null!;
	private TextBlock _statusText = null!;
	private TextBlock _envText = null!;
	private TextBlock _capsText = null!;
	private TextBlock _mtpText = null!;
	private TextBlock _quickExtra = null!;
	private TextBlock _errorText = null!;
	private Button _startBtn = null!;
	private Button _stopBtn = null!;
	private Border _runBanner = null!;
	private ScrollViewer _resultsScroll = null!;
	private StackPanel _resultsHost = null!;
	// r103.1: history moved from the bottom list to a dropdown above the charts.
	private ComboBox _historyCombo = null!;
	private readonly List<BenchmarkRun> _historyComboRuns = new();
	private bool _historyComboUpdating;
	private bool _userPickedHistory;
	private Button _histExportBtn = null!;
	private Button _histDeleteBtn = null!;
	private ComboBox _presetCombo = null!;
	private ScrollViewer _compareScroll = null!;
	private StackPanel _compareHost = null!;
	private BarChart _prefillChart = null!;
	private BarChart _genChart = null!;
	private BenchmarkCenterViewModel? _bm;
	private MonitoringViewModel? _monitor;

	// r112: test selection = dropdown (was segmented buttons); the «Как это
	// работает» card is DYNAMIC — a per-test description below the selector.
	private ComboBox _testCombo = null!;
	private bool _testComboUpdating;
	private TextBlock _instrTitle = null!;
	private StackPanel _instrBody = null!;

	// r112: multi-history comparison — slots A–D, each a run dropdown. The
	// charts overlay all selected runs in slot colors (legend above them);
	// the compare table shows PP/TG per run with Δ%/×N vs slot A.
	private readonly ComboBox[] _slotCombos = new ComboBox[4];
	private readonly List<BenchmarkRun>[] _slotRuns =
	{
			new List<BenchmarkRun>(), new List<BenchmarkRun>(), new List<BenchmarkRun>(), new List<BenchmarkRun>()
	};
	private bool _slotUpdating;
	private StackPanel _legend = null!;
	private static readonly string[] SlotLetters = { "A", "B", "C", "D" };
	private static readonly IBrush[] SlotColors =
	{
			new ImmutableSolidColorBrush(Color.Parse("#F59E0B")), // A — amber
			new ImmutableSolidColorBrush(Color.Parse("#38BDF8")), // B — sky
			new ImmutableSolidColorBrush(Color.Parse("#F472B6")), // C — pink
			new ImmutableSolidColorBrush(Color.Parse("#34D399"))  // D — green
	};
	private static readonly IBrush[] SlotSoftColors =
	{
			new ImmutableSolidColorBrush(Color.Parse("#F59E0B"), 0.18),
			new ImmutableSolidColorBrush(Color.Parse("#38BDF8"), 0.18),
			new ImmutableSolidColorBrush(Color.Parse("#F472B6"), 0.18),
			new ImmutableSolidColorBrush(Color.Parse("#34D399"), 0.18)
	};

	/// <summary>r103: DI singleton view model (survives page navigation).</summary>
	public BenchmarkCenterViewModel? BenchmarkVm { get; set; }

	public BenchmarkCenterControl()
	{
		// r114: app-wide localization (available once the app's services are up).
		_loc = App.Services?.GetService<ILocalizationService>();
		if (_loc != null)
		{
			ChartEmptyText = _loc.T("bench.chart_empty");
			_loc.OnLanguageChanged += OnLanguageChanged;
		}
		BuildUi();
		PropertyChanged += (_, args) =>
		{
			if (args.Property == DataContextProperty)
			{
				SetViewModel(DataContext as MonitoringViewModel);
			}
		};
	}

	/// <summary>r114: language switch → rebuild the whole UI (all static labels,
	/// tooltips, the per-test description, charts/table from the VM).</summary>
	private void OnLanguageChanged(object? sender, string language)
	{
		ChartEmptyText = T("bench.chart_empty");
		// r115: FRESH _root. The old panel is still a child of the old _card
		// (Control.Content) — reusing it made BuildUi's `Child = _root` throw
		// "The Control already has a parent" (reported on RU→EN→RU switch).
		_root = new StackPanel { Spacing = 12 };
		BuildUi();
		if (_bm != null)
		{
			// re-apply the VM state to the fresh controls (BuildUi already
			// reads it where possible; re-sync the dynamic parts).
			UpdateTestDescription();
			RebuildHistoryCombo();
			RebuildResults();
			UpdateCharts();
			RebuildCompare();
		}
	}

	private void BuildUi()
	{
		// r114: BuildUi also runs on language change (fresh controls) — suppress
		// selection handlers while wiring so they don't write back to the VM.
		_testComboUpdating = true;
		_historyComboUpdating = true;
		_slotUpdating = true;
		_card = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20),
			Child = _root
		};
		_card.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ThemeCard"));
		Content = _card;

		// ===== r105: "TEST IS RUNNING" banner — a running test must be visible
		// no matter which tab you switched to (and back). Bound to the DI-singleton
		// VM, so it lights up on every recreated page while the test goes on. =====
		_runBanner = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 10, 14, 10),
			Background = new ImmutableSolidColorBrush(Color.Parse("#10B981"), 0.14),
			BorderBrush = GenLine,
			BorderThickness = new Thickness(1),
			IsVisible = false,
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10,
				VerticalAlignment = VerticalAlignment.Center,
				Children =
				{
					new TextBlock { Text = "●", Foreground = GenLine, FontSize = 15, VerticalAlignment = VerticalAlignment.Center },
					new TextBlock { Text = T("bench.running_banner"), Foreground = Res("ThemeTextPrimary"), FontSize = 15, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
					new TextBlock { Foreground = Res("ThemeTextSecondary"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap }
				}
			}
		};
		((TextBlock)(((StackPanel)_runBanner.Child!).Children)[2])
			.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.ProgressText)));
		_runBanner.Bind(Border.IsVisibleProperty, new Binding(nameof(BenchmarkCenterViewModel.IsRunning)));
		ToolTip.SetTip(_runBanner, T("bench.running_banner_tip"));
		_root.Children.Add(_runBanner);

		// ===== title (r112: renamed to match the left-menu item) =====
		_root.Children.Add(new TextBlock { Text = T("bench.title"), Foreground = Res("ThemeTextPrimary"), FontSize = 16, FontWeight = FontWeight.SemiBold });

		// ===== test selection (r112: DROPPDOWN, was segmented buttons) + start/stop =====
		var row0 = new DockPanel { LastChildFill = false };
		var selPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		selPanel.Children.Add(new TextBlock { Text = T("bench.test_label"), Foreground = Res("ThemeTextSecondary"), FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
		// r107: Quick is NOT here — it lives on the speed card at the top of the
		// page (green «Быстрый тест» button); it still fills these charts/history.
		_testCombo = new ComboBox { Width = 240, VerticalAlignment = VerticalAlignment.Center };
		ToolTip.SetTip(_testCombo, T("bench.test_combo_tip"));
		string[] testNames = { T("bench.test_name_0"), T("bench.test_name_1"), T("bench.test_name_2"), T("bench.test_name_3") };
		string[] testTips =
		{
			T("bench.test_tip_0"),
			T("bench.test_tip_1"),
			T("bench.test_tip_2"),
			T("bench.test_tip_3")
		};
		for (int i = 0; i < 4; i++)
		{
			int captured = i;
			var item = new ComboBoxItem { Content = testNames[captured] };
			ToolTip.SetTip(item, testTips[captured]);
			_testCombo.Items.Add(item);
		}
		// r114: restore the selected test on language-rebuild (guarded by the
		// _testComboUpdating flag set at the top of BuildUi).
		if (_bm != null)
		{
			_testCombo.SelectedIndex = Math.Clamp(_bm.SelectedTest, 0, 3);
		}
		_testCombo.SelectionChanged += (_, _) =>
		{
			if (_testComboUpdating || _bm == null || _bm.IsRunning)
			{
				return;
			}
			int i = _testCombo.SelectedIndex;
			if (i >= 0 && i < 4)
			{
				_bm.SelectTestCommand.Execute(i);
			}
		};
		selPanel.Children.Add(_testCombo);
		DockPanel.SetDock(selPanel, Dock.Left);
		row0.Children.Add(selPanel);
		// r103.1: Старт/Стоп — группа с зазором. Цвета по состоянию:
		// идёт тест → Старт серый, Стоп активный красный; нет теста → наоборот.
		_startBtn = MakeButton(T("bench.start_btn"), primary: true);
		ToolTip.SetTip(_startBtn, T("bench.start_tip"));
		_stopBtn = MakeButton(T("bench.stop_btn"), stopStyle: true);
		ToolTip.SetTip(_stopBtn, T("bench.stop_tip"));
		var runRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		runRow.Children.Add(_startBtn);
		runRow.Children.Add(_stopBtn);
		DockPanel.SetDock(runRow, Dock.Right);
		row0.Children.Add(runRow);
		_root.Children.Add(row0);

		// ===== r112: DYNAMIC «Как это работает» — the description of the
		// SELECTED test, directly below the test dropdown (was a static block
		// above everything). Rebuilt in UpdateTestDescription(). =====
		var instr = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 12, 14, 12) };
		instr.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ThemeSurface"));
		var instrPanel = new StackPanel { Spacing = 5 };
		_instrTitle = new TextBlock { Text = T("bench.desc_title"), Foreground = Res("ThemeTextPrimary"), FontSize = 14, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
		instrPanel.Children.Add(_instrTitle);
		_instrBody = new StackPanel { Spacing = 3 };
		instrPanel.Children.Add(_instrBody);
		instr.Child = instrPanel;
		ToolTip.SetTip(instr, T("bench.instr_card_tip"));
		_root.Children.Add(instr);

		// ===== options row =====
		var optRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(0, 2, 0, 0) };
		optRow.Children.Add(new TextBlock { Text = T("bench.preset_label"), Foreground = Res("ThemeTextSecondary"), VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });
		_presetCombo = new ComboBox { Width = 120, VerticalAlignment = VerticalAlignment.Center };
		_presetCombo.Items.Add(new ComboBoxItem { Content = "Quick" });
		_presetCombo.Items.Add(new ComboBoxItem { Content = "Standard" });
		_presetCombo.Items.Add(new ComboBoxItem { Content = "Thorough" });
		// r108: synced from the VM — a hardcoded index here desynced UI from VM.
		// r114: also read from the VM on language-rebuild (BuildUi re-runs).
		_presetCombo.SelectedIndex = _bm != null ? Math.Clamp(_bm.Preset, 0, 2) : 0;
		ToolTip.SetTip(_presetCombo, T("bench.preset_tip"));
		_presetCombo.SelectionChanged += (_, _) => { if (_bm != null) _bm.Preset = _presetCombo.SelectedIndex; };
		optRow.Children.Add(_presetCombo);
		// r114: initial state from the VM (BuildUi re-runs on language change).
		var warmup = new CheckBox { Content = T("bench.warmup_label"), IsChecked = _bm?.WarmupEnabled ?? true, VerticalAlignment = VerticalAlignment.Center };
		ToolTip.SetTip(warmup, T("bench.warmup_tip"));
		warmup.Checked += (_, _) => { if (_bm != null) _bm.WarmupEnabled = true; };
		warmup.Unchecked += (_, _) => { if (_bm != null) _bm.WarmupEnabled = false; };
		optRow.Children.Add(warmup);
		var manual = new CheckBox { Content = T("bench.manual_label"), IsChecked = _bm?.ManualPointsMode ?? false, VerticalAlignment = VerticalAlignment.Center };
		ToolTip.SetTip(manual, T("bench.manual_tip"));
		manual.Checked += (_, _) => { if (_bm != null) _bm.ManualPointsMode = true; };
		manual.Unchecked += (_, _) => { if (_bm != null) _bm.ManualPointsMode = false; };
		optRow.Children.Add(manual);
		var manualBox = new TextBox { Width = 240, VerticalAlignment = VerticalAlignment.Center };
		// r114: restore the user's custom-points text on language-rebuild.
		if (_bm != null)
		{
			manualBox.Text = _bm.ManualPointsText;
		}
		manualBox.Watermark = "16K, 32K, 64K";
		ToolTip.SetTip(manualBox, T("bench.manual_tip2"));
		manualBox.PropertyChanged += (_, e) =>
		{
			if (e.Property == TextBox.TextProperty && _bm != null)
			{
				_bm.ManualPointsText = manualBox.Text;
			}
		};
		optRow.Children.Add(manualBox);
		var autoHint = new TextBlock { Text = T("bench.auto_hint"), Foreground = Res("ThemeTextTertiary"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		ToolTip.SetTip(autoHint, T("bench.auto_hint_tip"));
		optRow.Children.Add(autoHint);
		_root.Children.Add(optRow);

		// ===== progress =====
		_progressBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 10, CornerRadius = new CornerRadius(5) };
		ToolTip.SetTip(_progressBar, T("bench.progress_tip"));
		_root.Children.Add(_progressBar);
		_progressText = new TextBlock { Text = "", Foreground = Res("ThemeTextSecondary"), FontSize = 14, IsVisible = false, TextWrapping = TextWrapping.Wrap };
		_root.Children.Add(_progressText);

		_envText = new TextBlock { Text = "", Foreground = Res("ThemeTextSecondary"), FontSize = 13, TextWrapping = TextWrapping.Wrap };
		ToolTip.SetTip(_envText, T("bench.env_tip"));
		_root.Children.Add(_envText);
		_capsText = new TextBlock { Text = "", Foreground = Res("ThemeTextSecondary"), FontSize = 13, IsVisible = false, TextWrapping = TextWrapping.Wrap };
		ToolTip.SetTip(_capsText, T("bench.caps_tip"));
		_root.Children.Add(_capsText);
		_mtpText = new TextBlock { Text = "", Foreground = Res("ThemeTextSecondary"), FontSize = 13, IsVisible = false };
		ToolTip.SetTip(_mtpText, T("bench.mtp_tip"));
		_root.Children.Add(_mtpText);
		_quickExtra = new TextBlock { Text = "", Foreground = Res("ThemeTextSecondary"), FontSize = 13, IsVisible = false };
		ToolTip.SetTip(_quickExtra, T("bench.quick_extra_tip"));
		_root.Children.Add(_quickExtra);
		_errorText = new TextBlock { Text = "", Foreground = ErrRed, FontSize = 14, TextWrapping = TextWrapping.Wrap, IsVisible = false };
		_root.Children.Add(_errorText);
		_statusText = new TextBlock { Text = "", Foreground = OkGreen, FontSize = 14, IsVisible = false, TextWrapping = TextWrapping.Wrap };
		_root.Children.Add(_statusText);

		// ===== r103.1: history — dropdown ABOVE the charts (was a bottom list) =====
		var histRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };
		histRow.Children.Add(new TextBlock { Text = T("bench.history_label"), Foreground = Res("ThemeTextSecondary"), FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
		_historyCombo = new ComboBox { Width = 560, VerticalAlignment = VerticalAlignment.Center };
		ToolTip.SetTip(_historyCombo, T("bench.history_tip"));
		_historyCombo.SelectionChanged += (_, _) =>
		{
			if (_historyComboUpdating || _bm == null)
			{
				return;
			}
			int i = _historyCombo.SelectedIndex;
			if (i >= 0 && i < _historyComboRuns.Count)
			{
				// r107: the user explicitly picked a run — respect that choice
				// (otherwise the combo would snap back to the latest one).
				_userPickedHistory = true;
				_bm.SelectRunCommand.Execute(_historyComboRuns[i]);
			}
		};
		histRow.Children.Add(_historyCombo);
		_histExportBtn = MakeButton(T("bench.export_btn"));
		_histExportBtn.Padding = new Thickness(12, 5, 12, 5);
		ToolTip.SetTip(_histExportBtn, T("bench.export_tip"));
		_histExportBtn.Click += (_, _) => { var r = SelectedHistoryRun(); if (r != null) _ = _bm!.ExportRunAsync(r); };
		histRow.Children.Add(_histExportBtn);
		_histDeleteBtn = MakeButton(T("bench.delete_btn"));
		_histDeleteBtn.Padding = new Thickness(12, 5, 12, 5);
		ToolTip.SetTip(_histDeleteBtn, T("bench.delete_tip"));
		_histDeleteBtn.Click += (_, _) => { var r = SelectedHistoryRun(); if (r != null) _ = _bm!.DeleteRunAsync(r); };
		histRow.Children.Add(_histDeleteBtn);
		var importBtn = MakeButton(T("bench.import_btn"));
		importBtn.Padding = new Thickness(12, 5, 12, 5);
		ToolTip.SetTip(importBtn, T("bench.import_tip"));
		importBtn.Click += async (_, _) => { if (_bm != null) await _bm.ImportRunAsync(); };
		histRow.Children.Add(importBtn);
		_root.Children.Add(histRow);

		// ===== r112: comparison legend (visible when 2+ slot histories are picked) =====
		_legend = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 14,
			IsVisible = false,
			Margin = new Thickness(0, 0, 0, 2)
		};
		ToolTip.SetTip(_legend, T("bench.legend_tip"));
		_root.Children.Add(_legend);

		// ===== charts: full width, always visible, interactive =====
		var chartsGrid = new Grid { Height = 300, ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 2, 0, 2) };
		var prefillChartCard = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(12), Margin = new Thickness(0, 0, 8, 0) };
		prefillChartCard.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ThemeSurface"));
		var prefillChartPanel = new DockPanel { LastChildFill = true };
		var prefillTitles = new StackPanel { Spacing = 3 };
		prefillTitles.Children.Add(new TextBlock { Text = T("bench.prefill_chart_title"), Foreground = Res("ThemeTextPrimary"), FontSize = 14, FontWeight = FontWeight.SemiBold });
		prefillTitles.Children.Add(new TextBlock { Text = T("bench.prefill_chart_hint"), Foreground = Res("ThemeTextTertiary"), FontSize = 12 });
		DockPanel.SetDock(prefillTitles, Dock.Top);
		prefillChartPanel.Children.Add(prefillTitles);
		ToolTip.SetTip(prefillChartCard, T("bench.prefill_card_tip"));
		_prefillChart = new BarChart(PrefillLine, PrefillFill);
		prefillChartPanel.Children.Add(_prefillChart);
		prefillChartCard.Child = prefillChartPanel;
		Grid.SetColumn(prefillChartCard, 0);
		chartsGrid.Children.Add(prefillChartCard);
		var genChartCard = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(12), Margin = new Thickness(8, 0, 0, 0) };
		genChartCard.Bind(Border.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ThemeSurface"));
		var genChartPanel = new DockPanel { LastChildFill = true };
		var genTitles = new StackPanel { Spacing = 3 };
		genTitles.Children.Add(new TextBlock { Text = T("bench.gen_chart_title"), Foreground = Res("ThemeTextPrimary"), FontSize = 14, FontWeight = FontWeight.SemiBold });
		genTitles.Children.Add(new TextBlock { Text = T("bench.gen_chart_hint"), Foreground = Res("ThemeTextTertiary"), FontSize = 12 });
		DockPanel.SetDock(genTitles, Dock.Top);
		genChartPanel.Children.Add(genTitles);
		ToolTip.SetTip(genChartCard, T("bench.gen_card_tip"));
		_genChart = new BarChart(GenLine, GenFill);
		genChartPanel.Children.Add(_genChart);
		genChartCard.Child = genChartPanel;
		Grid.SetColumn(genChartCard, 1);
		chartsGrid.Children.Add(genChartCard);
		_root.Children.Add(chartsGrid);

		// ===== results =====
		_root.Children.Add(SectionHeader(T("bench.results_title")));
		_resultsScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 300 };
		_resultsHost = new StackPanel { Spacing = 3 };
		_resultsScroll.Content = _resultsHost;
		ToolTip.SetTip(_resultsScroll, T("bench.results_tip"));
		_root.Children.Add(_resultsScroll);

		// ===== r112: compare — slots A–D (each a history dropdown). 2–4 slots
		// selected → charts overlay the runs in slot colors (legend above the
		// charts) + the table below shows PP/TG per run, Δ%/×N vs slot A. =====
		var cmpHeaderRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		var cmpHeader = SectionHeader(T("bench.compare_title"));
		cmpHeader.VerticalAlignment = VerticalAlignment.Center;
		cmpHeaderRow.Children.Add(cmpHeader);
		var clearCompareBtn = MakeButton(T("bench.compare_reset"));
		clearCompareBtn.Padding = new Thickness(12, 5, 12, 5);
		clearCompareBtn.VerticalAlignment = VerticalAlignment.Center;
		ToolTip.SetTip(clearCompareBtn, T("bench.compare_reset_tip"));
		clearCompareBtn.Click += (_, _) =>
		{
			_slotUpdating = true;
			foreach (var c in _slotCombos)
			{
				c.SelectedIndex = -1;
			}
			_slotUpdating = false;
			UpdateCompareFromSlots();
		};
		cmpHeaderRow.Children.Add(clearCompareBtn);
		_root.Children.Add(cmpHeaderRow);
		var slotRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 2, 0, 6) };
		for (int s = 0; s < 4; s++)
		{
			int captured = s;
			var slotItem = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
			var dot = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = SlotColors[captured], VerticalAlignment = VerticalAlignment.Center };
			slotItem.Children.Add(dot);
			slotItem.Children.Add(new TextBlock { Text = SlotLetters[captured], Foreground = SlotColors[captured], FontWeight = FontWeight.SemiBold, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
			var slotCombo = new ComboBox { Width = 250, VerticalAlignment = VerticalAlignment.Center };
			ToolTip.SetTip(slotCombo, string.Format(T("bench.slot_tip"), SlotLetters[captured]));
			slotCombo.SelectionChanged += (_, _) =>
			{
				if (_slotUpdating || _bm == null)
				{
					return;
				}
				UpdateCompareFromSlots();
			};
			_slotCombos[captured] = slotCombo;
			slotItem.Children.Add(slotCombo);
			slotRow.Children.Add(slotItem);
		}
		_root.Children.Add(slotRow);
		_compareScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 300 };
		_compareHost = new StackPanel { Spacing = 3 };
		_compareScroll.Content = _compareHost;
		_root.Children.Add(_compareScroll);

		// r114: wiring done — re-enable the selection handlers.
		_testComboUpdating = false;
		_historyComboUpdating = false;
		_slotUpdating = false;
	}

	private TextBlock SectionHeader(string text)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = Res("ThemeTextPrimary"),
			FontSize = 14,
			FontWeight = FontWeight.SemiBold,
			TextWrapping = TextWrapping.Wrap
		};
	}

	private Button MakeButton(string label, bool primary = false, bool stopStyle = false)
	{
		var b = new Button
		{
			Content = label,
			Foreground = Res("ThemeTextPrimary"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(stopStyle || primary ? 20 : 14, 9, stopStyle || primary ? 20 : 14, 9),
			FontSize = 13,
			FontWeight = stopStyle || primary ? FontWeight.SemiBold : FontWeight.Normal
		};
		if (stopStyle)
		{
			b.Background = SoftRed;
		}
		else if (primary)
		{
			b.Background = GenLine;
			b.Foreground = new ImmutableSolidColorBrush(Color.Parse("#052E22"));
		}
		else
		{
			b.Bind(Button.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ThemeSurface"));
		}
		return b;
	}

	private void SetViewModel(MonitoringViewModel? vm)
	{
		_monitor = vm;
		if (vm == null)
		{
			return;
		}
		if (_bm == null)
		{
			// r103: prefer the DI singleton (survives page navigation);
			// r105: also fall back to the process-wide Current instance so a
			// running test is NEVER hidden behind a fresh local VM.
			_bm = BenchmarkVm ?? BenchmarkCenterViewModel.Current ?? new BenchmarkCenterViewModel(vm);
			_root.DataContext = _bm;
			// r108: the preset combo reflects the VM (UI and run must match).
			_presetCombo.SelectedIndex = Math.Clamp(_bm.Preset, 0, 2);
			// r112: the test combo reflects the VM too.
			_testComboUpdating = true;
			_testCombo.SelectedIndex = Math.Clamp(_bm.SelectedTest, 0, 3);
			_testComboUpdating = false;
			BindAll();
			_bm.PropertyChanged += OnBmPropertyChanged;
			_bm.HistoryRuns.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(RebuildHistoryCombo);
			_bm.CurrentPoints.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(() => { RebuildResults(); UpdateCharts(); });
			_bm.CompareRows.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(RebuildCompare);
			_ = _bm.RefreshEnvironmentSummaryAsync();
			RebuildHistoryCombo();
			RebuildResults();
			// r113: the chart control is NEW (empty) on every page recreation,
			// while the VM keeps the last run's live points. RebuildResults fills
			// the table from them, but the charts need an explicit redraw too —
			// otherwise coming back to the page shows «нет данных» until the user
			// manually re-picks a history (which re-fires UpdateCharts).
			UpdateCharts();
			RebuildCompare();
			UpdateTestDescription();
			UpdateRunButtons();
		}
	}

	private void BindAll()
	{
		if (_bm == null)
		{
			return;
		}
		_progressBar.Bind(ProgressBar.ValueProperty, new Binding(nameof(BenchmarkCenterViewModel.ProgressPercent)));
		_progressBar.Bind(ProgressBar.IsIndeterminateProperty, new Binding(nameof(BenchmarkCenterViewModel.PointRunning)));
		_progressText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.ProgressText)));
		_progressText.Bind(TextBlock.IsVisibleProperty, new Binding(nameof(BenchmarkCenterViewModel.IsRunning)));
		_statusText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.StatusText)));
		BindVisible(_statusText, nameof(BenchmarkCenterViewModel.StatusText), new NotEmptyConverter());
		_envText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.EnvironmentSummary)));
		_capsText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.CapabilitiesText)));
		BindVisible(_capsText, nameof(BenchmarkCenterViewModel.CapabilitiesText), new NotEmptyConverter());
		_mtpText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.MtpText)));
		BindVisible(_mtpText, nameof(BenchmarkCenterViewModel.MtpText), new NotDashConverter());
		_quickExtra.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.QuickResultExtra)));
		BindVisible(_quickExtra, nameof(BenchmarkCenterViewModel.QuickResultExtra), new NotEmptyConverter());
		_errorText.Bind(TextBlock.TextProperty, new Binding(nameof(BenchmarkCenterViewModel.ErrorText)));
		BindVisible(_errorText, nameof(BenchmarkCenterViewModel.ErrorText), new NotEmptyConverter());
		_startBtn.Bind(Button.CommandProperty, new Binding(nameof(BenchmarkCenterViewModel.StartCommand)));
		_stopBtn.Bind(Button.CommandProperty, new Binding(nameof(BenchmarkCenterViewModel.StopCommand)));
		// r103.1/r106: Start + selection disabled while a test is active;
		// Stop is enabled ONLY while a test runs (idle = grey, not pressable):
		// «если нет теста, стоп не активна серая, нажать нельзя».
		BindEnabled(_startBtn, nameof(BenchmarkCenterViewModel.IsRunning), new InverseBoolConverter());
		BindEnabled(_stopBtn, nameof(BenchmarkCenterViewModel.IsRunning), null);
		// r112: the test dropdown is locked while a test runs (same rule the
		// segmented buttons had).
		var testEnabled = new Binding(nameof(BenchmarkCenterViewModel.IsRunning)) { Converter = new InverseBoolConverter() };
		_testCombo.Bind(ComboBox.IsEnabledProperty, testEnabled);
	}

	private void OnBmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (_bm == null)
		{
			return;
		}
		if (e.PropertyName == nameof(BenchmarkCenterViewModel.SelectedTest))
		{
			// r112: the VM's selection may change programmatically — mirror it
			// into the dropdown and refresh the per-test description.
			_testComboUpdating = true;
			_testCombo.SelectedIndex = Math.Clamp(_bm.SelectedTest, 0, 3);
			_testComboUpdating = false;
			UpdateTestDescription();
		}
		if (e.PropertyName == nameof(BenchmarkCenterViewModel.IsRunning))
		{
			UpdateRunButtons();
		}
		// r114: color by the language-independent StatusKind, not RU prefixes.
		if (e.PropertyName == nameof(BenchmarkCenterViewModel.StatusKind))
		{
			_statusText.Foreground = _bm.StatusKind switch
			{
				BenchmarkStatusKind.Error => ErrRed,
				BenchmarkStatusKind.Warning => WarnAmber,
				_ => OkGreen
			};
		}
	}

	/// <summary>
	/// r112: per-test «Как это работает» — the card under the test dropdown
	/// always describes the SELECTED test (what it measures, points, time,
	/// server impact).
	/// </summary>
	private string[][] TestDescriptions => new[]
	{
		new[] { T("bench.desc_00"), T("bench.desc_01"), T("bench.desc_02"), T("bench.desc_03") },
		new[] { T("bench.desc_10"), T("bench.desc_11"), T("bench.desc_12"), T("bench.desc_13") },
		new[] { T("bench.desc_20"), T("bench.desc_21"), T("bench.desc_22"), T("bench.desc_23") },
		new[] { T("bench.desc_30"), T("bench.desc_31"), T("bench.desc_32"), T("bench.desc_33") },
	};

	private void UpdateTestDescription()
	{
		int i = _bm?.SelectedTest ?? 0;
		i = Math.Clamp(i, 0, 3);
		_instrTitle.Text = T("bench.desc_title") + _bm!.SelectedTestName;
		_instrBody.Children.Clear();
		foreach (var line in TestDescriptions[i])
		{
			_instrBody.Children.Add(new TextBlock
			{
				Text = "• " + line,
				Foreground = Res("ThemeTextSecondary"),
				FontSize = 13,
				TextWrapping = TextWrapping.Wrap
			});
		}
		_instrBody.Children.Add(new TextBlock
		{
			Text = T("bench.desc_quick_foot"),
			Foreground = Res("ThemeTextTertiary"),
			FontSize = 12,
			TextWrapping = TextWrapping.Wrap
		});
	}

	/// <summary>r106 (revert of r105 «always-ready stop»): strict state logic.
	/// No test running → Старт зелёная активна, Стоп серая НЕАКТИВНА (нажать
	/// нельзя). Test running → Стоп красная активна, Старт серая НЕАКТИВНА.</summary>
	private void UpdateRunButtons()
	{
		bool running = _bm != null && _bm.IsRunning;
		if (running)
		{
			_startBtn.Background = TestDisabledBg;
			_startBtn.Foreground = TestDisabledFg;
			_stopBtn.Background = SoftRed;
			_stopBtn.Foreground = White;
		}
		else
		{
			_startBtn.Background = GenLine;
			_startBtn.Foreground = TestSelFg;
			_stopBtn.Background = TestDisabledBg;
			_stopBtn.Foreground = TestDisabledFg;
		}
	}

	private BenchmarkRun? SelectedHistoryRun()
	{
		int i = _historyCombo.SelectedIndex;
		return i >= 0 && i < _historyComboRuns.Count ? _historyComboRuns[i] : null;
	}

	private void BindVisible(AvaloniaObject target, string path, IValueConverter converter)
	{
		var b = new Binding(path);
		b.Converter = converter;
		target.Bind(TextBlock.IsVisibleProperty, b);
	}

	private static void BindEnabled(Control target, string path, IValueConverter converter)
	{
		var b = new Binding(path);
		b.Converter = converter;
		target.Bind(Button.IsEnabledProperty, b);
	}

	// ===== results table =====

	private static readonly int[] TableWidths = { 110, 80, 90, 80, 80, 66, 74, 70, 128, 80, 62 };

	private void RebuildResults()
	{
		if (_bm == null)
		{
			return;
		}
		_resultsHost.Children.Clear();
		_resultsHost.Children.Add(MakeTableHeader(new[] { T("bench.th_context"), T("bench.th_requested"), T("bench.th_actual"), "PP tok/s", "TG tok/s", "E2E", T("bench.th_ttft"), T("bench.th_mptok"), T("bench.th_power"), T("bench.th_vram"), "MTP" }));
		foreach (var p in _bm.CurrentPoints)
		{
			var cells = new[]
			{
				p.Label + (string.IsNullOrEmpty(p.Tag) ? "" : " (" + p.Tag + ")"),
				Fmt(p.ContextRequested),
				Fmt(p.PromptTokensActual),
				FmtTps(p.PrefillTps),
				FmtTps(p.GenerationTps),
				FmtTps(p.E2eTps),
				p.TtftMs > 0 ? p.TtftMs.ToString("0", CultureInfo.InvariantCulture) : "—",
				p.MsPerToken > 0 ? p.MsPerToken.ToString("0.##", CultureInfo.InvariantCulture) : "—",
				p.PowerAvgW > 0 ? p.PowerAvgW.ToString("0", CultureInfo.InvariantCulture) + "W / " + p.PowerPeakW.ToString("0") + "W" : "—",
				p.PeakVramGb > 0 ? p.PeakVramGb.ToString("0.#", CultureInfo.InvariantCulture) + "GB" : "—",
				p.MtpAcceptancePercent.HasValue ? p.MtpAcceptancePercent.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%" : "—"
			};
			_resultsHost.Children.Add(MakeTableRow(cells));
		}
		if (_bm.CurrentPoints.Count == 0)
		{
			_resultsHost.Children.Add(new TextBlock { Text = T("bench.no_points"), Foreground = Res("ThemeTextTertiary"), FontSize = 13, Margin = new Thickness(0, 6, 0, 0) });
		}
	}

	private void UpdateCharts()
	{
		if (_bm == null)
		{
			return;
		}
		var slots = ActiveSlotRuns();
		if (slots.Count >= 2)
		{
			// r112: multi-history overlay — grouped bars, one color per slot.
			FillChartGrouped(_prefillChart, slots, p => p.PrefillTps);
			FillChartGrouped(_genChart, slots, p => p.GenerationTps);
			UpdateLegend();
			return;
		}
		var points = _bm.CurrentPoints.Where(p => p.PromptTokensActual > 0).OrderBy(p => p.PromptTokensActual).ToList();
		// r106: bars instead of dots. A single quick-test point renders as ONE
		// labeled bar (value on top) — readable at a glance.
		bool single = points.Count == 1;
		string LabelFor(int i)
		{
			if (single)
			{
				return T("bench.quick_label");
			}
			string ctx = points[i].PromptTokensActual >= 1024
				? (points[i].PromptTokensActual / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + "K"
				: points[i].PromptTokensActual.ToString(CultureInfo.InvariantCulture);
			return points[i].Tag == "ENGINE" ? ctx + " E" : ctx;
		}
		_prefillChart.Update(points.Select((p, i) => new ChartBar
		{
			Group = LabelFor(i),
			Value = p.PrefillTps,
			Color = PrefillLine,
			SoftColor = PrefillFill,
			Series = string.Empty
		}).ToList());
		_genChart.Update(points.Select((p, i) => new ChartBar
		{
			Group = LabelFor(i),
			Value = p.GenerationTps,
			Color = GenLine,
			SoftColor = GenFill,
			Series = string.Empty
		}).ToList());
		UpdateLegend();
	}

	/// <summary>r112: runs picked in compare slots A–D, in slot order.</summary>
	private List<BenchmarkRun> ActiveSlotRuns()
	{
		var list = new List<BenchmarkRun>();
		for (int s = 0; s < 4; s++)
		{
			int i = _slotCombos[s].SelectedIndex;
			if (i >= 0 && i < _slotRuns[s].Count)
			{
				list.Add(_slotRuns[s][i]);
			}
		}
		return list;
	}

	/// <summary>r112: slot change → VM compare selection (slot A = baseline) +
	/// overlay charts.</summary>
	private void UpdateCompareFromSlots()
	{
		if (_bm == null)
		{
			return;
		}
		_bm.SetCompareRuns(ActiveSlotRuns().Select(r => r.Id).ToList());
		UpdateCharts();
	}

	/// <summary>r112: grouped bars across the selected runs — group = context
	/// label (sorted by actual context size), one bar per run in slot color.</summary>
	private void FillChartGrouped(BarChart chart, List<BenchmarkRun> slots, System.Func<BenchmarkPoint, double> metric)
	{
		var labelSizes = new Dictionary<string, int>();
		foreach (var run in slots)
		{
			foreach (var p in run.Points)
			{
				if (string.IsNullOrEmpty(p.Label))
				{
					continue;
				}
				int size = Math.Max(1, p.PromptTokensActual);
				if (!labelSizes.TryGetValue(p.Label, out int prev) || size < prev)
				{
					labelSizes[p.Label] = size;
				}
			}
		}
		var labels = labelSizes.OrderBy(kv => kv.Value, Comparer<int>.Default).Select(kv => kv.Key).ToList();
		var bars = new List<ChartBar>();
		for (int s = 0; s < slots.Count; s++)
		{
			var run = slots[s];
			foreach (var label in labels)
			{
				var p = run.Points.FirstOrDefault(q => q.Label == label);
				bars.Add(new ChartBar
				{
					Group = label,
					Value = p != null ? metric(p) : 0,
					Color = SlotColors[s],
					SoftColor = SlotSoftColors[s],
					Series = SlotLetters[s] + " — " + ShortRunName(run)
				});
			}
		}
		chart.Update(bars);
	}

	/// <summary>r112: legend above the charts — one entry per active slot.</summary>
	private void UpdateLegend()
	{
		if (_bm == null)
		{
			return;
		}
		_legend.Children.Clear();
		var slots = ActiveSlotRuns();
		if (slots.Count < 2)
		{
			_legend.IsVisible = false;
			return;
		}
		for (int s = 0; s < slots.Count; s++)
		{
			var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
			item.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = SlotColors[s], VerticalAlignment = VerticalAlignment.Center });
			item.Children.Add(new TextBlock
			{
				Text = SlotLetters[s] + " — " + ShortRunName(slots[s]),
				Foreground = Res("ThemeTextSecondary"),
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center
			});
			_legend.Children.Add(item);
		}
		_legend.IsVisible = true;
	}

	private static string ShortRunName(BenchmarkRun run)
	{
		string name = run.DisplayName.Length > 26 ? run.DisplayName[..26] + "…" : run.DisplayName;
		return name + " · " + run.CreatedAt.ToString("dd.MM HH:mm");
	}

	// ===== history (r103.1: dropdown above the charts) =====

	private void RebuildHistoryCombo()
	{
		if (_bm == null)
		{
			return;
		}
		// preserve the currently selected run across list rebuilds
		string? prevId = null;
		int cur = _historyCombo.SelectedIndex;
		if (cur >= 0 && cur < _historyComboRuns.Count)
		{
			prevId = _historyComboRuns[cur].Id;
		}
		_historyComboUpdating = true;
		_historyCombo.Items.Clear();
		_historyComboRuns.Clear();
		foreach (var run in _bm.HistoryRuns)
		{
			string badge = run.Source == "imported" ? T("bench.badge_imported") : string.Empty;
			_historyCombo.Items.Add(new ComboBoxItem
			{
				Content = run.DisplayName + badge + "  ·  " + run.CreatedAt.ToString("dd.MM HH:mm") + " · " + run.Status
					+ (run.DurationS > 0 ? " · " + run.DurationS.ToString("0", CultureInfo.InvariantCulture) + " с" : "")
					+ " · " + run.Points.Count + " т."
			});
			_historyComboRuns.Add(run);
		}
		// r107: the dropdown is NEVER empty — unless the user explicitly picked
		// a run, it always shows the LATEST one (list is newest-first).
		int sel;
		if (_userPickedHistory && prevId != null)
		{
			sel = _historyComboRuns.FindIndex(r => r.Id == prevId);
		}
		else
		{
			sel = _historyComboRuns.Count > 0 ? 0 : -1;
		}
		_historyCombo.SelectedIndex = sel;
		_historyComboUpdating = false;

		// r112: refresh the compare slots A–D (same run list, per-slot selection
		// kept); if a selected run was deleted, re-sync the VM compare selection.
		if (RebuildSlotCombos())
		{
			UpdateCompareFromSlots();
		}

		// r107: no empty charts either — if there are no live points (fresh app
		// start / new page), fill the table + charts with the selected (last) run.
		if (sel >= 0 && !_bm.IsRunning && _bm.CurrentPoints.Count == 0)
		{
			_bm.SelectRunCommand.Execute(_historyComboRuns[sel]);
		}

		bool hasSel = sel >= 0;
		_histExportBtn.IsEnabled = hasSel;
		_histDeleteBtn.IsEnabled = hasSel;
	}

	/// <summary>r112: fill the A–D slot dropdowns with the history list,
	/// keeping each slot's currently selected run across rebuilds. Returns true
	/// when a slot LOST its selection (its run was deleted) — the caller then
	/// syncs the VM compare selection with the slots.</summary>
	private bool RebuildSlotCombos()
	{
		bool anyLost = false;
		_slotUpdating = true;
		for (int s = 0; s < 4; s++)
		{
			string? prevId = null;
			int cur = _slotCombos[s].SelectedIndex;
			if (cur >= 0 && cur < _slotRuns[s].Count)
			{
				prevId = _slotRuns[s][cur].Id;
			}
			_slotCombos[s].Items.Clear();
			_slotRuns[s].Clear();
			foreach (var run in _bm!.HistoryRuns)
			{
				_slotCombos[s].Items.Add(new ComboBoxItem
				{
					Content = run.DisplayName + "  ·  " + run.CreatedAt.ToString("dd.MM HH:mm")
						+ " · " + run.Points.Count + " т."
				});
				_slotRuns[s].Add(run);
			}
			int sel = prevId != null ? _slotRuns[s].FindIndex(r => r.Id == prevId) : -1;
			if (prevId != null && sel < 0)
			{
				anyLost = true;
			}
			_slotCombos[s].SelectedIndex = sel;
		}
		_slotUpdating = false;
		return anyLost;
	}

	// ===== compare =====

	private void RebuildCompare()
	{
		if (_bm == null)
		{
			return;
		}
		_compareHost.Children.Clear();
		if (_bm.CompareRows.Count == 0)
		{
			_compareHost.Children.Add(new TextBlock { Text = T("bench.compare_empty"), Foreground = Res("ThemeTextTertiary"), FontSize = 13, TextWrapping = TextWrapping.Wrap });
			return;
		}
		var selected = _bm.CompareSelection
			.Select(id => _bm.HistoryRuns.FirstOrDefault(r => r.Id == id))
			.Where(r => r != null)
			.Cast<BenchmarkRun>()
			.ToList();
		// r112: colored column headers — the color IS the slot (matches the charts).
		var headers = new List<(string Text, IBrush Brush)> { (T("bench.th_context"), Res("ThemeTextTertiary")) };
		for (int i = 0; i < selected.Count; i++)
		{
			headers.Add((SlotLetters[i] + (i == 0 ? " (база) — " : " — ") + ShortRunName(selected[i]), SlotColors[i]));
		}
		_compareHost.Children.Add(MakeTableHeader(headers));
		foreach (var row in _bm.CompareRows)
		{
			// r112: PP and TG are separate rows (each run's value + Δ% + ×N vs A).
			var ppCells = new[] { row.Label + " · PP" }.Concat(row.Cells.Select(c =>
				c.Prefill > 0
					? c.Prefill.ToString("0.#", CultureInfo.InvariantCulture) + "  " + c.PrefillDiff + " · " + c.PrefillRatio
					: "—")).ToArray();
			_compareHost.Children.Add(MakeTableRow(ppCells));
			var tgCells = new[] { row.Label + " · TG" }.Concat(row.Cells.Select(c =>
				c.Gen > 0
					? c.Gen.ToString("0.#", CultureInfo.InvariantCulture) + "  " + c.GenDiff + " · " + c.GenRatio
					: "—")).ToArray();
			_compareHost.Children.Add(MakeTableRow(tgCells));
		}
	}

	// ===== table helpers =====

	private StackPanel MakeTableHeader(string[] headers)
	{
		return MakeTableHeader(headers.Select(h => (h, (IBrush)Res("ThemeTextTertiary"))).ToList());
	}

	/// <summary>r112: header with per-column colors (compare table: slot colors).</summary>
	private StackPanel MakeTableHeader(List<(string Text, IBrush Brush)> headers)
	{
		var row = new StackPanel { Orientation = Orientation.Horizontal };
		for (int i = 0; i < headers.Count; i++)
		{
			row.Children.Add(new TextBlock
			{
				Text = headers[i].Text,
				Foreground = headers[i].Brush,
				FontSize = 12,
				FontWeight = FontWeight.SemiBold,
				Width = i < TableWidths.Length ? TableWidths[i] : 110.0,
				Margin = new Thickness(3, 0, 3, 3)
			});
		}
		return row;
	}

	private StackPanel MakeTableRow(string[] cells)
	{
		var row = new StackPanel { Orientation = Orientation.Horizontal };
		for (int i = 0; i < cells.Length; i++)
		{
			row.Children.Add(new TextBlock
			{
				Text = cells[i],
				Foreground = i == 0 ? Res("ThemeTextPrimary") : Res("ThemeTextSecondary"),
				FontSize = 12,
				Width = i < TableWidths.Length ? TableWidths[i] : 100.0,
				Margin = new Thickness(3, 1, 3, 1)
			});
		}
		return row;
	}

	private static string Fmt(int v) => v > 0 ? v.ToString("N0", CultureInfo.InvariantCulture).Replace(" ", " ") : "—";
	private static string FmtTps(double v) => v > 0 ? v.ToString("0.#", CultureInfo.InvariantCulture) : "—";

	// ===== converters =====

	private class NotEmptyConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string s && s.Length > 0;
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	private class NotDashConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string s && s != "—" && s.Length > 0;
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	private class InverseBoolConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b ? !b : true;
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	// ===== r106/r112: interactive bar chart (grouped, per-bar color) =====

	/// <summary>r112: one bar. Group = x-axis category (context label); bars
	/// with the same Group render side by side; Color/SoftColor are the series
	/// colors (slot colors in compare mode); Series — hover/legend name.</summary>
	public sealed class ChartBar
	{
		public string Group = string.Empty;
		public double Value;
		public IBrush? Color;
		public IBrush? SoftColor;
		public string Series = string.Empty;
	}

	/// <summary>
	/// r106: responsive bar chart (no external libraries), r112: grouped bars —
	/// several series per group (compare mode: one color per history slot),
	/// value printed on top of every bar, hover shows the exact value + series.
	/// </summary>
	public sealed class BarChart : Canvas
	{
		private const double PadL = 46;
		private const double PadR = 12;
		private const double PadT = 24;
		private const double PadB = 26;
		private readonly IBrush _line;
		private readonly IBrush _fill;
		private List<ChartBar> _bars = new();
		private List<string> _groups = new();
		private int _hoverGroup = -1;
		private int _hoverBar = -1;

		public BarChart(IBrush line, IBrush fill)
		{
			_line = line;
			_fill = fill;
			SizeChanged += (_, _) => Render();
			PointerMoved += (_, e) =>
			{
				var (g, b) = BarAt(e.GetPosition(this));
				if (g != _hoverGroup || b != _hoverBar)
				{
					_hoverGroup = g;
					_hoverBar = b;
					Render();
				}
			};
			PointerExited += (_, _) =>
			{
				if (_hoverGroup != -1)
				{
					_hoverGroup = -1;
					_hoverBar = -1;
					Render();
				}
			};
			Render();
		}

		public void Update(IList<ChartBar> bars)
		{
			_bars = (bars ?? new List<ChartBar>()).Where(b => !string.IsNullOrEmpty(b.Group)).ToList();
			_groups = _bars.Select(b => b.Group).Distinct().ToList();
			_hoverGroup = _hoverBar = -1;
			Render();
		}

		private (int Group, int Bar) BarAt(Point pos)
		{
			if (_groups.Count == 0)
			{
				return (-1, -1);
			}
			double w = Bounds.Width;
			double h = Bounds.Height;
			if (pos.X < PadL - 4 || pos.X > w - PadR + 4 || pos.Y < PadT - 6 || pos.Y > h - PadB + 18)
			{
				return (-1, -1);
			}
			double slot = (w - PadL - PadR) / _groups.Count;
			int gi = Math.Clamp((int)((pos.X - PadL) / slot), 0, _groups.Count - 1);
			int m = BarsInGroup(gi).Count;
			if (m == 0)
			{
				return (-1, -1);
			}
			double innerW = slot * 0.82;
			double gx = PadL + slot * gi + (slot - innerW) / 2;
			int bi = Math.Clamp((int)((pos.X - gx) / (innerW / m)), 0, m - 1);
			return (gi, bi);
		}

		private List<ChartBar> BarsInGroup(int gi)
		{
			string group = _groups[gi];
			return _bars.Where(b => b.Group == group).ToList();
		}

		private void Render()
		{
			Children.Clear();
			double w = Bounds.Width;
			double h = Bounds.Height;
			if (w < 60 || h < 60)
			{
				return;
			}
			if (_bars.Count == 0 || _bars.All(b => b.Value <= 0))
			{
				var t = new TextBlock { Text = BenchmarkCenterControl.ChartEmptyText, Foreground = Res("ThemeTextTertiary"), FontSize = 13 };
				SetLeft(t, w / 2 - 80);
				SetTop(t, h / 2 - 10);
				Children.Add(t);
				return;
			}
			double plotW = w - PadL - PadR;
			double plotH = h - PadT - PadB;
			double rawMax = _bars.Max(b => b.Value);
			double maxY = rawMax * 1.15;
			// axes
			AddLine(PadL, h - PadB, w - PadR, h - PadB, GridLine);
			AddLine(PadL, PadT, PadL, h - PadB, GridLine);
			// gridlines + y labels
			for (int g = 1; g <= 3; g++)
			{
				double gy = h - PadB - plotH * g / 3.0;
				AddLine(PadL, gy, w - PadR, gy, GridLine);
				var yl = new TextBlock { Text = (maxY * g / 3.0).ToString("0.#", CultureInfo.InvariantCulture), Foreground = Res("ThemeTextTertiary"), FontSize = 11 };
				SetLeft(yl, 4);
				SetTop(yl, gy - 7);
				Children.Add(yl);
			}
			int n = _groups.Count;
			double slot = plotW / n;
			for (int gi = 0; gi < n; gi++)
			{
				var gBars = BarsInGroup(gi);
				int m = gBars.Count;
				double innerW = slot * 0.82;
				double barW = Math.Min(innerW / m, 64);
				double gx = PadL + slot * gi + (slot - innerW) / 2;
				for (int bi = 0; bi < m; bi++)
				{
					var bar = gBars[bi];
					IBrush solid = bar.Color ?? _line;
					IBrush soft = bar.SoftColor ?? _fill;
					bool hovered = _hoverGroup == gi && _hoverBar == bi;
					double bh = Math.Max(2, bar.Value / maxY * plotH);
					double x = gx + innerW * bi / m + (innerW / m - barW) / 2;
					double y = h - PadB - bh;
					var rect = new Border
					{
						Width = barW,
						Height = bh,
						Background = bar.Value <= 0 ? Brushes.Transparent : (hovered ? solid : soft),
						BorderBrush = solid,
						BorderThickness = new Thickness(1),
						CornerRadius = new CornerRadius(4, 4, 0, 0)
					};
					SetLeft(rect, x);
					SetTop(rect, y);
					Children.Add(rect);
					// value on top of the bar (when it fits — the table has exact values)
					if (barW >= 24)
					{
						var vl = new TextBlock
						{
							Text = bar.Value.ToString("0.#", CultureInfo.InvariantCulture),
							Foreground = hovered ? Res("ThemeTextPrimary") : Res("ThemeTextSecondary"),
							FontSize = 11,
							FontWeight = hovered ? FontWeight.SemiBold : FontWeight.Normal
						};
						vl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
						SetLeft(vl, Math.Max(2, x + barW / 2 - vl.DesiredSize.Width / 2));
						SetTop(vl, Math.Max(PadT - 16, y - 17));
						Children.Add(vl);
					}
				}
				// group label under the group (centered over all its bars)
				var xl = new TextBlock { Text = _groups[gi], Foreground = Res("ThemeTextTertiary"), FontSize = 11, TextAlignment = TextAlignment.Center };
				xl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				SetLeft(xl, Math.Max(2, PadL + slot * gi + slot / 2 - xl.DesiredSize.Width / 2));
				SetTop(xl, h - PadB + 6);
				Children.Add(xl);
			}
			// hover: exact value near the cursor (+ series in compare mode)
			if (_hoverGroup >= 0 && _hoverGroup < n)
			{
				var gBars = BarsInGroup(_hoverGroup);
				if (_hoverBar >= 0 && _hoverBar < gBars.Count)
				{
					var bar = gBars[_hoverBar];
					string what = string.IsNullOrEmpty(bar.Series)
						? _groups[_hoverGroup]
						: bar.Series + " · " + _groups[_hoverGroup];
					var tip = new Border
					{
						Background = new ImmutableSolidColorBrush(Color.Parse("#1E293B"), 0.96),
						CornerRadius = new CornerRadius(6),
						Padding = new Thickness(8, 5, 8, 5),
						Child = new TextBlock
						{
							Text = what + " · " + bar.Value.ToString("0.##", CultureInfo.InvariantCulture) + " tok/s",
							Foreground = Res("ThemeTextPrimary"),
							FontSize = 12
						}
					};
					tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					double slotX = plotW / n;
					double bx = PadL + slotX * _hoverGroup + slotX / 2;
					double lx = bx + 12;
					if (lx + tip.DesiredSize.Width > w - 4)
					{
						lx = bx - tip.DesiredSize.Width - 12;
					}
					SetLeft(tip, Math.Max(2, lx));
					SetTop(tip, PadT);
					Children.Add(tip);
				}
			}
		}

		private void AddLine(double x1, double y1, double x2, double y2, IBrush brush)
		{
			var fig = new PathFigure { StartPoint = new Point(x1, y1), IsClosed = false };
			fig.Segments.Add(new PolyLineSegment(new[] { new Point(x2, y2) }) { IsStroked = true });
			var geo = new PathGeometry();
			geo.Figures.Add(fig);
			Children.Add(new Path { Data = geo, Stroke = brush, StrokeThickness = 1 });
		}
	}
}
