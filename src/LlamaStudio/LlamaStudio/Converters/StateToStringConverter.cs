using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class StateToStringConverter : IValueConverter
{
	private static readonly Dictionary<string, string> _states = new Dictionary<string, string>
	{
		{ "Stopped", "⏹ Stopped" },
		{ "Starting", "⏳ Starting..." },
		{ "Running", "▶ Running" },
		{ "Stopping", "⏳ Stopping..." },
		{ "Error", "⚠ Error" }
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		string text = value?.ToString();
		if (!_states.TryGetValue(text ?? string.Empty, out string value2))
		{
			return text;
		}
		return value2;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
