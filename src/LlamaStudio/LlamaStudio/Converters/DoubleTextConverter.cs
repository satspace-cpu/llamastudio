using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class DoubleTextConverter : IValueConverter
{
	private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = value?.ToString() ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, _inv, out var result) && double.TryParse(text, NumberStyles.Any, _inv, out var result2) && Math.Abs(result2 - result) < 1E-09)
		{
			return "";
		}
		return text;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Any, _inv, out var result))
		{
			return result;
		}
		if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, _inv, out var result2))
		{
			return result2;
		}
		return 0.0;
	}
}
