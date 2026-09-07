using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class BoolToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		string[] array = parameter?.ToString()?.Split('|');
		if (value is bool)
		{
			if ((bool)value)
			{
				if (array == null || array.Length < 1)
				{
					return "True";
				}
				return array[0];
			}
			if (array == null || array.Length < 2)
			{
				return "False";
			}
			return array[1];
		}
		return value?.ToString() ?? string.Empty;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return false;
	}
}
