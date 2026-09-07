using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Converters;

public class BuildTypeToBrushConverter : IValueConverter
{
	private static readonly Dictionary<BuildType, SolidColorBrush> _brushes = new Dictionary<BuildType, SolidColorBrush>
	{
		{
			BuildType.Cuda12x,
			new SolidColorBrush(Color.Parse("#F59E0B"))
		},
		{
			BuildType.Cuda13x,
			new SolidColorBrush(Color.Parse("#A855F7"))
		},
		{
			BuildType.Vulkan,
			new SolidColorBrush(Color.Parse("#3B82F6"))
		},
		{
			BuildType.OpenVino,
			new SolidColorBrush(Color.Parse("#10B981"))
		},
		{
			BuildType.Cpu,
			new SolidColorBrush(Color.Parse("#64748B"))
		},
		{
			BuildType.Unknown,
			new SolidColorBrush(Color.Parse("#64748B"))
		}
	};

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is BuildType key && _brushes.TryGetValue(key, out SolidColorBrush value2))
		{
			return value2;
		}
		return _brushes[BuildType.Unknown];
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
