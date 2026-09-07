using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ChatRoleToNameConverter : IValueConverter
{
	private static readonly Dictionary<ChatRole, string> _names = new Dictionary<ChatRole, string>
	{
		{
			ChatRole.System,
			"System"
		},
		{
			ChatRole.User,
			"You"
		},
		{
			ChatRole.Assistant,
			"Assistant"
		},
		{
			ChatRole.Tool,
			"Tool"
		}
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatRole key && _names.TryGetValue(key, out string value2))
		{
			return value2;
		}
		return "Unknown";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
