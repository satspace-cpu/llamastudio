using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ReasoningVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatMessage chatMessage)
		{
			return !string.IsNullOrEmpty(chatMessage.Reasoning) && chatMessage.Role == ChatRole.Assistant;
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
