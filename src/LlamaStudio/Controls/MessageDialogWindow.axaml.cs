 using Avalonia;
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Media;

namespace LlamaStudio.Controls;

public class MessageDialogWindow : Window
{
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MessageDialogWindow, string>(nameof(Message));

    public MessageBoxButton ButtonType
    {
        get => GetValue(ButtonTypeProperty);
        set => SetValue(ButtonTypeProperty, value);
    }

    public static readonly StyledProperty<MessageBoxButton> ButtonTypeProperty =
        AvaloniaProperty.Register<MessageDialogWindow, MessageBoxButton>(nameof(ButtonType));

    public MessageBoxButtonResult Result { get; private set; } = MessageBoxButtonResult.None;

    TextBlock? _titleBlock;
    TextBlock? _messageBlock;
    Button? _cancelButton;
    Button? _okButton;

    static readonly Avalonia.Media.FontFamily s_font = Avalonia.Media.FontFamily.Parse("Segoe UI, Arial, sans-serif");

    public MessageDialogWindow()
    {
        Width = 450;
        Height = 250;
        MinWidth = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SystemDecorations = SystemDecorations.None;
        Background = new SolidColorBrush(Color.Parse("#0F172A"));

        Content = CreateDialogContent();
        
        this.GetObservable(MessageProperty).Subscribe(_ => UpdateContent());
        this.GetObservable(ButtonTypeProperty).Subscribe(_ => UpdateButtons());
        this.GetObservable(Window.TitleProperty).Subscribe(_ => UpdateContent());
    }

    Control CreateDialogContent()
    {
        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(20) };

        _titleBlock = new TextBlock
        {
            FontSize = 18,
            FontWeight = (FontWeight)600,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = s_font,
        };

        _messageBlock = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 200,
            FontFamily = s_font,
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        _cancelButton = new Button
        {
            Content = "No",
            Background = new SolidColorBrush(Color.Parse("#475569")),
            Foreground = Brushes.White,
            Padding = new Thickness(16, 8, 16, 8),
            CornerRadius = new CornerRadius(6),
            IsVisible = false
        };
        _cancelButton.Click += (s, e) =>
        {
            Result = MessageBoxButtonResult.No;
            Close();
        };

        _okButton = new Button
        {
            Content = "OK",
            Background = new SolidColorBrush(Color.Parse("#3B82F6")),
            Foreground = Brushes.White,
            Padding = new Thickness(16, 8, 16, 8),
            CornerRadius = new CornerRadius(6)
        };
        _okButton.Click += (s, e) =>
        {
            Result = ButtonType == MessageBoxButton.YesNo ? MessageBoxButtonResult.Yes : MessageBoxButtonResult.Ok;
            Close();
        };

        buttonPanel.Children.Add(_cancelButton);
        buttonPanel.Children.Add(_okButton);

        panel.Children.Add(_titleBlock);
        panel.Children.Add(_messageBlock);
        panel.Children.Add(buttonPanel);

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E293B")),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            Child = panel
        };

        UpdateContent();
        UpdateButtons();
        return border;
    }

    void UpdateContent()
    {
        _titleBlock!.Text = Title;
        _messageBlock!.Text = Message;
    }

    void UpdateButtons()
    {
        if (ButtonType == MessageBoxButton.YesNo)
        {
            _cancelButton!.IsVisible = true;
            _okButton!.Content = "Yes";
        }
        else
        {
            _cancelButton!.IsVisible = false;
            _okButton!.Content = "OK";
        }
    }
}
