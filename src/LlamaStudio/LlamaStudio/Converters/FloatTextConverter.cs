using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class FloatTextConverter : IValueConverter
{
	private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = value?.ToString() ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		float num2;
		if (parameter is double num)
		{
			num2 = (float)num;
		}
		else if (parameter is float num3)
		{
			num2 = num3;
		}
		else
		{
			if (parameter == null || !float.TryParse(parameter.ToString(), NumberStyles.Any, _inv, out var result))
			{
				return text;
			}
			num2 = result;
		}
		if (float.TryParse(text.Replace(',', '.'), NumberStyles.Any, _inv, out var result2) && Math.Abs(result2 - num2) < 1E-06f)
		{
			return "";
		}
		return text;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Any, _inv, out var result))
		{
			return result;
		}
		if (parameter is double num)
		{
			return (float)num;
		}
		if (parameter is float num2)
		{
			return num2;
		}
		if (parameter != null && float.TryParse(parameter.ToString(), NumberStyles.Any, _inv, out var result2))
		{
			return result2;
		}
		return 0f;
	}
}
