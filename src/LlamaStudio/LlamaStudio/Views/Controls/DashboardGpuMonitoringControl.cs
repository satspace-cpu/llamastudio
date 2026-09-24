using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;
using Avalonia.Threading;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Controls;

public sealed class DashboardGpuMonitoringControl : UserControl
{
	private readonly StackPanel _cards = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
	private readonly ScrollViewer _scrollViewer;
	private DashboardViewModel? _viewModel;
	private bool _rebuildQueued;

	public DashboardGpuMonitoringControl()
	{
		_scrollViewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _cards };
		Content = _scrollViewer;
		PropertyChanged += (_, args) =>
		{
			if (args.Property == DataContextProperty)
			{
				SetViewModel(DataContext as DashboardViewModel);
			}
		};
	}

	private void SetViewModel(DashboardViewModel? viewModel)
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
		if (e.PropertyName is "GpuInfos" or "RamUsedText" or "RamPercentText" or "GpuVramLabel" or "GpuCoreLabel" or "GpuPowerLabel" or "GpuTempLabel" or "GpuMemTempLabel" or "GpuFanLabel")
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
		Vector offset = _scrollViewer.Offset;
		_cards.Children.Clear();
		if (_viewModel == null || _viewModel.GpuInfos.Count == 0)
		{
			IsVisible = false;
			return;
		}
		IsVisible = true;
		StackPanel ram = new StackPanel { Spacing = 3 };
		ram.Children.Add(new TextBlock { Text = "RAM", FontWeight = FontWeight.DemiBold, Foreground = Brush("#55D6BE") });
		AddBar(ram, global::LlamaStudio.NativeLanguage.Text("ОЗУ", "RAM"), _viewModel.RamUsedPercent, _viewModel.RamUsedText, Brush("#10B981"));
		_cards.Children.Add(new Border { Padding = new Thickness(12), Child = ram, Background = ThemeBrushes.Get("GpuMonitorInner", "#111A2A"), CornerRadius = new CornerRadius(7) });
		foreach (GpuInfo gpu in _viewModel.GpuInfos)
		{
			StackPanel metrics = new StackPanel { Spacing = 6 };
			metrics.Children.Add(new TextBlock { Text = gpu.DisplayName, FontSize = 15, FontWeight = FontWeight.DemiBold });
			AddBar(metrics, _viewModel.GpuVramLabel, gpu.MemoryPercent, $"{gpu.MemoryUsedGb:F1} / {gpu.MemoryTotalGb:F1} GB", Brush("#3B82F6"));
			AddBar(metrics, _viewModel.GpuCoreLabel, gpu.GpuUtilization, $"{gpu.GpuUtilization:F0}%", Brush("#8B5CF6"));
			AddBar(metrics, _viewModel.GpuPowerLabel, gpu.PowerPercent, $"{gpu.PowerDrawWatts:F0} / {gpu.PowerLimitWatts:F0} W", Brush("#F59E0B"));
			AddBar(metrics, _viewModel.GpuTempLabel, Math.Min(100, gpu.TemperatureCelsius / 95 * 100), $"{gpu.TemperatureCelsius:F0} C", Brush("#10B981"));
			if (gpu.MemoryTemperatureCelsius > 0)
			{
				AddBar(metrics, _viewModel.GpuMemTempLabel, Math.Min(100, gpu.MemoryTemperatureCelsius / 110 * 100), $"{gpu.MemoryTemperatureCelsius:F0} C", Brush("#EF4444"));
			}
			AddBar(metrics, _viewModel.GpuFanLabel, gpu.FanSpeed, $"{gpu.FanSpeed}%", Brush("#06B6D4"));
			metrics.Children.Add(new TextBlock { Text = $"Clock {gpu.ClockMhz} MHz   Compute Capability {gpu.ComputeCapability:F1}", FontSize = 11 });
		_cards.Children.Add(new Border { Padding = new Thickness(12), Child = metrics, HorizontalAlignment = HorizontalAlignment.Stretch, Background = ThemeBrushes.Get("GpuMonitorBackground", "#17142B"), BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4352"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7) });
		}
		Dispatcher.UIThread.Post(() => _scrollViewer.Offset = offset, DispatcherPriority.Loaded);
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
