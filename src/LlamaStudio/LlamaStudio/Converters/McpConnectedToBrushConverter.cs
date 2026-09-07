using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class McpConnectedToBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			if (!(bool)value)
			{
				return new SolidColorBrush(Color.Parse("#64748B"));
			}
			return new SolidColorBrush(Color.Parse("#34D399"));
		}
		return new SolidColorBrush(Color.Parse("#64748B"));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
