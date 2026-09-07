using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class IntTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = value?.ToString() ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		if (parameter != null && int.TryParse(parameter.ToString(), out var result) && int.TryParse(text, out var result2) && result2 == result)
		{
			return "";
		}
		return text;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && int.TryParse(text.Trim(), out var result))
		{
			return result;
		}
		if (parameter != null && int.TryParse(parameter.ToString(), out var result2))
		{
			return result2;
		}
		return 0;
	}
}
