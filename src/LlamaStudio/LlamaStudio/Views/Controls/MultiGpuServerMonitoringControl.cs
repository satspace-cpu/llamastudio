using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;
using Avalonia.Threading;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Controls;

public sealed class MultiGpuServerMonitoringControl : UserControl
{
	private readonly StackPanel _cards = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
	private readonly ScrollViewer _scrollViewer;
	private ServerViewModel? _viewModel;
	private bool _rebuildQueued;

	public MultiGpuServerMonitoringControl()
	{
		_scrollViewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _cards };
		Content = _scrollViewer;
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
		}
		_viewModel = viewModel;
		if (_viewModel != null)
		{
			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}
		Rebuild();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is "GpuInfos" or "GpuInfosTelemetry" or "GpuMonitoringLabel" or "RamLabel" or "RamUsedText" or "RamPercent" or "VramLabel" or "GpuCoreLabel" or "GpuTempLabel" or "GpuMemTempLabel" or "PowerLabel" or "FanLabel" or "ClockLabel" or "ComputeCapabilityLabel")
		{
			if (_rebuildQueued)
			{
				return;
			}
			_rebuildQueued = true;
			Dispatcher.UIThread.Post(() =>
			{
				_rebuildQueued = false;
				Rebuild();
			});
		}
	}

	private void Rebuild()
	{
		Vector savedOffset = _scrollViewer.Offset;
		_cards.Children.Clear();
		if (_viewModel == null || _viewModel.GpuInfos.Count == 0)
		{
			IsVisible = false;
			return;
		}
		IsVisible = true;
		_cards.Children.Add(new TextBlock { Text = _viewModel.GpuMonitoringLabel, FontSize = 14, FontWeight = FontWeight.DemiBold });
		StackPanel ram = new StackPanel { Spacing = 3 };
		ram.Children.Add(new TextBlock { Text = "RAM", FontWeight = FontWeight.DemiBold, Foreground = Brush("#55D6BE") });
		AddBar(ram, _viewModel.RamLabel, _viewModel.RamPercent, _viewModel.RamUsedText, Brush("#10B981"));
		_cards.Children.Add(new Border { Padding = new Thickness(12), Child = ram, HorizontalAlignment = HorizontalAlignment.Stretch, Background = ThemeBrushes.Get("GpuMonitorInner", "#111A2A"), CornerRadius = new CornerRadius(7) });
		foreach (GpuInfo gpu in _viewModel.GpuInfos)
		{
			StackPanel metrics = new StackPanel { Spacing = 6 };
			Grid gpuHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
			gpuHeader.Children.Add(new TextBlock { Text = gpu.DisplayName, FontSize = 15, FontWeight = FontWeight.DemiBold, VerticalAlignment = VerticalAlignment.Center });
			if (gpu.IsRemote)
			{
				var remoteTelemetry = new CheckBox
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(10, 0, 0, 0)
				};
				remoteTelemetry.Bind(ContentControl.ContentProperty, new Binding(nameof(ServerViewModel.RemoteTelemetryLabel)));
				remoteTelemetry.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ServerViewModel.RemoteTelemetryEnabled))
				{
					Mode = BindingMode.TwoWay
				});
				remoteTelemetry.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcSshConnected)));
				Grid.SetColumn(remoteTelemetry, 1);
				gpuHeader.Children.Add(remoteTelemetry);
			}
			metrics.Children.Add(gpuHeader);
			AddBar(metrics, _viewModel.VramLabel, gpu.MemoryPercent, $"{gpu.MemoryUsedGb:F1} / {gpu.MemoryTotalGb:F1} GB", Brush("#3B82F6"));
			AddBar(metrics, _viewModel.GpuCoreLabel, gpu.GpuUtilization, $"{gpu.GpuUtilization:F0}%", Brush("#8B5CF6"));
			AddBar(metrics, _viewModel.PowerLabel, gpu.PowerPercent, $"{gpu.PowerDrawWatts:F0} / {gpu.PowerLimitWatts:F0} W", Brush("#F59E0B"));
			AddBar(metrics, _viewModel.GpuTempLabel, Math.Min(100, gpu.TemperatureCelsius / 95 * 100), $"{gpu.TemperatureCelsius:F0} C", Brush("#10B981"));
			if (gpu.MemoryTemperatureCelsius > 0)
			{
				AddBar(metrics, _viewModel.GpuMemTempLabel, Math.Min(100, gpu.MemoryTemperatureCelsius / 110 * 100), $"{gpu.MemoryTemperatureCelsius:F0} C", Brush("#EF4444"));
			}
			AddBar(metrics, _viewModel.FanLabel, gpu.FanSpeed, $"{gpu.FanSpeed}%", Brush("#06B6D4"));
			metrics.Children.Add(new TextBlock { Text = $"{_viewModel.ClockLabel} {gpu.ClockMhz} MHz   {_viewModel.ComputeCapabilityLabel} {gpu.ComputeCapability:F1}", FontSize = 11 });
			_cards.Children.Add(new Border { Padding = new Thickness(12), Child = metrics, HorizontalAlignment = HorizontalAlignment.Stretch, Background = ThemeBrushes.Get("GpuMonitorBackground", "#17142B"), BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4352"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7) });
		}
		Dispatcher.UIThread.Post(() => _scrollViewer.Offset = savedOffset, DispatcherPriority.Loaded);
	}

	private static void AddBar(StackPanel panel, string label, double value, string text, IBrush color)
	{
		StackPanel block = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
		Grid header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), HorizontalAlignment = HorizontalAlignment.Stretch };
		header.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
		TextBlock valueText = new TextBlock { Text = text, FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(valueText, 1);
		header.Children.Add(valueText);
		block.Children.Add(header);
		block.Children.Add(new ProgressBar { Value = Math.Max(0, Math.Min(100, value)), Maximum = 100, Height = 9, Foreground = color, HorizontalAlignment = HorizontalAlignment.Stretch });
		panel.Children.Add(block);
	}

	private static SolidColorBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
}
