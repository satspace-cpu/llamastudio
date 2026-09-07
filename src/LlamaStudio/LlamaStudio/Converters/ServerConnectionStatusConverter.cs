using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class ServerConnectionStatusConverter : IMultiValueConverter
{
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values.Count < 2)
		{
			return "Disconnected";
		}
		string? obj = values[0]?.ToString();
		string text = values[1]?.ToString();
		if (!(obj == "Running"))
		{
			return "Disconnected";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "Connected";
		}
		return text;
	}
}
