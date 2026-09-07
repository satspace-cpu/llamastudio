using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class StringTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = value?.ToString() ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		string parameterText = parameter?.ToString();
		if (!string.IsNullOrEmpty(parameterText) && string.Equals(text, parameterText, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return text;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && !string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (parameter != null)
		{
			return parameter.ToString();
		}
		return "";
	}
}
