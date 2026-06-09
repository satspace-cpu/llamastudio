using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class LlamaReleasesPage : UserControl
{
    public LlamaReleasesPage()
    {
        InitializeComponent();
    }

    public LlamaReleasesPage(LlamaReleasesViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
        Loaded += (s, e) => viewModel.RefreshInstalledVersionsCommand.Execute(null);
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
