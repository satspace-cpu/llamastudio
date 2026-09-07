using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class ChatRoleToVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is ChatRole chatRole && parameter is string text)
		{
			if (text == "NotTool")
			{
				return chatRole != ChatRole.Tool;
			}
			return chatRole.ToString().Equals(text, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
