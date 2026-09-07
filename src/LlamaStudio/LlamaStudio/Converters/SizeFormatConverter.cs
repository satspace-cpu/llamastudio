using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class SizeFormatConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is long num)
		{
			if (num >= 1048576)
			{
				if (num >= 1073741824)
				{
					return $"{(double)num / 1073741824.0:F1} GiB";
				}
				return $"{(double)num / 1048576.0:F1} MiB";
			}
			if (num >= 1024)
			{
				return $"{(double)num / 1024.0:F1} KiB";
			}
			return $"{num} B";
		}
		return value?.ToString() ?? "0 B";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
