using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LlamaStudio;

namespace LlamaStudio.Controls;

public class ProfileSelectDialogWindow : Window
{
	public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<ProfileSelectDialogWindow, string>("Message");

	private TextBlock? _messageBlock;

	private ComboBox? _comboBox;

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

	public string? SelectedProfileName { get; private set; }

	public bool ResultConfirmed { get; private set; }

	public ProfileSelectDialogWindow()
	{
		base.Width = 520.0;
		base.MinWidth = 420.0;
		base.MinHeight = 220.0;
		base.MaxHeight = 520.0;
		base.SizeToContent = SizeToContent.Height;
		base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		base.SystemDecorations = SystemDecorations.None;
		base.Background = ThemeBrushes.Get("ThemeBackground", "#0F172A");
		base.Content = CreateDialogContent();
		this.GetObservable(MessageProperty).Subscribe(delegate
		{
			UpdateContent();
		});
	}

	public void SetItems(IEnumerable<string> items)
	{
		_comboBox?.Items.Clear();
		foreach (string item in items)
		{
			_comboBox?.Items.Add(item);
		}
		if (_comboBox != null && _comboBox.Items.Count > 0)
		{
			_comboBox.SelectedIndex = 0;
		}
	}

	private Control CreateDialogContent()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0,
			Margin = new Thickness(0.0)
		};
		_messageBlock = new TextBlock
		{
			FontSize = 14.0,
			Foreground = ThemeBrushes.Get("ThemeTextPrimary", "#E5E7EB"),
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 460.0
		};
		_comboBox = new ComboBox
		{
			Background = ThemeBrushes.Get("ThemeInputBackground", "#0F172A"),
			Foreground = Brushes.White,
			BorderBrush = ThemeBrushes.Get("ThemeBorder", "#334155"),
			Padding = new Thickness(10.0, 8.0),
			CornerRadius = new CornerRadius(8.0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MinHeight = 38.0
		};
		StackPanel buttonRowPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Cancel",
			Background = ThemeBrushes.Get("BtnSecondary", "#475569"),
			Foreground = Brushes.White,
			Padding = new Thickness(24.0, 8.0, 24.0, 8.0),
			CornerRadius = new CornerRadius(6.0),
			MinWidth = 96.0,
			MinHeight = 36.0,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center
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
			Padding = new Thickness(24.0, 8.0, 24.0, 8.0),
			CornerRadius = new CornerRadius(6.0),
			MinWidth = 96.0,
			MinHeight = 36.0,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		okButton.Click += delegate
		{
			if (_comboBox?.SelectedItem is string selectedProfileName)
			{
				SelectedProfileName = selectedProfileName;
				ResultConfirmed = true;
			}
			Close();
		};
		buttonRowPanel.Children.Add(button);
		buttonRowPanel.Children.Add(okButton);
		stackPanel.Children.Add(_messageBlock);
		stackPanel.Children.Add(_comboBox);
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
		return result;
	}

	private void UpdateContent()
	{
		_messageBlock.Text = Message;
	}
}
