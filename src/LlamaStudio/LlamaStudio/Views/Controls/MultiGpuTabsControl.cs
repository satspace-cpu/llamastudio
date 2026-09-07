using System;
using System.ComponentModel;
using System.Linq;
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

public sealed class MultiGpuTabsControl : UserControl
{
    private readonly TabControl _tabs = new TabControl { HorizontalAlignment = HorizontalAlignment.Stretch };
    private MonitoringViewModel? _viewModel;
    private bool _rebuildQueued;

    public MultiGpuTabsControl()
    {
        Content = _tabs;
        PropertyChanged += (_, args) =>
        {
            if (args.Property == DataContextProperty)
            {
                SetViewModel(DataContext as MonitoringViewModel);
            }
        };
    }

    private void SetViewModel(MonitoringViewModel? viewModel)
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
		if (e.PropertyName is "GpuInfos" or "AllGpusLabel" or "GpuLoadLabel" or "TotalGpuPowerLabel" or "ClockLabel" or "ComputeCapabilityLabel" or "VramLabel" or "GpuCoreLabel" or "GpuTempLabel" or "GpuMemTempLabel" or "PowerLabel" or "FanLabel")
        {
            RequestRebuild();
        }
    }

    private void RequestRebuild()
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

	private void Rebuild()
	{
		int selectedIndex = _tabs.SelectedIndex;
		_tabs.Items.Clear();
        if (_viewModel == null || _viewModel.GpuInfos.Count == 0)
        {
            IsVisible = false;
            return;
        }
        IsVisible = true;
		_tabs.Items.Add(new TabItem { Header = _viewModel.AllGpusLabel, Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = BuildAggregate() } });
		foreach (GpuInfo gpu in _viewModel.GpuInfos)
		{
			_tabs.Items.Add(new TabItem { Header = $"GPU{gpu.Index}", Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = BuildGpu(gpu) } });
		}
		_tabs.SelectedIndex = Math.Max(0, Math.Min(selectedIndex, _tabs.ItemCount - 1));
	}

    private Control BuildAggregate()
    {
        StackPanel aggregatePanel = new StackPanel { Spacing = 8, Margin = new Thickness(8) };
        AddMetric(aggregatePanel, _viewModel!.VramLabel, TotalMemoryPercent(), _viewModel.AllGpuVramText, Brush("#3B82F6"));
		AddMetric(aggregatePanel, _viewModel.GpuLoadLabel, AverageLoad(), _viewModel.AllGpuLoadText, Brush("#8B5CF6"));
		AddMetric(aggregatePanel, _viewModel.TotalGpuPowerLabel, TotalPowerPercent(), _viewModel.AllGpuPowerText, Brush("#F59E0B"));
        foreach (GpuInfo gpu in _viewModel.GpuInfos)
        {
		aggregatePanel.Children.Add(new Border { Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(8), Background = ThemeBrushes.Get("GpuMonitorBackground", "#17142B"), CornerRadius = new CornerRadius(6), Child = BuildGpu(gpu) });
        }
        return aggregatePanel;
    }

    private Control BuildGpu(GpuInfo gpu)
    {
        StackPanel gpuPanel = new StackPanel { Spacing = 6, Margin = new Thickness(8) };
        gpuPanel.Children.Add(new TextBlock { Text = gpu.Name, FontWeight = FontWeight.DemiBold, FontSize = 14 });
        AddMetric(gpuPanel, _viewModel!.VramLabel, gpu.MemoryPercent, $"{gpu.MemoryUsedGb:F1} / {gpu.MemoryTotalGb:F1} GB", Brush("#3B82F6"));
        AddMetric(gpuPanel, _viewModel.GpuCoreLabel, gpu.GpuUtilization, $"{gpu.GpuUtilization:F0}%", Brush("#8B5CF6"));
        AddMetric(gpuPanel, _viewModel.GpuTempLabel, Math.Min(100, gpu.TemperatureCelsius / 95 * 100), $"{gpu.TemperatureCelsius:F0} C", Brush("#10B981"));
        if (gpu.MemoryTemperatureCelsius > 0)
        {
            AddMetric(gpuPanel, _viewModel.GpuMemTempLabel, Math.Min(100, gpu.MemoryTemperatureCelsius / 110 * 100), $"{gpu.MemoryTemperatureCelsius:F0} C", Brush("#EF4444"));
        }
        AddMetric(gpuPanel, _viewModel.PowerLabel, gpu.PowerPercent, $"{gpu.PowerDrawWatts:F0} / {gpu.PowerLimitWatts:F0} W", Brush("#F59E0B"));
        AddMetric(gpuPanel, _viewModel.FanLabel, gpu.FanSpeed, $"{gpu.FanSpeed}%", Brush("#06B6D4"));
		gpuPanel.Children.Add(new TextBlock { Text = $"{_viewModel.ClockLabel} {gpu.ClockMhz} MHz   {_viewModel.ComputeCapabilityLabel} {gpu.ComputeCapability:F1}", FontSize = 11 });
        return gpuPanel;
    }

    private double TotalMemoryPercent()
    {
        double total = _viewModel!.GpuInfos.Sum(gpu => gpu.MemoryTotalGb);
        return total <= 0 ? 0 : _viewModel.GpuInfos.Sum(gpu => gpu.MemoryUsedGb) / total * 100;
    }

    private double AverageLoad() => _viewModel!.GpuInfos.Count == 0 ? 0 : _viewModel.GpuInfos.Average(gpu => gpu.GpuUtilization);

    private double TotalPowerPercent()
    {
        double limit = _viewModel!.GpuInfos.Sum(gpu => gpu.PowerLimitWatts);
        return limit <= 0 ? 0 : _viewModel.GpuInfos.Sum(gpu => gpu.PowerDrawWatts) / limit * 100;
    }

    private static void AddMetric(StackPanel panel, string label, double percent, string value, IBrush color)
    {
        Grid header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(new TextBlock { Text = label, Foreground = color, FontSize = 10, FontWeight = FontWeight.DemiBold });
        TextBlock valueText = new TextBlock { Text = value, FontSize = 10, TextAlignment = TextAlignment.Right };
        Grid.SetColumn(valueText, 1);
        header.Children.Add(valueText);
        panel.Children.Add(header);
		panel.Children.Add(new ProgressBar { Value = Math.Max(0, Math.Min(100, percent)), Maximum = 100, Height = 7, Background = ThemeBrushes.Get("ProgressTrack", "#34216B"), Foreground = color, CornerRadius = new CornerRadius(4) });
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
}
