using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class PrereleaseBorderConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		bool flag = default(bool);
		int num;
		if (value is bool)
		{
			flag = (bool)value;
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			return new SolidColorBrush(Color.Parse("#7F1D1D"));
		}
		return new SolidColorBrush(Color.Parse("#334155"));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
