using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class IntToBoolConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int num)
		{
			return num > 0;
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
