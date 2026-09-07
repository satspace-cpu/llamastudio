using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class GreaterThanConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is IComparable comparable && parameter != null)
		{
			return comparable.CompareTo(System.Convert.ChangeType(parameter, value.GetType())) > 0;
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
