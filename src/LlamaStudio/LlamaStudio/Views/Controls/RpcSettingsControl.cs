using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Controls;

/// <summary>
/// Keeps the remote RPC endpoint as a first-class profile setting instead of a raw command line argument.
/// </summary>
public sealed class RpcSettingsControl : UserControl
{
    public RpcSettingsControl()
    {
        var title = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.DemiBold,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        title.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcTitle)));

        var description = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        description.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcDescription)));

        var sshTitle = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.DemiBold,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        sshTitle.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcSshTitle)));

        var sshHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { sshTitle }
        };

        var instruction = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        instruction.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcSshInstruction)));

        var enabled = new ToggleSwitch
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        enabled.Bind(ToggleSwitch.ContentProperty, new Binding(nameof(ServerViewModel.RpcEnabledLabel)));
        enabled.Bind(ToggleSwitch.IsCheckedProperty, new Binding(nameof(ServerViewModel.RpcEnabled))
        {
            Mode = BindingMode.TwoWay
        });
        var userToggleReady = false;
        enabled.IsCheckedChanged += async (_, _) =>
        {
            if (!userToggleReady || DataContext is not ServerViewModel viewModel)
            {
                return;
            }

            if (enabled.IsChecked == true)
            {
                await viewModel.StartRpcServerAsync();
            }
            else
            {
                await viewModel.StopRpcServerAsync();
            }
        };
        Dispatcher.UIThread.Post(() => userToggleReady = true);

        var useCache = new ToggleSwitch
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        useCache.Bind(ToggleSwitch.ContentProperty, new Binding(nameof(ServerViewModel.RpcUseCacheLabel)));
        useCache.Bind(ToggleSwitch.IsCheckedProperty, new Binding(nameof(ServerViewModel.RpcUseCache))
        {
            Mode = BindingMode.TwoWay
        });
        useCache.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));

        var hostLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        hostLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcHostLabel)));

        var host = new TextBox
        {
            Watermark = global::LlamaStudio.NativeLanguage.Text("например, 192.168.1.100", "for example, 192.168.1.100"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6)
        };
        host.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.RpcHost))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        host.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));

        var portLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        portLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcPortLabel)));

        var port = new TextBox
        {
            Watermark = "50052",
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6)
        };
        port.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.RpcPort))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        port.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));

        var usernameLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        usernameLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcSshUsernameLabel)));

        var username = new TextBox
        {
            Watermark = "server",
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6)
        };
        username.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.RpcSshUsername))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        username.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));

        var passwordLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        passwordLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcSshPasswordLabel)));

        var password = new TextBox
        {
            Watermark = "Password",
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6),
            PasswordChar = '*'
        };
        password.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.RpcSshPassword))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        password.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));
        password.LostFocus += (_, _) =>
        {
            if (DataContext is ServerViewModel viewModel && viewModel.RpcSavePassword)
            {
                viewModel.SaveRpcCredentialsToProfile();
            }
        };

        var showPassword = new CheckBox();
        showPassword.Bind(ContentControl.ContentProperty, new Binding(nameof(ServerViewModel.RpcShowPasswordLabel)));
        showPassword.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));
        showPassword.IsCheckedChanged += (_, _) =>
        {
            password.PasswordChar = showPassword.IsChecked == true ? '\0' : '*';
        };

        var savePassword = new CheckBox();
        savePassword.Bind(ContentControl.ContentProperty, new Binding(nameof(ServerViewModel.RpcSavePasswordLabel)));
        savePassword.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ServerViewModel.RpcSavePassword))
        {
            Mode = BindingMode.TwoWay
        });
        savePassword.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));
        savePassword.Click += (_, _) =>
        {
            // Binding updates RpcSavePassword after the focus/click sequence. Defer the
            // save so the new checkbox value is included and the password is not cleared.
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is ServerViewModel viewModel)
                {
                    viewModel.SaveRpcPasswordPreferenceToProfile();
                }
            });
        };

        var checkSshButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = ThemeBrushes.Get("ThemeAccent", "#5B4BFF"),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 8),
            CornerRadius = new CornerRadius(5)
        };
        checkSshButton.Bind(Button.ContentProperty, new Binding(nameof(ServerViewModel.RpcSshButtonLabel)));
        checkSshButton.Bind(Button.BackgroundProperty, new Binding(nameof(ServerViewModel.RpcSshButtonBrush)));
        checkSshButton.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));
        checkSshButton.Click += async (_, _) =>
        {
            if (DataContext is ServerViewModel viewModel)
            {
                await viewModel.CheckRpcSshAsync();
            }
        };

        var sshStatus = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextMuted", "#7F8AA3"),
            VerticalAlignment = VerticalAlignment.Center
        };
        sshStatus.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcSshStatus)));

        AttachedToVisualTree += async (_, _) =>
        {
            if (DataContext is ServerViewModel viewModel && viewModel.RpcEnabled)
            {
                await viewModel.CheckRpcSshAsync();
            }
        };

        var sshCredentials = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,12,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };
        sshCredentials.Children.Add(usernameLabel);
        Grid.SetColumn(usernameLabel, 0);
        Grid.SetRow(usernameLabel, 0);
        sshCredentials.Children.Add(passwordLabel);
        Grid.SetColumn(passwordLabel, 2);
        Grid.SetRow(passwordLabel, 0);
        sshCredentials.Children.Add(username);
        Grid.SetColumn(username, 0);
        Grid.SetRow(username, 1);
        sshCredentials.Children.Add(password);
        Grid.SetColumn(password, 2);
        Grid.SetRow(password, 1);

        var endpoint = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };
        endpoint.Children.Add(hostLabel);
        Grid.SetColumn(hostLabel, 0);
        Grid.SetRow(hostLabel, 0);
        endpoint.Children.Add(host);
        Grid.SetColumn(host, 0);
        Grid.SetRow(host, 1);

        var rpcPortSetting = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,220"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };
        rpcPortSetting.Children.Add(portLabel);
        Grid.SetColumn(portLabel, 1);
        Grid.SetRow(portLabel, 0);
        rpcPortSetting.Children.Add(port);
        Grid.SetColumn(port, 1);
        Grid.SetRow(port, 1);

        var startButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = ThemeBrushes.Get("ThemeSuccess", "#08A878"),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 8),
            CornerRadius = new CornerRadius(5)
        };
        startButton.Bind(Button.ContentProperty, new Binding(nameof(ServerViewModel.RpcStartLabel)));
        startButton.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcCanStart)));
        startButton.Click += async (_, _) =>
        {
            if (DataContext is ServerViewModel viewModel)
            {
                await viewModel.StartRpcServerAsync();
            }
        };

        var stopButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = ThemeBrushes.Get("ThemeDanger", "#E53935"),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 8),
            CornerRadius = new CornerRadius(5)
        };
        stopButton.Bind(Button.ContentProperty, new Binding(nameof(ServerViewModel.RpcStopLabel)));
        stopButton.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcCanStop)));
        stopButton.Click += async (_, _) =>
        {
            if (DataContext is ServerViewModel viewModel)
            {
                await viewModel.StopRpcServerAsync();
            }
        };

        var sshOptions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children = { showPassword, savePassword, checkSshButton, sshStatus }
        };

        var managementTitle = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.DemiBold,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB")
        };
        managementTitle.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcManagementTitle)));

        var serverPathLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeBrushes.Get("ThemeTextSecondary", "#A8AFBF")
        };
        serverPathLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcServerPathLabel)));

        var serverPathInstruction = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextMuted", "#7F8AA3")
        };
        serverPathInstruction.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcServerPathInstruction)));

        var serverPath = new TextBox
        {
            Watermark = @"C:\Users\<username>\AppData\Roaming\LlamaStudio\<build>\ggml-rpc-server.exe",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = ThemeBrushes.Get("ThemeInputBackground", "#111A2A"),
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#F4F7FB"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#3A4B68"),
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6)
        };
        serverPath.Bind(TextBox.TextProperty, new Binding(nameof(ServerViewModel.RpcServerPath))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        serverPath.Bind(IsEnabledProperty, new Binding(nameof(ServerViewModel.RpcEnabled)));

        var status = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.DemiBold,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#E8ECF4")
        };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(ServerViewModel.RpcConnectionStatus)));

        var sshCard = new Border
        {
            Padding = new Thickness(14),
            Background = ThemeBrushes.Get("ThemeSurface", "#1E293B"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#334155"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Child = new StackPanel
            {
                Spacing = 10,
                Children = { sshHeader, instruction, endpoint, sshCredentials, sshOptions }
            }
        };

        var managementCard = new Border
        {
            Padding = new Thickness(14),
            Background = ThemeBrushes.Get("ThemeSurface", "#1E293B"),
            BorderBrush = ThemeBrushes.Get("ThemeBorder", "#334155"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    managementTitle,
                    serverPathLabel,
                    serverPathInstruction,
                    serverPath,
                    rpcPortSetting,
                    enabled,
                    useCache,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { startButton, stopButton } },
                    status
                }
            }
        };

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 12,
                Margin = new Thickness(8),
                Children =
                {
                    title,
                    description,
                    sshCard,
                    managementCard
                }
            }
        };
    }
}
