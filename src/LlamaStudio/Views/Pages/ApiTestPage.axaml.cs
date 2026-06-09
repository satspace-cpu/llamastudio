using Avalonia.Controls;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class ApiTestPage : UserControl
{
    public ApiTestPage()
    {
        InitializeComponent();
    }

    public ApiTestPage(ApiTestViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
