using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ChatRoleTextBrushConverter : IValueConverter
{
	private static readonly Dictionary<ChatRole, SolidColorBrush> _brushes = new Dictionary<ChatRole, SolidColorBrush>
	{
		{
			ChatRole.System,
			new SolidColorBrush(Color.Parse("#94A3B8"))
		},
		{
			ChatRole.User,
			new SolidColorBrush(Color.Parse("#E2E8F0"))
		},
		{
			ChatRole.Assistant,
			new SolidColorBrush(Color.Parse("#E2E8F0"))
		},
		{
			ChatRole.Tool,
			new SolidColorBrush(Color.Parse("#A78BFA"))
		}
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatRole key && _brushes.TryGetValue(key, out SolidColorBrush value2))
		{
			return value2;
		}
		return _brushes[ChatRole.Assistant];
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
