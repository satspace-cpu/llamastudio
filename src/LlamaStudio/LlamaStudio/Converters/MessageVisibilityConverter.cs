using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class MessageVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatMessage chatMessage)
		{
			if (chatMessage.Role == ChatRole.Assistant && chatMessage.ToolCalls.Count > 0)
			{
				return !string.IsNullOrWhiteSpace(chatMessage.Content);
			}
			return true;
		}
		return true;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
