using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class ServerPage : UserControl
{
    public ServerPage()
    {
        InitializeComponent();
    }

    public ServerPage(ServerViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
