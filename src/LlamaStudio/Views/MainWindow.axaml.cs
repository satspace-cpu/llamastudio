using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (App.IsShuttingDown)
        {
            base.OnClosing(e);
            return;
        }

        var services = App.Services;
        if (services == null)
        {
            base.OnClosing(e);
            return;
        }

        var settings = services.GetRequiredService<ISettings>();

        if (settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
