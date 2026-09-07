using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ChatRoleToInitialConverter : IValueConverter
{
	private static readonly Dictionary<ChatRole, string> _initials = new Dictionary<ChatRole, string>
	{
		{
			ChatRole.System,
			"S"
		},
		{
			ChatRole.User,
			"U"
		},
		{
			ChatRole.Assistant,
			"A"
		},
		{
			ChatRole.Tool,
			"T"
		}
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatRole key && _initials.TryGetValue(key, out string value2))
		{
			return value2;
		}
		return "?";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
