using Avalonia.Controls;
using Avalonia.Interactivity;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class MonitoringPage : UserControl
{
    readonly MonitoringViewModel _vm;
    public MonitoringPage(MonitoringViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    void OpenFloating_Click(object? sender, RoutedEventArgs e)
    {
         _vm.OpenFloatingWindowCommand?.Execute(null);
    }
}
