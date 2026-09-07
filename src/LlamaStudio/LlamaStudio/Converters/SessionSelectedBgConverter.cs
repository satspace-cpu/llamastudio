using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class SessionSelectedBgConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatSession chatSession && parameter is string text && chatSession.Id == text)
		{
			return new SolidColorBrush(Color.Parse("#2D2D3D"));
		}
		return Brushes.Transparent;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
