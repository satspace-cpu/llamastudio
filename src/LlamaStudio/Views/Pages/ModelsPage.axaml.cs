using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class ModelsPage : UserControl
{
    public ModelsPage()
    {
        InitializeComponent();
    }

    public ModelsPage(ModelsViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
