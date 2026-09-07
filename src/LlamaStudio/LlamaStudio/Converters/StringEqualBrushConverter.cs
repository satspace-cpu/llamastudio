using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace LlamaStudio.Converters;

public class StringEqualBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (parameter == null)
		{
			return null;
		}
		if (!(value?.ToString() == parameter.ToString()))
		{
			return null;
		}
		return new SolidColorBrush(Color.Parse(IsLightTheme() ? "#D4D4D4" : "#27272A"));
	}

	private static bool IsLightTheme()
	{
		try
		{
			Application current = Application.Current;
			if (current != null)
			{
				return current.ActualThemeVariant == ThemeVariant.Light;
			}
		}
		catch
		{
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
