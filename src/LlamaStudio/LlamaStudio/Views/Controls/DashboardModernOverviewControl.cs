using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;

namespace LlamaStudio.Views.Controls;

public sealed class DashboardModernOverviewControl : UserControl
{
    private static readonly IBrush Surface = ThemeBrushes.Get("ThemeSurface", "#1E293B");
    private static readonly IBrush Input = ThemeBrushes.Get("ThemeInputBackground", "#0F172A");
    private static readonly IBrush Primary = ThemeBrushes.Get("BtnPrimary", "#6366F1");
    private static readonly IBrush Success = ThemeBrushes.Get("BtnSuccess", "#10B981");
    private static readonly IBrush Text = ThemeBrushes.Get("ThemeTextPrimary", "#E5E7EB");
    private static readonly IBrush Muted = ThemeBrushes.Get("ThemeTextSecondary", "#94A3B8");
    private static readonly IBrush Border = ThemeBrushes.Get("ThemeBorder", "#334155");

    public DashboardModernOverviewControl()
    {
        Content = Build();
    }

    private Control Build()
    {
        var root = new StackPanel { Spacing = 16, Margin = new Thickness(20) };
        root.Children.Add(new TextBlock
        {
            Text = "Llama Studio",
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = Text
        });
        root.Children.Add(new TextBlock
        {
            Text = "A local llama.cpp manager for models, profiles, servers and multi-GPU monitoring.",
            FontSize = 14,
            Foreground = Muted,
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(new TextBlock
        {
            Text = "Localization: English and Russian",
            FontSize = 12,
            Foreground = Muted
        });

        var cards = new UniformGrid { Columns = 4, Rows = 1 };
        cards.Children.Add(Card("Server", "ServerStatus.State", "server", new[] { "ServerRunning", "ServerStopped" }));
        cards.Children.Add(Card("Active model", "SelectedModel", "model"));
        cards.Children.Add(Card("Profiles", "ProfilesSavedText", "profiles"));
        cards.Children.Add(Card("llama.cpp", "ActiveVersionDisplay", "version"));
        root.Children.Add(cards);

        var profile = new Border
        {
            Background = Surface,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16)
        };
        var profileStack = new StackPanel { Spacing = 10 };
        profileStack.Children.Add(Label("Active profile"));
        var profileRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var profileCombo = new ComboBox
        {
            Width = 280,
            MinHeight = 36,
            Background = Input,
            Foreground = Text,
            BorderBrush = Border,
            Padding = new Thickness(10, 7)
        };
        profileCombo.Bind(ItemsControl.ItemsSourceProperty, new Binding("AllProfiles"));
        profileCombo.Bind(SelectingItemsControl.SelectedItemProperty, new Binding("SelectedProfile"));
        profileCombo.DataTemplates.Add(new FuncDataTemplate<object>((item, _) => new TextBlock { Text = item?.ToString() ?? "" }));
        profileRow.Children.Add(profileCombo);
        profileRow.Children.Add(Action("Set default", "SetDefaultProfileCommand", Primary));
        profileStack.Children.Add(profileRow);
        profileStack.Children.Add(BoundText("ActiveProfileLabel", Muted, 12));
        profileStack.Children.Add(BoundText("ActiveProfileName", Text, 16));
        profile.Child = profileStack;
        root.Children.Add(profile);

        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = 150, ItemHeight = 38 };
        actions.Children.Add(Action("Start server", "StartServerCommand", Success));
        actions.Children.Add(Action("Stop server", "StopServerCommand", ThemeBrushes.Get("BtnDanger", "#EF4444")));
        actions.Children.Add(Action("Open models", "NavigateCommand", Primary, "Models"));
        actions.Children.Add(Action("Open monitoring", "NavigateCommand", Primary, "Monitoring"));
        actions.Children.Add(Action("Check updates", "CheckVersionCommand", Primary));
        actions.Children.Add(Action("Update application", "UpdateAppCommand", Success));
        root.Children.Add(actions);

        var update = new Border
        {
            Background = Input,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16)
        };
        var updateStack = new StackPanel { Spacing = 8 };
        updateStack.Children.Add(Label("Application updates"));
        updateStack.Children.Add(BoundText("AppUpdateStatus", Muted, 13));
        updateStack.Children.Add(BoundText("AppVersionLabel", Muted, 12));
        updateStack.Children.Add(BoundText("AppVersion", Text, 15));
        update.Child = updateStack;
        root.Children.Add(update);

        return root;
    }

    private Border Card(string title, string bindingPath, string accent, string[]? visibility = null)
    {
        var stack = new StackPanel { Spacing = 7 };
        stack.Children.Add(new TextBlock { Text = title, Foreground = Muted, FontSize = 12 });
        stack.Children.Add(BoundText(bindingPath, Text, 18));
        return new Border
        {
            Background = Surface,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(4),
            Child = stack
        };
    }

    private TextBlock Label(string value) => new() { Text = value, Foreground = Text, FontSize = 14, FontWeight = FontWeight.DemiBold };

    private TextBlock BoundText(string path, IBrush brush, double size)
    {
        var text = new TextBlock { Foreground = brush, FontSize = size, TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.TextProperty, new Binding(path));
        return text;
    }

    private Button Action(string text, string command, IBrush background, object? parameter = null)
    {
        var button = new Button
        {
            Content = text,
            Background = background,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Bind(Button.CommandProperty, new Binding(command));
        if (parameter != null)
        {
            button.CommandParameter = parameter;
        }
        return button;
    }
}
