  using Avalonia.Controls;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views;

public partial class ArgPickerWindow : Window
{
    public bool IsConfirmed { get; private set; }
    public ArgPickerViewModel ViewModel => (ArgPickerViewModel)DataContext!;

    public ArgPickerWindow()
    {
        InitializeComponent();
    }

    void AddButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }

    void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
