using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class NotEqualConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null && parameter == null)
		{
			return false;
		}
		if (value == null || parameter == null)
		{
			return true;
		}
		if (value.Equals(parameter))
		{
			return false;
		}
		string text = value.ToString();
		string parameterText = parameter.ToString();
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(parameterText))
		{
			return string.IsNullOrEmpty(text) != string.IsNullOrEmpty(parameterText);
		}
		if (text == parameterText)
		{
			return false;
		}
		try
		{
			return !object.Equals(System.Convert.ChangeType(value, typeof(double)), System.Convert.ChangeType(parameter, typeof(double)));
		}
		catch
		{
			return true;
		}
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
