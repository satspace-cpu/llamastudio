using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class LogLevelBrushConverter : IValueConverter
{
	private static readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>
	{
		{
			"Debug",
			new SolidColorBrush(Color.Parse("#64748B"))
		},
		{
			"Information",
			new SolidColorBrush(Color.Parse("#38BDF8"))
		},
		{
			"Warning",
			new SolidColorBrush(Color.Parse("#FBBF24"))
		},
		{
			"Error",
			new SolidColorBrush(Color.Parse("#F87171"))
		},
		{
			"Fatal",
			new SolidColorBrush(Color.Parse("#FF0000"))
		}
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		string text = value?.ToString();
		if (!_brushes.TryGetValue(text ?? string.Empty, out IBrush value2))
		{
			return null;
		}
		return value2;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
