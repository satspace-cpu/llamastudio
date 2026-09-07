using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;

namespace LlamaStudio.Controls;

public class InputDialogWindow : Window
{
	public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<InputDialogWindow, string>("Message");

	public static readonly StyledProperty<string> DefaultValueProperty = AvaloniaProperty.Register<InputDialogWindow, string>("DefaultValue");

	private TextBlock? _messageBlock;

	private TextBox? _inputBox;

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

	public string DefaultValue
	{
		get
		{
			return GetValue(DefaultValueProperty);
		}
		set
		{
			SetValue(DefaultValueProperty, value);
		}
	}

	public string? ResultText { get; private set; }

	public bool ResultConfirmed { get; private set; }

	public InputDialogWindow()
	{
		base.Width = 450.0;
		base.Height = 200.0;
		base.MinWidth = 350.0;
		base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		base.SystemDecorations = SystemDecorations.None;
		base.Background = ThemeBrushes.Get("ThemeBackground", "#0F172A");
		base.Content = CreateDialogContent();
		this.GetObservable(MessageProperty).Subscribe(delegate
		{
			UpdateContent();
		});
		this.GetObservable(DefaultValueProperty).Subscribe(delegate
		{
			UpdateInput();
		});
		this.GetObservable(Window.TitleProperty).Subscribe(delegate
		{
		});
	}

	private Control CreateDialogContent()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0,
			Margin = new Thickness(20.0)
		};
		_messageBlock = new TextBlock
		{
			FontSize = 14.0,
			Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#E5E7EB"),
			TextWrapping = TextWrapping.Wrap
		};
		_inputBox = new TextBox
		{
			Background = ThemeBrushes.Get("ThemeInputBackground", "#0F172A"),
			Foreground = Brushes.White,
			BorderBrush = ThemeBrushes.Get("ThemeBorder", "#334155"),
			Padding = new Thickness(10.0, 8.0),
			CornerRadius = new CornerRadius(8.0)
		};
		StackPanel buttonRowPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		Button button = new Button
		{
			Content = "Cancel",
			Background = ThemeBrushes.Get("BtnSecondary", "#475569"),
			Foreground = Brushes.White,
			Padding = new Thickness(16.0, 8.0, 16.0, 8.0),
			CornerRadius = new CornerRadius(6.0)
		};
		button.Click += delegate
		{
			ResultConfirmed = false;
			Close();
		};
		Button okButton = new Button
		{
			Content = "OK",
			Background = ThemeBrushes.Get("BtnPrimary", "#3B82F6"),
			Foreground = Brushes.White,
			Padding = new Thickness(16.0, 8.0, 16.0, 8.0),
			CornerRadius = new CornerRadius(6.0)
		};
		okButton.Click += delegate
		{
			ResultText = _inputBox.Text;
			ResultConfirmed = true;
			Close();
		};
		buttonRowPanel.Children.Add(button);
		buttonRowPanel.Children.Add(okButton);
		stackPanel.Children.Add(_messageBlock);
		stackPanel.Children.Add(_inputBox);
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
		UpdateInput();
		return result;
	}

	private void UpdateContent()
	{
		_messageBlock.Text = Message;
	}

	private void UpdateInput()
	{
		_inputBox.Text = DefaultValue;
	}
}
