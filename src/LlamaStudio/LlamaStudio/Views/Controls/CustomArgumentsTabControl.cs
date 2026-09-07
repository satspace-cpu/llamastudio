using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Controls;

/// <summary>
/// Editor for arguments that are not represented by the primary Server tabs.
/// The existing picker and AdditionalArgs property remain the single source of truth.
/// </summary>
public sealed class CustomArgumentsTabControl : UserControl
{
    public CustomArgumentsTabControl()
    {
        var title = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.DemiBold,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        title.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.CustomArgsTitle)));

        var description = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        description.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.CustomArgsDesc)));

        var editor = new TextBox
        {
            AcceptsReturn = true,
            MinHeight = 150,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextWrapping = TextWrapping.Wrap,
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6)
        };
        editor.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.AdditionalArgs))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        editor.Bind(TextBox.WatermarkProperty, new Binding(nameof(ServerViewModel.CustomArgsWatermark)));

        var pickerButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = ThemeBrushes.Get("ThemeAccent", "#5B4BFF"),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 8),
            CornerRadius = new CornerRadius(5)
        };
        pickerButton.Bind(Button.ContentProperty, new Binding(nameof(ServerViewModel.AddArgsBtnText)));
        pickerButton.Bind(Button.CommandProperty, new Binding(nameof(ServerViewModel.OpenArgPickerCommand)));

        var note = new TextBlock
        {
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextMuted", "#7F8AA3")
        };
        note.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.CustomArgsNote)));

        var argumentsPanel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(8),
            Children = { title, description, editor, pickerButton, note }
        };

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = argumentsPanel
        };
    }
}
