using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsPage(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
