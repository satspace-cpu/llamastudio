using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Services;
using LlamaStudio.Infrastructure.Logging;
using LlamaStudio.Infrastructure.Llama;
using LlamaStudio.Infrastructure.Models;
using LlamaStudio.Infrastructure.Profiles;
using LlamaStudio.Infrastructure.Updater;
using LlamaStudio.Infrastructure.HuggingFace;
using LlamaStudio.Infrastructure.Chat;
using LlamaStudio.Infrastructure.Mcp;
using LlamaStudio.Services;
using LlamaStudio.ViewModels;
using LlamaStudio.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LlamaStudio;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static bool IsShuttingDown { get; private set; }
    static IHost? _host;
    TrayManager? _trayManager;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            var host = CreateHost();
            _host = host;
            Services = host.Services;

            // Load settings before creating any views/viewmodels
            var settings = Services.GetRequiredService<ISettings>();
            await settings.LoadAsync();

            // Apply saved theme to application
            ApplyTheme(settings.Theme);

            // Apply saved language to localization service
            var loc = Services.GetRequiredService<ILocalizationService>();
            if (!string.IsNullOrEmpty(settings.Language))
                loc.ChangeLanguage(settings.Language);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create host: {ex}");
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var mainWindow = Services!.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;

                _trayManager = Services.GetRequiredService<TrayManager>();
                _trayManager.Initialize();

                var appSettings = Services.GetRequiredService<ISettings>();

                // If StartMinimized is enabled, start hidden (in tray) — only show monitoring if enabled
                if (appSettings.StartMinimized)
                {
                    if (appSettings.StartMonitoringWindow)
                        ShowMonitoringWindow();
                }
                else
                {
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show window: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            desktop.Exit += (s, e) =>
            {
                IsShuttingDown = true;

                try { _trayManager?.Dispose(); } catch { }
                try
                {
                    // Ensure ViewModel settings are synced before saving
                    var settingsVm = Services.GetService<SettingsViewModel>();
                    settingsVm?.ApplySettingsToModel();
                    Services.GetService<ISettings>().Save();
                }
                catch { }
                // Never kill llama server on exit — leave it running
                try { _host?.StopAsync(); } catch { }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    static void ShowWindowFromTray()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
            if (desktop.MainWindow?.WindowState == WindowState.Minimized)
                desktop.MainWindow.WindowState = WindowState.Normal;
        }
    }

    static void ShowMonitoringWindow()
    {
        try
        {
            var vm = Services?.GetService(typeof(ViewModels.MonitoringViewModel)) as ViewModels.MonitoringViewModel;
            if (vm != null)
            {
                var win = new Views.MonitoringWindow(vm);
                win.Show();
            }
        }
        catch { }
    }

    static void RequestExitFromTray(bool force)
    {
        IsShuttingDown = true;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    public void ShowWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
            if (desktop.MainWindow?.WindowState == WindowState.Minimized)
                desktop.MainWindow.WindowState = WindowState.Normal;
        }
    }

    public void RequestExit(bool force)
    {
        IsShuttingDown = true;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var handler = new System.Net.Http.SocketsHttpHandler()
                {
                    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    },
                    // Optimize for large downloads
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 4
                };
                var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LlamaStudio/1.0");
                services.AddSingleton(httpClient);

                services.AddSingleton<ILocalizationService, LocalizationService>();
                services.AddSingleton<ISettings, AppSettings>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<ILlamaUpdater>(sp => new LlamaUpdater(
                        sp.GetRequiredService<HttpClient>(),
                        sp.GetRequiredService<ILogService>(),
                        sp.GetRequiredService<ISettings>(),
                        sp.GetRequiredService<ICliValidator>(),
                        sp.GetRequiredService<IHelpParser>()));
                services.AddSingleton<IServerManager, ServerManager>();
                services.AddSingleton<IModelScanner, ModelScanner>();
                services.AddSingleton<IProfileManager, ProfileManager>();
                services.AddSingleton<IHelpParser, HelpParser>();
                services.AddSingleton<ICliValidator, CliValidator>();
                services.AddSingleton<IHuggingFaceDownloader>(sp => new HuggingFaceDownloader(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ILogService>()));
                services.AddSingleton<IGpuMonitor, GpuMonitorService>();
                services.AddSingleton<IChatService, ChatService>();
                services.AddSingleton<IMcpToolsService, McpToolsService>(sp => new McpToolsService(
                        sp.GetRequiredService<ILogService>(),
                        sp.GetRequiredService<ISettings>()));
                services.AddSingleton<ChatSessionStore>();

                services.AddSingleton<INavigationService>(sp =>
                    new NavigationService(() => sp.GetRequiredService<MainViewModel>()));

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ModelsViewModel>();
                services.AddSingleton<ServerViewModel>();
                services.AddSingleton<ProfilesViewModel>();
                services.AddSingleton<LogsViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ApiTestViewModel>();
                services.AddSingleton<ChatViewModel>();
                services.AddSingleton<MonitoringViewModel>();
                services.AddSingleton<LlamaReleasesViewModel>();
                services.AddSingleton<SupportViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<IDialogService>(sp =>
                    new DialogService(() => sp.GetRequiredService<MainWindow>(), sp.GetRequiredService<ILogService>()));

                services.AddSingleton<IFilePickerService>(sp => new FilePickerService(() => sp.GetRequiredService<MainWindow>()));

                services.AddSingleton<TrayManager>(sp =>
                    new TrayManager(
                        sp.GetRequiredService<ISettings>(),
                        sp.GetRequiredService<IServerManager>(),
                        sp.GetRequiredService<IProfileManager>(),
                        sp.GetRequiredService<ILocalizationService>(),
                        sp.GetRequiredService<ILogService>(),
                        () => ShowWindowFromTray(),
                        force => RequestExitFromTray(force)));
            })
            .Build();
    }

    static void ApplyTheme(string? theme)
    {
        var variant = theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => DetectSystemThemeVariant() // System — detect actual preference
        };

        if (Current is App app)
        {
            app.RequestedThemeVariant = variant;

            // Apply theme colors to resources
            var lightMode = variant == Avalonia.Styling.ThemeVariant.Light;
            ApplyThemeColors(app, lightMode);
        }
    }

    static Avalonia.Styling.ThemeVariant DetectSystemThemeVariant()
    {
        try
        {
            // Check Windows registry for actual system theme preference
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value != null)
            {
                return int.TryParse(value.ToString(), out var isLight) && isLight == 1
                    ? Avalonia.Styling.ThemeVariant.Light
                    : Avalonia.Styling.ThemeVariant.Dark;
            }
        }
        catch { }

        // Default to Light if we can't detect
        return Avalonia.Styling.ThemeVariant.Light;
    }

    static void ApplyThemeColors(App app, bool lightMode)
    {
        if (lightMode)
        {
            // Light theme: OpenCode style — soft gray sidebar, light content area, white cards with borders
            app.Resources["ThemeBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F5F5F5"));
            app.Resources["ThemeSurface"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FAFAFA"));
            app.Resources["ThemeCard"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["ThemeTextPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            app.Resources["ThemeTextSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#656D76"));
            app.Resources["ThemeTextTertiary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B949E"));
            app.Resources["ThemeBorder"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D0D7DE"));
            app.Resources["ThemeBorderStrong"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#C9D1D9"));
            app.Resources["ThemeInputBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["ThemeAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["ThemeAccentHover"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1A7FCA"));
            // Dashboard card backgrounds — all white with colored accents
            app.Resources["CardServerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardModelsBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardProfilesBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardVersionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["CardSectionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            // Card accent colors (borders, labels)
            app.Resources["CardServerAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            // Card label text colors
            app.Resources["CardServerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            // Sub-text inside cards
            app.Resources["CardSubText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280"));
            // GPU monitor section
            app.Resources["GpuMonitorBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["GpuMonitorInner"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["GpuMonitorHeader"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            // Progress bar tracks
            app.Resources["ProgressTrack"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E1E4E8"));
            app.Resources["ProgressVram"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["ProgressCore"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["ProgressPower"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["ProgressFan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["ProgressRam"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            // Sidebar — soft gray like OpenCode
            app.Resources["SidebarBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F0F0F0"));
            app.Resources["SidebarItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["SidebarItemSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D4D4D4"));
            app.Resources["SidebarItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#57606A"));
            app.Resources["SidebarItemTextSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            // TabItem
            app.Resources["TabItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["TabItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#57606A"));
            app.Resources["TabItemSelectedBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["TabItemSelectedText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["TabItemHoverBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D4D4D4"));
            // Profile section inner
            app.Resources["ProfileInnerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            app.Resources["ProfileModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F6F8FA"));
            // Badges
            app.Resources["BadgeMtp"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BadgeReasoning"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BadgeFlash"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2563EB"));
            app.Resources["BadgeCache"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D97706"));
            app.Resources["BadgeBatching"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
            app.Resources["BadgeText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            // Server page
            app.Resources["ServerBannerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            app.Resources["ServerBannerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["ServerBannerSub"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280"));
            app.Resources["SectionTitle"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F2328"));
            app.Resources["SectionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#393F46"));
            app.Resources["SectionLabelLight"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#656D76"));
            // Buttons — subtle, matching OpenCode style
            app.Resources["BtnPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2F81F7"));
            app.Resources["BtnSuccess"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2DA44E"));
            app.Resources["BtnDanger"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CF222E"));
            app.Resources["BtnSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8E8E8"));
            app.Resources["BtnPurple"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B54FF"));
            app.Resources["BtnCyan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
        }
        else
        {
            // Dark theme: deep slates, contrast cards
            app.Resources["ThemeBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeSurface"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B"));
            app.Resources["ThemeCard"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeTextPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["ThemeTextSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["ThemeTextTertiary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            app.Resources["ThemeBorder"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155"));
            app.Resources["ThemeBorderStrong"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            app.Resources["ThemeInputBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["ThemeAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["ThemeAccentHover"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4F46E5"));
            // Dashboard cards — dark colored backgrounds
            app.Resources["CardServerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1B4B"));
            app.Resources["CardModelsBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#064E3B"));
            app.Resources["CardProfilesBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C2D12"));
            app.Resources["CardVersionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E3A5F"));
            app.Resources["CardModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#172554"));
            app.Resources["CardSectionBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F1533"));
            // Card accent colors
            app.Resources["CardServerAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["CardModelsAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["CardProfilesAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["CardVersionAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["CardModelAccent"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            // Card label text colors
            app.Resources["CardServerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#818CF8"));
            app.Resources["CardModelsLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#34D399"));
            app.Resources["CardProfilesLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FDBA74"));
            app.Resources["CardVersionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#60A5FA"));
            app.Resources["CardModelLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#60A5FA"));
            // Sub-text
            app.Resources["CardSubText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A5B4FC"));
            // GPU monitor
            app.Resources["GpuMonitorBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1F1533"));
            app.Resources["GpuMonitorInner"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["GpuMonitorHeader"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A78BFA"));
            // Progress
            app.Resources["ProgressTrack"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["ProgressVram"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));
            app.Resources["ProgressCore"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            app.Resources["ProgressPower"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F59E0B"));
            app.Resources["ProgressFan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CF6"));
            app.Resources["ProgressRam"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981"));
            // Sidebar
            app.Resources["SidebarBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"));
            app.Resources["SidebarItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B"));
            app.Resources["SidebarItemSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#27272A"));
            app.Resources["SidebarItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["SidebarItemTextSelected"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            // TabItem
            app.Resources["TabItemBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155"));
            app.Resources["TabItemText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));
            app.Resources["TabItemSelectedBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366F1"));
            app.Resources["TabItemSelectedText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["TabItemHoverBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            // Profile section inner
            app.Resources["ProfileInnerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            app.Resources["ProfileModelBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2D1B69"));
            // Badges
            app.Resources["BadgeMtp"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BadgeReasoning"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BadgeFlash"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2563EB"));
            app.Resources["BadgeCache"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D97706"));
            app.Resources["BadgeBatching"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
            app.Resources["BadgeText"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            // Server page
            app.Resources["ServerBannerBackground"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1B4B"));
            app.Resources["ServerBannerLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#818CF8"));
            app.Resources["ServerBannerSub"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            app.Resources["SectionTitle"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            app.Resources["SectionLabel"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E2E8F0"));
            app.Resources["SectionLabelLight"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            // Buttons
            app.Resources["BtnPrimary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4338CA"));
            app.Resources["BtnSuccess"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#059669"));
            app.Resources["BtnDanger"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DC2626"));
            app.Resources["BtnSecondary"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            app.Resources["BtnPurple"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            app.Resources["BtnCyan"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0891B2"));
        }
    }
}