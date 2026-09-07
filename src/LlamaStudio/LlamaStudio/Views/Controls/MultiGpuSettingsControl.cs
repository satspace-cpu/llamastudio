using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Controls;

public sealed class MultiGpuSettingsControl : UserControl
{
	private readonly StackPanel _content = new StackPanel { Spacing = 8 };
	private readonly MultiGpuServerMonitoringControl _serverMonitoringControl = new MultiGpuServerMonitoringControl();
	private ServerViewModel? _viewModel;
	private bool _sliderInteractionActive;
	private bool _rebuildQueued;
	private bool _queuedRebuildRequiresFull;
	private string _gpuStructureSignature = string.Empty;

	public MultiGpuSettingsControl()
	{
		Content = _content;
		PropertyChanged += (_, args) =>
		{
			if (args.Property == DataContextProperty)
			{
				SetViewModel(DataContext as ServerViewModel);
			}
		};
	}

	private void SetViewModel(ServerViewModel? viewModel)
	{
		if (_viewModel != null)
		{
			_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
			((INotifyCollectionChanged)_viewModel.GpuInfos).CollectionChanged -= OnGpuInfosCollectionChanged;
		}
		_viewModel = viewModel;
		if (_viewModel != null)
		{
			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
			((INotifyCollectionChanged)_viewModel.GpuInfos).CollectionChanged += OnGpuInfosCollectionChanged;
		}
		Rebuild();
	}

	public void AttachViewModel(ServerViewModel viewModel)
	{
		SetViewModel(viewModel);
	}

