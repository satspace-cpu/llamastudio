using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ChatRoleToBrushConverter : IValueConverter
{
	private static readonly Dictionary<ChatRole, SolidColorBrush> _brushes = new Dictionary<ChatRole, SolidColorBrush>
	{
		{
			ChatRole.System,
			new SolidColorBrush(Color.Parse("#64748B"))
		},
		{
			ChatRole.User,
			new SolidColorBrush(Color.Parse("#4338CA"))
		},
		{
			ChatRole.Assistant,
			new SolidColorBrush(Color.Parse("#059669"))
		},
		{
			ChatRole.Tool,
			new SolidColorBrush(Color.Parse("#7C3AED"))
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
