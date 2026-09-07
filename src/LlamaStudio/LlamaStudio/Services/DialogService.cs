using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using LlamaStudio.Controls;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Services;

public class DialogService : IDialogService
{
	private class LoadingDialogDisposable : IDisposable
	{
		private readonly Window _win;

		public LoadingDialogDisposable(Window win)
		{
			_win = win;
		}

		public void Dispose()
		{
			_win.Close();
		}
	}

	private readonly Func<Window> _windowFactory;

	private readonly ILogService _log;

	private readonly ILocalizationService? _loc;

	private Window? _window;

	private Window Window => _window ?? (_window = _windowFactory());

	public DialogService(Func<Window> windowFactory, ILogService log, ILocalizationService? loc = null)
	{
		_windowFactory = windowFactory;
		_log = log;
		_loc = loc;
	}

	public async Task<string?> SelectFolderAsync(string title, string? initialPath = null)
	{
		if (OperatingSystem.IsWindows())
		{
			try
			{
				nint zero = IntPtr.Zero;
				IFileOpenDialog fileOpenDialog = (IFileOpenDialog)new FileOpenDialogRCW();
				fileOpenDialog.GetOptions(out var pfos);
				pfos |= FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_NOVALIDATE;
				fileOpenDialog.SetOptions(pfos);
				fileOpenDialog.SetTitle(title);
				if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
				{
					IShellItem folder = ShellItem.CreateFromPath(initialPath);
					fileOpenDialog.SetFolder(folder);
				}
				if (fileOpenDialog.Show(zero) == 0)
				{
					fileOpenDialog.GetResult(out IShellItem ppsi);
					ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string ppszName);
					return ppszName;
				}
				return null;
			}
			catch (Exception ex)
			{
				_log.Warning("Native folder picker failed, using fallback: " + ex.Message, "DialogService");
			}
		}
		FolderPickerOpenOptions folderOptions = new FolderPickerOpenOptions
		{
			Title = title,
			AllowMultiple = false
		};
		if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
		{
			FolderPickerOpenOptions folderPickerOpenOptions = folderOptions;
			folderPickerOpenOptions.SuggestedStartLocation = await Window.StorageProvider.TryGetFolderFromPathAsync(initialPath);
		}
		IReadOnlyList<IStorageFolder> readOnlyList = await Window.StorageProvider.OpenFolderPickerAsync(folderOptions);
		return (readOnlyList.Count > 0) ? readOnlyList[0].Path.LocalPath : null;
	}

	public async Task<string?> SelectFileAsync(string title, string? initialPath = null, string? filter = null)
	{
		FilePickerOpenOptions options = new FilePickerOpenOptions
		{
			Title = title,
			AllowMultiple = false
		};
		if (!string.IsNullOrEmpty(filter))
		{
			string[] array = filter.Split('|');
			if (array.Length >= 2)
			{
				string text = array[1].Trim();
				options.FileTypeFilter = new List<FilePickerFileType>
				{
					new FilePickerFileType(array[0].Trim())
					{
						Patterns = new string[1] { text }
					}
				};
			}
		}
		if (!string.IsNullOrEmpty(initialPath))
		{
			string directoryName = Path.GetDirectoryName(initialPath);
			if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
			{
				FilePickerOpenOptions filePickerOpenOptions = options;
				filePickerOpenOptions.SuggestedStartLocation = await Window.StorageProvider.TryGetFolderFromPathAsync(directoryName);
			}
		}
		IReadOnlyList<IStorageFile> readOnlyList = await Window.StorageProvider.OpenFilePickerAsync(options);
		return (readOnlyList.Count > 0) ? readOnlyList[0].Path.LocalPath : null;
	}

	public async Task<string?> SaveFileAsync(string title, string? initialPath = null, string? filter = null)
	{
		string text = (await Window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = title
		}))?.Path.LocalPath;
		if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(initialPath))
		{
			string extension = Path.GetExtension(initialPath);
			if (!string.IsNullOrEmpty(extension) && string.IsNullOrEmpty(Path.GetExtension(text)))
			{
				text += extension;
			}
		}
		return text;
	}

	public async Task ShowMessageAsync(string message, string? title = null)
	{
		await new MessageDialogWindow
		{
			Title = (title ?? _loc?.T("dialog.information")),
			Message = message,
			ButtonType = MessageBoxButton.Ok
		}.ShowDialog(Window);
	}

	public async Task<bool> ShowConfirmationAsync(string message, string? title = null)
	{
		MessageDialogWindow dialog = new MessageDialogWindow
		{
			Title = (title ?? _loc?.T("dialog.confirmation")),
			Message = message,
			ButtonType = MessageBoxButton.YesNo
		};
		await dialog.ShowDialog(Window);
		return dialog.Result == MessageBoxButtonResult.Yes;
	}

	public async Task ShowErrorAsync(string message, string? title = null)
	{
		Window win = new Window
		{
			Title = (title ?? _loc?.T("dialog.error")),
			Width = 480.0,
			Height = 280.0,
			MinWidth = 350.0,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			SystemDecorations = SystemDecorations.None,
			Background = new SolidColorBrush(Color.Parse("#0F172A"))
		};
		Border border = new Border
		{
			Background = new SolidColorBrush(Color.Parse("#1E293B")),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(20.0),
			BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
			BorderThickness = new Thickness(1.0),
			Child = new StackPanel
			{
				Spacing = 16.0,
				Children = 
				{
					(Control)new TextBlock
					{
						Text = (title ?? _loc?.T("dialog.error")),
						FontSize = 18.0,
						FontWeight = FontWeight.DemiBold,
						Foreground = Brushes.White,
						TextWrapping = TextWrapping.Wrap
					},
					(Control)new TextBlock
					{
						Text = message,
						FontSize = 14.0,
						Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
						TextWrapping = TextWrapping.Wrap,
						MaxHeight = 200.0
					},
					(Control)new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Children = { (Control)new Button
						{
							Content = _loc?.T("dialog.ok"),
							Background = new SolidColorBrush(Color.Parse("#3B82F6")),
							Foreground = Brushes.White,
							Padding = new Thickness(24.0, 8.0, 24.0, 8.0),
							CornerRadius = new CornerRadius(6.0)
						} }
					}
				}
			}
		};
		(((StackPanel)((StackPanel)border.Child).Children[2]).Children[0] as Button).Click += delegate
		{
			win.Close();
		};
		win.Content = border;
		await win.ShowDialog(Window);
	}

	public async Task ShowSuccessAsync(string message, string? title = null)
	{
		await new MessageDialogWindow
		{
			Title = (title ?? _loc?.T("dialog.success")),
			Message = message,
			ButtonType = MessageBoxButton.Ok
		}.ShowDialog(Window);
	}

	public async Task ShowInfoAsync(string message, string? title = null, bool isLongText = false)
	{
		if (isLongText)
		{
			Window win = new Window
			{
				Title = (title ?? _loc?.T("dialog.information")),
				Width = 650.0,
				Height = 550.0,
				MinWidth = 450.0,
				MinHeight = 350.0,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				SystemDecorations = SystemDecorations.BorderOnly,
				Background = new SolidColorBrush(Color.Parse("#0F172A"))
			};
			Grid grid = new Grid
			{
				RowDefinitions = 
				{
					new RowDefinition
					{
						Height = GridLength.Star
					},
					new RowDefinition
					{
						Height = GridLength.Auto
					}
				},
				Margin = new Thickness(16.0)
			};
			ScrollViewer scrollViewer = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
			};
			Grid.SetRow(scrollViewer, 0);
			TextBlock content = new TextBlock
			{
				Text = message,
				FontFamily = FontFamily.Parse("Segoe UI, Arial"),
				FontSize = 12.0,
				Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
				TextWrapping = TextWrapping.Wrap
			};
			scrollViewer.Content = content;
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
			};
			Grid.SetRow(stackPanel, 1);
			Button button = new Button
			{
				Content = (_loc?.T("dialog.close_btn") ?? "Close"),
				Background = new SolidColorBrush(Color.Parse("#3B82F6")),
				Foreground = Brushes.White,
				Padding = new Thickness(24.0, 8.0, 24.0, 8.0),
				CornerRadius = new CornerRadius(6.0)
			};
			button.Click += delegate
			{
				win.Close();
			};
			stackPanel.Children.Add(button);
			grid.Children.Add(scrollViewer);
			grid.Children.Add(stackPanel);
			win.Content = grid;
			await win.ShowDialog(Window);
		}
		else
		{
			await new MessageDialogWindow
			{
				Title = (title ?? _loc?.T("dialog.information")),
				Message = message,
				ButtonType = MessageBoxButton.Ok
			}.ShowDialog(Window);
		}
	}

	public async Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
	{
		InputDialogWindow dialog = new InputDialogWindow
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
		ProfileSelectDialogWindow dialog = new ProfileSelectDialogWindow
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
		Window window = new Window();
		window.Title = title ?? _loc?.T("common.loading") ?? "Loading";
		window.Width = 380.0;
		window.Height = 160.0;
		window.MinWidth = 280.0;
		window.CanResize = false;
		window.SystemDecorations = SystemDecorations.None;
		window.Background = new SolidColorBrush(Color.Parse("#0F172A"));
		window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		window.Content = new Border
		{
			Background = new SolidColorBrush(Color.Parse("#1E293B")),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(24.0, 20.0),
			BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
			BorderThickness = new Thickness(1.0),
			Child = new StackPanel
			{
				Spacing = 16.0,
				Children = 
				{
					(Control)new TextBlock
					{
						Text = (title ?? _loc?.T("common.loading") ?? "Loading"),
						FontSize = 16.0,
						FontWeight = FontWeight.DemiBold,
						Foreground = Brushes.White,
						TextWrapping = TextWrapping.Wrap
					},
					(Control)new StackPanel
					{
						Spacing = 8.0,
						Children = 
						{
							(Control)new Border
							{
								Width = 24.0,
								Height = 24.0,
								HorizontalAlignment = HorizontalAlignment.Center,
								CornerRadius = new CornerRadius(12.0),
								BorderBrush = new SolidColorBrush(Color.Parse("#3B82F6")),
								BorderThickness = new Thickness(3.0)
							},
							(Control)new TextBlock
							{
								Text = message,
								FontSize = 13.0,
								Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
								TextWrapping = TextWrapping.Wrap,
								HorizontalAlignment = HorizontalAlignment.Center
							}
						}
					}
				}
			}
		};
		window.Show(Window);
		return new LoadingDialogDisposable(window);
	}
}
