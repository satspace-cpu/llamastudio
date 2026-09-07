using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class ContextProgressBarColorConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is double num)
		{
			if (num < 70.0)
			{
				return "#10B981";
			}
			if (num < 90.0)
			{
				return "#F59E0B";
			}
			return "#EF4444";
		}
		return "#10B981";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
