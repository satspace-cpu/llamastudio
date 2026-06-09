using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LlamaStudio.Controls;

public class ProfileSelectDialogWindow : Window
{
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ProfileSelectDialogWindow, string>(nameof(Message));

    public string? SelectedProfileName { get; private set; }
    public bool ResultConfirmed { get; private set; }

    TextBlock? _messageBlock;
    ComboBox? _comboBox;

    public ProfileSelectDialogWindow()
    {
        Width = 400;
        Height = 180;
        MinWidth = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SystemDecorations = SystemDecorations.None;
        Background = new SolidColorBrush(Color.Parse("#0F172A"));

        Content = CreateDialogContent();
        this.GetObservable(MessageProperty).Subscribe(_ => UpdateContent());
    }

    public void SetItems(IEnumerable<string> items)
    {
        _comboBox?.Items.Clear();
        foreach (var item in items)
        {
            _comboBox?.Items.Add(item);
        }
        if (_comboBox != null && _comboBox.Items.Count > 0)
        {
            _comboBox.SelectedIndex = 0;
        }
    }

    Control CreateDialogContent()
    {
        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(20) };

        _messageBlock = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
            TextWrapping = TextWrapping.Wrap,
        };

        _comboBox = new ComboBox
        {
            Background = new SolidColorBrush(Color.Parse("#0F172A")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(8),
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Background = new SolidColorBrush(Color.Parse("#475569")),
            Foreground = Brushes.White,
            Padding = new Thickness(24, 8, 24, 8),
            CornerRadius = new CornerRadius(6)
        };
        cancelButton.Click += (s, e) =>
        {
            ResultConfirmed = false;
            Close();
        };

        var okButton = new Button
        {
            Content = "OK",
            Background = new SolidColorBrush(Color.Parse("#3B82F6")),
            Foreground = Brushes.White,
            Padding = new Thickness(24, 8, 24, 8),
            CornerRadius = new CornerRadius(6)
        };
        okButton.Click += (s, e) =>
        {
            if (_comboBox?.SelectedItem is string selected)
            {
                SelectedProfileName = selected;
                ResultConfirmed = true;
            }
            Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);

        panel.Children.Add(_messageBlock);
        panel.Children.Add(_comboBox);
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
        return border;
    }

    void UpdateContent()
    {
        _messageBlock!.Text = Message;
    }
}
