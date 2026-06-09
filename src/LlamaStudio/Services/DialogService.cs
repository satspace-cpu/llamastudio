  using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Thickness = Avalonia.Thickness;
using CornerRadius = Avalonia.CornerRadius;
using LlamaStudio.Controls;
using LlamaStudio.Core.Interfaces;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace LlamaStudio.Services;

public class DialogService : IDialogService
{
    readonly Func<Window> _windowFactory;
    readonly ILogService _log;
    Window? _window;

    public DialogService(Func<Window> windowFactory, ILogService log)
    {
        _windowFactory = windowFactory;
        _log = log;
    }

    Window Window => _window ??= _windowFactory();

    public async Task<string?> SelectFolderAsync(string title, string? initialPath = null)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // Use native Windows Shell IFileOpenDialog with FOS_PICKFOLDERS
                // This shows the standard Windows folder picker dialog
                var hwnd = IntPtr.Zero;
                var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
                dialog.GetOptions(out var options);
                options |= FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_NOVALIDATE;
                dialog.SetOptions(options);

                dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    var folder = ShellItem.CreateFromPath(initialPath);
                    dialog.SetFolder(folder);
                }

                var hr = dialog.Show(hwnd);
                if (hr == 0)
                {
                    dialog.GetResult(out var result);
                    result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    return path;
                }
                return null;
            }
            catch (Exception ex)
            {
                _log.Warning($"Native folder picker failed, using fallback: {ex.Message}", "DialogService");
            }
        }

        var folderOptions = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
        {
            folderOptions.SuggestedStartLocation = await Window.StorageProvider.TryGetFolderFromPathAsync(initialPath);
        }

        var folders = await Window.StorageProvider.OpenFolderPickerAsync(folderOptions);
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    public async Task<string?> SelectFileAsync(string title, string? initialPath = null, string? filter = null)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrEmpty(filter))
        {
            var parts = filter.Split('|');
            if (parts.Length >= 2)
            {
                var pattern = parts[1].Trim();
                options.FileTypeFilter = new List<Avalonia.Platform.Storage.FilePickerFileType>
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(parts[0].Trim())
                    {
                        Patterns = new[] { pattern },
                    }
                };
            }
        }

        if (!string.IsNullOrEmpty(initialPath))
        {
            var dir = System.IO.Path.GetDirectoryName(initialPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                options.SuggestedStartLocation = await Window.StorageProvider.TryGetFolderFromPathAsync(dir);
            }
        }

        var files = await Window.StorageProvider.OpenFilePickerAsync(options);
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveFileAsync(string title, string? initialPath = null, string? filter = null)
    {
        var file = await Window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
        });

        return file?.Path.LocalPath;
    }

    public async Task ShowMessageAsync(string message, string? title = null)
    {
        var dialog = new MessageDialogWindow
        {
            Title = title ?? "Information",
            Message = message,
            ButtonType = MessageBoxButton.Ok
        };
        await dialog.ShowDialog(Window);
    }

    public async Task<bool> ShowConfirmationAsync(string message, string? title = null)
    {
        var dialog = new MessageDialogWindow
        {
            Title = title ?? "Confirmation",
            Message = message,
            ButtonType = MessageBoxButton.YesNo
        };
        await dialog.ShowDialog(Window);
        return dialog.Result == MessageBoxButtonResult.Yes;
    }

    public async Task ShowErrorAsync(string message, string? title = null)
    {
        var win = new Window
        {
            Title = title ?? "Error",
            Width = 480,
            Height = 280,
            MinWidth = 350,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            SystemDecorations = Avalonia.Controls.SystemDecorations.None,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A")),
        };

        var border = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B")),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = title ?? "Error",
                        FontSize = 18,
                        FontWeight = (Avalonia.Media.FontWeight)600,
                        Foreground = Avalonia.Media.Brushes.White,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 14,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E5E7EB")),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        MaxHeight = 200,
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            new Button
                            {
                                Content = "OK",
                                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6")),
                                Foreground = Avalonia.Media.Brushes.White,
                                Padding = new Thickness(24, 8, 24, 8),
                                CornerRadius = new CornerRadius(6)
                            }
                        }
                    }
                }
            }
        };

        var okBtn = ((StackPanel)((StackPanel)border.Child).Children[2]).Children[0] as Button;
        okBtn!.Click += (s, e) => win.Close();

        win.Content = border;
        await win.ShowDialog(Window);
    }

    public async Task ShowSuccessAsync(string message, string? title = null)
    {
        var dialog = new MessageDialogWindow
        {
            Title = title ?? "Success",
            Message = message,
            ButtonType = MessageBoxButton.Ok
        };
        await dialog.ShowDialog(Window);
    }

    public async Task ShowInfoAsync(string message, string? title = null, bool isLongText = false)
    {
        if (isLongText)
        {
            var win = new Window
            {
                Title = title ?? "Information",
                Width = 650,
                Height = 550,
                MinWidth = 450,
                MinHeight = 350,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                SystemDecorations = Avalonia.Controls.SystemDecorations.BorderOnly,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"))
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto }
                },
                Margin = new Thickness(16)
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            };
            Grid.SetRow(scroll, 0);

            var textBlock = new TextBlock
            {
                Text = message,
                FontFamily = Avalonia.Media.FontFamily.Parse("Segoe UI, Arial"),
                FontSize = 12,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E5E7EB")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            };

            scroll.Content = textBlock;

            var btnPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 1);

            var btn = new Button
            {
                Content = "Закрыть",
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6")),
                Foreground = Avalonia.Media.Brushes.White,
                Padding = new Thickness(24, 8, 24, 8),
                CornerRadius = new CornerRadius(6)
            };
            btn.Click += (s, e) => win.Close();
            btnPanel.Children.Add(btn);

            grid.Children.Add(scroll);
            grid.Children.Add(btnPanel);
            win.Content = grid;
            await win.ShowDialog(Window);
        }
        else
        {
            var dialog = new MessageDialogWindow
            {
                Title = title ?? "Information",
                Message = message,
                ButtonType = MessageBoxButton.Ok
            };
            await dialog.ShowDialog(Window);
        }
    }

    public async Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        var dialog = new InputDialogWindow
        {
            Title = title,
            Message = message,
            DefaultValue = defaultValue
        };
        await dialog.ShowDialog(Window);
        return dialog.ResultConfirmed ? dialog.ResultText : null;
    }

    public async Task<string?> ShowProfileSelectAsync(string title, string message, IEnumerable<string> profiles)
    {
        var dialog = new ProfileSelectDialogWindow
        {
            Title = title,
            Message = message
        };
        dialog.SetItems(profiles);
        await dialog.ShowDialog(Window);
        return dialog.ResultConfirmed ? dialog.SelectedProfileName : null;
    }

    public IDisposable? ShowLoading(string message, string? title = null)
    {
        var win = new Window
        {
            Title = title ?? "Загрузка",
            Width = 380,
            Height = 160,
            MinWidth = 280,
            CanResize = false,
            SystemDecorations = Avalonia.Controls.SystemDecorations.None,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A")),
        };

        win.Content = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E293B")),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24, 20),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = title ?? "Загрузка",
                        FontSize = 16,
                        FontWeight = (Avalonia.Media.FontWeight)600,
                        Foreground = Avalonia.Media.Brushes.White,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new StackPanel { Spacing = 8, Children =
                        {
                            // Spinner placeholder — animated border
                            new Border
                            {
                                Width = 24, Height = 24,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                CornerRadius = new CornerRadius(12),
                                BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6")),
                                BorderThickness = new Thickness(3),
                            },
                            new TextBlock
                            {
                                Text = message,
                                FontSize = 13,
                                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8")),
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            }
                        }
                    },
                }
            }
        };

        win.Show(Window);
        return new LoadingDialogDisposable(win);
    }

    class LoadingDialogDisposable : IDisposable
    {
        readonly Window _win;
        public LoadingDialogDisposable(Window win) => _win = win;
        public void Dispose() => _win.Close();
    }
}

