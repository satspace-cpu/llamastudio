using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class BoolToIndexConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool flag)
		{
			return flag ? 1 : 0;
		}
		return 0;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int num)
		{
			return num == 1;
		}
		return false;
	}
}
