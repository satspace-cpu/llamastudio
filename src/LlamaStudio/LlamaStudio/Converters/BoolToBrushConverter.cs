using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class BoolToBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		string[] array = parameter?.ToString()?.Split('|');
		if (value is bool flag && array != null && array.Length >= 2)
		{
			return new SolidColorBrush(Color.Parse(flag ? array[0] : array[1]));
		}
		return new SolidColorBrush(Color.Parse("#64748B"));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