// --- Windows Shell COM interfaces for native folder picker ---

static class ShellItem
{
    public static IShellItem CreateFromPath(string path)
    {
        var iid = new Guid("43826D1E-E718-42EE-BC55-A8E261BC376A");
        var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item);
        if (hr != 0)
            throw new System.ComponentModel.Win32Exception((int)hr);
        return item;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv);
}

[ComImport]
[Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
internal class FileOpenDialogRCW { }

[ComImport]
[Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    [PreserveSig] int Show(IntPtr hwndOwner);
    void SetFileTypes();
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise();
    void Unadvise();
    void SetOptions(FOS fos);
    void GetOptions(out FOS pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, int alignment);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid();
    void ClearClientData();
    void SetFilter();
}

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A8E261BC376A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler();
    void GetParent();
    void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes();
    void Compare();
}

[Flags]
internal enum FOS : uint
{
    FOS_OVERWRITEPROMPT = 0x2,
    FOS_STRICTFILETYPES = 0x4,
    FOS_NOCHANGEDIR = 0x8,
    FOS_PICKFOLDERS = 0x20,
    FOS_FORCEFILESYSTEM = 0x40,
    FOS_ALLNONSTORAGEITEMS = 0x80,
    FOS_NOVALIDATE = 0x100,
    FOS_ALLOWMULTISELECT = 0x200,
    FOS_PATHMUSTEXIST = 0x800,
    FOS_FILEMUSTEXIST = 0x1000,
    FOS_PICKITEMS = 0x20000,
    FOS_FORCEPREVIEWPANEON = 0x40000,
}

internal enum SIGDN : uint
{
    SIGDN_NORMALDISPLAY = 0x00000000,
    SIGDN_PARENTRELATIVEPARSING = 0x80018001,
    SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
    SIGDN_PARENTRELATIVEEDITING = 0x80031001,
    SIGDN_DESKTOPABSOLUTEEDITING = 0x8004C000,
    SIGDN_FILESYSPATH = 0x80058000,
    SIGDN_URL = 0x80068000,
    SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007C001,
    SIGDN_PARENTRELATIVE = 0x80080001,
}
