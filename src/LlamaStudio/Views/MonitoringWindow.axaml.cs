using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views;

public partial class MonitoringWindow : Window
{
    readonly MonitoringViewModel _vm;

    public MonitoringWindow(MonitoringViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;

        // Use BeginMoveDrag for smooth dragging — no custom logic needed
        AddHandler(PointerPressedEvent, OnTitleBarPointerPressed, RoutingStrategies.Tunnel);
    }

    void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetPosition(this).Y <= 36 && sender is not Button && CloseBtn?.IsPointerOver != true)
        {
            this.BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        this.Hide();
    }
}
