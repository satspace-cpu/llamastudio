using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class DefaultToOpacityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return 1.0;
		}
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		double result;
		if (value is double doubleValue)
		{
			result = doubleValue;
		}
		else if (value is float floatValue)
		{
			result = floatValue;
		}
		else if (value is int intValue)
		{
			result = intValue;
		}
		else if (!double.TryParse((value.ToString() ?? "").Replace(',', '.'), NumberStyles.Any, invariantCulture, out result))
		{
			return 1.0;
		}
		List<double> defaultValues = new List<double>();
		if (parameter is double defaultDouble)
		{
			defaultValues.Add(defaultDouble);
		}
		else if (parameter is float defaultFloat)
		{
			defaultValues.Add(defaultFloat);
		}
		else if (parameter is int defaultInt)
		{
			defaultValues.Add(defaultInt);
		}
		else
		{
			string[] parameterParts = (parameter.ToString() ?? "").Split('|');
			for (int i = 0; i < parameterParts.Length; i++)
			{
				if (double.TryParse(parameterParts[i].Trim().Replace(',', '.'), NumberStyles.Any, invariantCulture, out var parsedValue))
				{
					defaultValues.Add(parsedValue);
				}
			}
		}
		if (value is string stringValue && defaultValues.Count == 0)
		{
			string[] parameterParts = (parameter.ToString() ?? "").Split('|');
			foreach (string parameterPart in parameterParts)
			{
				if (string.Equals(stringValue, parameterPart.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return 0.4;
				}
			}
		}
		foreach (double defaultValue in defaultValues)
		{
			if (Math.Abs(result - defaultValue) < 0.001)
			{
				return 0.4;
			}
		}
		return 1.0;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
