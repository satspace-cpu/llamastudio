using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class BookmarkTooltipConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		bool flag = value is bool && (bool)value;
		if (parameter is string text && text.Contains('|'))
		{
			string[] array = text.Split('|');
			if (!flag)
			{
				return array[0];
			}
			return array[1];
		}
		if (!flag)
		{
			return "Bookmark";
		}
		return "Remove bookmark";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
