using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LlamaStudio.Converters;

public class PercentToDashConverter : IValueConverter
{
	private const double DefaultRadius = 36.0;

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		double radius = 36.0;
		if (parameter is string stringParameter && double.TryParse(stringParameter, CultureInfo.InvariantCulture, out var parsedRadius))
		{
			radius = parsedRadius;
		}
		else if (parameter is double doubleParameter)
		{
			radius = doubleParameter;
		}
		double center = radius;
		if (value is double percentage && percentage > 0.0)
		{
			double sweepAngle = percentage / 100.0 * 360.0;
			if (sweepAngle > 360.0)
			{
				sweepAngle = 360.0;
			}
			double endAngle = -Math.PI / 2.0 + sweepAngle * Math.PI / 180.0;
			double endX = center + radius * Math.Cos(endAngle);
			double endY = center + radius * Math.Sin(endAngle);
			bool isLargeArc = sweepAngle > 180.0;
			return StreamGeometry.Parse(string.Format(CultureInfo.InvariantCulture, "M {0:F2},{1:F2} A {2:F2},{2:F2} 0 {3},1 {4:F2},{5:F2}", center, center - radius, radius, isLargeArc ? 1 : 0, endX, endY));
		}
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
