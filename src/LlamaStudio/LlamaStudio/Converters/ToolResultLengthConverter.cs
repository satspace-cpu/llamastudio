using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class ToolResultLengthConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string { Length: var length })
		{
			if (length < 1024)
			{
				return $"{length} chars";
			}
			return $"{(double)length / 1024.0:F1} KB";
		}
		return "";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
