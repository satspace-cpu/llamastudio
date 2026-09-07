using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace LlamaStudio.Converters;

public class Base64ToImageConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string text && !string.IsNullOrEmpty(text))
		{
			try
			{
				using MemoryStream stream = new MemoryStream(System.Convert.FromBase64String(text));
				return new Bitmap(stream);
			}
			catch
			{
			}
		}
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
