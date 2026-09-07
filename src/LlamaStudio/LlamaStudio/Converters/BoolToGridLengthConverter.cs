using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class BoolToGridLengthConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			return ((bool)value) ? GridLength.Auto : GridLength.Parse("0");
		}
		return GridLength.Parse("0");
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
