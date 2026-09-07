using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class NotContainsConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return true;
		}
		string? text = value.ToString();
		return text == null || !text.Contains(parameter.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
