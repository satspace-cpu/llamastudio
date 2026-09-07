using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Converters;

public class McpTransportToVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is McpTransportType mcpTransportType && parameter is string text)
		{
			return mcpTransportType.ToString() == text;
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
