using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class BookmarkBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return new SolidColorBrush(Color.Parse((value is int num && num != 0) ? "#FBBF24" : "#6B7280"));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