	private void OnGpuInfosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_sliderInteractionActive)
		{
			return;
		}
		if (!GpuStructureChanged())
		{
			return;
		}
		RequestRebuild(onlyIfStructureChanged: true);
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "GpuInfos" && !GpuStructureChanged())
		{
			return;
		}
		// HasMultipleGpus and SelectedGpuDeviceText are raised on every
		// monitoring tick (RefreshGpuAsync), even when nothing structural
		// changed. Only rebuild if the GPU structure (count / selection)
		// actually changed, so the management UI stays stable while the
		// monitoring panel refreshes independently.
		if ((e.PropertyName == "HasMultipleGpus" || e.PropertyName == "SelectedGpuDeviceText") && !GpuStructureChanged())
		{
			return;
		}
		if (e.PropertyName is "GpuInfos" or "HasMultipleGpus" or "SelectedGpuDeviceText" or "UsedGpusLabel" or "MainGpuDisabledTensorSplitLabel" or "ModelDistributionLabel" or "AutomaticDistributionLabel" or "GpuMonitoringLabel" or "GpuSplitMode" or "SplitModeLabel" || (e.PropertyName == "TensorSplit" && !_sliderInteractionActive))
		{
			RequestRebuild(onlyIfStructureChanged: e.PropertyName == "GpuInfos");
		}
	}

	private void RequestRebuild(bool onlyIfStructureChanged = false)
	{
		_queuedRebuildRequiresFull |= !onlyIfStructureChanged;
		if (_rebuildQueued)
		{
			return;
		}
		_rebuildQueued = true;
		Dispatcher.UIThread.Post(() =>
		{
			_rebuildQueued = false;
			bool requiresFullRebuild = _queuedRebuildRequiresFull;
			_queuedRebuildRequiresFull = false;
			if (requiresFullRebuild || GpuStructureChanged())
			{
				Rebuild();
			}
		});
	}

	private void Rebuild()
	{
		_gpuStructureSignature = GetGpuStructureSignature();
		_content.Children.Clear();
		if (_viewModel == null || _viewModel.GpuInfos.Count == 0)
		{
			IsVisible = false;
			return;
		}
		if (_viewModel.GpuInfos.Count <= 1)
		{
			IsVisible = true;
			_serverMonitoringControl.DataContext = _viewModel;
			_serverMonitoringControl.Margin = new Thickness(0, 8, 0, 0);
			_content.Children.Add(_serverMonitoringControl);
			return;
		}
		IsVisible = true;
		_content.Children.Add(new TextBlock { Text = _viewModel.UsedGpusLabel, FontSize = 13, FontWeight = FontWeight.DemiBold });
		foreach (GpuInfo gpu in _viewModel.GpuInfos)
		{
			CheckBox checkBox = new CheckBox { Content = gpu.DisplayName, IsChecked = gpu.IsSelected };
			int index = gpu.Index;
			checkBox.IsCheckedChanged += (_, _) => _viewModel.SetGpuSelected(index, checkBox.IsChecked == true);
			_content.Children.Add(checkBox);
		}

		TextBlock splitModeLabel = new TextBlock { Text = "Режим распределения", FontSize = 13, FontWeight = FontWeight.DemiBold, Margin = new Thickness(0, 4, 0, 0) };
		ToolTip.SetTip(splitModeLabel, "Тензоры задают доли для выбранных GPU; слои оставляют распределение llama.cpp.");
		_content.Children.Add(splitModeLabel);
		_content.Children.Add(CreateSplitModeRadios());

		bool tensorSplitActive = _viewModel.GpuInfos.Count(item => item.IsSelected) > 1 && !string.IsNullOrWhiteSpace(_viewModel.TensorSplit);
		_content.Children.Add(new TextBlock { Text = tensorSplitActive ? _viewModel.MainGpuDisabledTensorSplitLabel : _viewModel.MainGpuShortLabel, Foreground = new SolidColorBrush(Color.Parse("#A8AFBF")), Margin = new Thickness(0, 4, 0, 0) });
		foreach (GpuInfo gpu in _viewModel.GpuInfos.Where(item => item.IsSelected && !item.IsRemote))
		{
			RadioButton radio = new RadioButton { Content = gpu.DisplayName, GroupName = "MainGpu", IsChecked = gpu.Index == _viewModel.MainGpu, IsEnabled = !tensorSplitActive };
			ToolTip.SetTip(radio, tensorSplitActive ? _viewModel.MainGpuSplitTip : _viewModel.MainGpuTip);
			int index = gpu.Index;
			radio.IsCheckedChanged += (_, _) => { if (radio.IsChecked == true) _viewModel.SetMainGpuFromSelection(index); };
			_content.Children.Add(radio);
		}

		GpuInfo[] ordered = _viewModel.GetOrderedSelectedGpus().ToArray();
		if (ordered.Length >= 2)
		{
			_content.Children.Add(new TextBlock { Text = _viewModel.ModelDistributionLabel });
			_content.Children.Add(CreateDistributionPanel(ordered));
			StackPanel presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Left };
			Button byVram = new Button { Content = "По VRAM", Padding = new Thickness(10, 4) };
			byVram.Click += (_, _) => _viewModel.ApplyVramTensorSplit();
			Button equal = new Button { Content = "Поровну", Padding = new Thickness(10, 4) };
			equal.Click += (_, _) => _viewModel.ApplyEqualTensorSplit();
			Button auto = new Button { Content = "Автоматически", Padding = new Thickness(10, 4) };
			auto.Click += (_, _) => _viewModel.ApplyAutomaticTensorSplit();
			presets.Children.Add(byVram);
			presets.Children.Add(equal);
			presets.Children.Add(auto);
			_content.Children.Add(presets);
		}
		_serverMonitoringControl.DataContext = _viewModel;
		_serverMonitoringControl.Margin = new Thickness(0, 8, 0, 0);
		_content.Children.Add(_serverMonitoringControl);
	}

	private Control CreateSplitModeRadios()
	{
		StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 0) };
		// Row is omitted: it is not reliably supported in current llama.cpp
		// builds for this hardware. Only Layer (default) and Tensor are offered.
		// If the profile holds a legacy value (None/Row), fall back to Layer.
		GpuSplitMode current = _viewModel.GpuSplitMode;
		if (current != GpuSplitMode.Layer && current != GpuSplitMode.Tensor)
		{
			current = GpuSplitMode.Layer;
			_viewModel.SetGpuSplitMode(current);
		}
		(GpuSplitMode mode, string label)[] options = new (GpuSplitMode, string)[2]
		{
			(GpuSplitMode.Layer, "Слои"),
			(GpuSplitMode.Tensor, "Тензоры")
		};
		foreach ((GpuSplitMode mode, string label) in options)
		{
			RadioButton radio = new RadioButton
			{
				Content = label,
				GroupName = "SplitMode",
				IsChecked = current == mode,
				VerticalAlignment = VerticalAlignment.Center
			};
			ToolTip.SetTip(radio, "Тензоры задают доли для выбранных GPU; слои оставляют распределение llama.cpp.");
			GpuSplitMode captured = mode;
			radio.IsCheckedChanged += (_, _) => { if (radio.IsChecked == true) _viewModel?.SetGpuSplitMode(captured); };
			panel.Children.Add(radio);
		}
		return panel;
	}

	private bool GpuStructureChanged() => !string.Equals(_gpuStructureSignature, GetGpuStructureSignature(), StringComparison.Ordinal);

	private string GetGpuStructureSignature()
	{
		if (_viewModel == null)
		{
			return string.Empty;
		}
		// Include IsSelected so that a selection change is treated as a
		// structural change (rebuild), while pure metric updates (VRAM, temp,
		// clocks) are not. This keeps the management UI stable while the
		// monitoring panel refreshes every tick.
		return string.Join("|", _viewModel.GpuInfos.Select(gpu => $"{gpu.Index}:{gpu.RemoteDeviceId}:{gpu.Name}:{gpu.IsRemote}:{gpu.IsSelected}"));
	}

	private Control CreateDistributionPanel(GpuInfo[] ordered)
	{
		int[] split = _viewModel.GetEffectiveSplit();
		StackPanel panel = new StackPanel { Spacing = 4 };
		for (int i = 0; i < ordered.Length; i++)
		{
			int percent = i < split.Length ? split[i] : 0;
			panel.Children.Add(CreateDeviceSplitRow(ordered[i], percent, i));
		}
		return panel;
	}

	private Control CreateDeviceSplitRow(GpuInfo gpu, int percent, int position)
	{
		Grid grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto"),
			Margin = new Thickness(0, 1, 0, 1)
		};
		StackPanel namePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		namePanel.Children.Add(new TextBlock { Text = gpu.Name, FontSize = 13 });
		if (gpu.IsRemote)
		{
			namePanel.Children.Add(new Border
			{
				Child = new TextBlock { Text = "RPC", FontSize = 10, Foreground = Brushes.White },
				Background = new SolidColorBrush(Color.Parse("#8A6D1A")),
				CornerRadius = new CornerRadius(3),
				Padding = new Thickness(4, 1, 4, 1),
				VerticalAlignment = VerticalAlignment.Center
			});
		}
		Grid.SetColumn(namePanel, 0);
		grid.Children.Add(namePanel);
		StackPanel right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		right.Children.Add(new TextBlock
		{
			Text = $"{percent}%",
			FontSize = 14,
			FontWeight = FontWeight.SemiBold,
			MinWidth = 44,
			TextAlignment = TextAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		});
		foreach ((string label, int delta) in new[] { ("−5", -5), ("−1", -1), ("+1", 1), ("+5", 5) })
		{
			Button button = new Button { Content = label, Padding = new Thickness(8, 2), MinWidth = 34 };
			button.Click += (_, _) => _viewModel.AdjustTensorSplit(position, delta);
			right.Children.Add(button);
		}
		Grid.SetColumn(right, 1);
		grid.Children.Add(right);
		return grid;
	}
}
