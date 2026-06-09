using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    public DashboardPage(DashboardViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
