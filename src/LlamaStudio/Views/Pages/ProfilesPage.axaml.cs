using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

public partial class ProfilesPage : UserControl
{
    public ProfilesPage()
    {
        InitializeComponent();
    }

    public ProfilesPage(ProfilesViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
