using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;

namespace LlamaStudio.Controls;

public class MessageDialogWindow : Window
{
	public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<MessageDialogWindow, string>("Message");

	public static readonly StyledProperty<MessageBoxButton> ButtonTypeProperty = AvaloniaProperty.Register<MessageDialogWindow, MessageBoxButton>("ButtonType", MessageBoxButton.Ok);

	private TextBlock? _titleBlock;

	private TextBlock? _messageBlock;

	private Button? _cancelButton;

	private Button? _okButton;

	private static readonly FontFamily s_font = Avalonia.Media.FontFamily.Parse("Segoe UI, Arial, sans-serif");

	public string Message
	{
		get
		{
			return GetValue(MessageProperty);
		}
		set
		{
			SetValue(MessageProperty, value);
		}
	}

	public MessageBoxButton ButtonType
	{
		get
		{
			return GetValue(ButtonTypeProperty);
		}
		set
		{
			SetValue(ButtonTypeProperty, value);
		}
	}

	public MessageBoxButtonResult Result { get; private set; }

	public MessageDialogWindow()
	{
		base.Width = 520.0;
		base.Height = 340.0;
		base.MinWidth = 350.0;
		base.MinHeight = 230.0;
		base.MaxHeight = 620.0;
		base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		base.SystemDecorations = SystemDecorations.None;
		base.Background = ThemeBrushes.Get("ThemeBackground", "#0F172A");
		base.Content = CreateDialogContent();
		this.GetObservable(MessageProperty).Subscribe(delegate
		{
			UpdateContent();
		});
		this.GetObservable(ButtonTypeProperty).Subscribe(delegate
		{
			UpdateButtons();
		});
		this.GetObservable(Window.TitleProperty).Subscribe(delegate
		{
			UpdateContent();
		});
	}

	private Control CreateDialogContent()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0,
			Margin = new Thickness(20.0)
		};
		_titleBlock = new TextBlock
		{
			FontSize = 18.0,
			FontWeight = FontWeight.DemiBold,
			Foreground = Brushes.White,
			TextWrapping = TextWrapping.Wrap,
			FontFamily = s_font
		};
		_messageBlock = new TextBlock
		{
			FontSize = 14.0,
			Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#E5E7EB"),
			TextWrapping = TextWrapping.Wrap,
			FontFamily = s_font
		};
		ScrollViewer messageScroll = new ScrollViewer
		{
			Content = _messageBlock,
			MaxHeight = 390.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		StackPanel buttonRowPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_cancelButton = new Button
		{
			Content = "No",
			Background = ThemeBrushes.Get("BtnSecondary", "#475569"),
			Foreground = Brushes.White,
			Padding = new Thickness(16.0, 8.0, 16.0, 8.0),
			CornerRadius = new CornerRadius(6.0),
			IsVisible = false
		};
		_cancelButton.Click += delegate
		{
			Result = MessageBoxButtonResult.No;
			Close();
		};
		_okButton = new Button
		{
			Content = "OK",
			Background = ThemeBrushes.Get("BtnPrimary", "#3B82F6"),
			Foreground = Brushes.White,
			Padding = new Thickness(16.0, 8.0, 16.0, 8.0),
			CornerRadius = new CornerRadius(6.0)
		};
		_okButton.Click += delegate
		{
			Result = ((ButtonType != MessageBoxButton.YesNo) ? MessageBoxButtonResult.Ok : MessageBoxButtonResult.Yes);
			Close();
		};
		buttonRowPanel.Children.Add(_cancelButton);
		buttonRowPanel.Children.Add(_okButton);
		stackPanel.Children.Add(_titleBlock);
		stackPanel.Children.Add(messageScroll);
		stackPanel.Children.Add(buttonRowPanel);
		Border result = new Border
		{
			Background = ThemeBrushes.Get("ThemeSurface", "#1E293B"),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(20.0),
			BorderBrush = ThemeBrushes.Get("ThemeBorder", "#334155"),
			BorderThickness = new Thickness(1.0),
			Child = stackPanel
		};
		UpdateContent();
		UpdateButtons();
		return result;
	}

	private void UpdateContent()
	{
		_titleBlock.Text = base.Title;
		_messageBlock.Text = Message;
	}

	private void UpdateButtons()
	{
		if (ButtonType == MessageBoxButton.YesNo)
		{
			_cancelButton.IsVisible = true;
			_okButton.Content = "Yes";
		}
		else
		{
			_cancelButton.IsVisible = false;
			_okButton.Content = "OK";
		}
	}
}
