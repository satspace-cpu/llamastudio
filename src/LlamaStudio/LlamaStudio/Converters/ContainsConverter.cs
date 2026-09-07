using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class ContainsConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return false;
		}
		return value.ToString()?.Contains(parameter.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) ?? false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
