using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class EqualConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return false;
		}
		if (value.Equals(parameter))
		{
			return true;
		}
		string text = value.ToString();
		string parameterText = parameter.ToString();
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(parameterText))
		{
			return false;
		}
		if (text == parameterText)
		{
			return true;
		}
		try
		{
			return object.Equals(System.Convert.ChangeType(value, typeof(double)), System.Convert.ChangeType(parameter, typeof(double)));
		}
		catch
		{
			return false;
		}
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return parameter;
	}
}
