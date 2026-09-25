using System;
using System.Globalization;

namespace LlamaStudio.Core.Services;

/// <summary>r101: difference math for the compare view (factual numbers only).</summary>
public static class BenchmarkMath
{
	/// <summary>(value − base) / base × 100. Returns NaN when base is 0 (no data).</summary>
	public static double PercentDiff(double baseValue, double value)
	{
		if (double.IsNaN(baseValue) || double.IsNaN(value) || baseValue <= 0.0)
		{
			return double.NaN;
		}
		return (value - baseValue) / baseValue * 100.0;
	}

	/// <summary>value / base (e.g. 1.35×). Returns NaN when base is 0 (no data).</summary>
	public static double Ratio(double baseValue, double value)
	{
		if (double.IsNaN(baseValue) || double.IsNaN(value) || baseValue <= 0.0)
		{
			return double.NaN;
		}
		return value / baseValue;
	}

	/// <summary>Formats +35% / −12.4% / — (no data).</summary>
	public static string FormatPercentDiff(double baseValue, double value)
	{
		double diff = PercentDiff(baseValue, value);
		if (double.IsNaN(diff))
		{
			return "—";
		}
		return (diff >= 0 ? "+" : "−") + Math.Abs(diff).ToString("0.#", CultureInfo.InvariantCulture) + "%";
	}

	/// <summary>Formats 1.35× / 0.72× / —.</summary>
	public static string FormatRatio(double baseValue, double value)
	{
		double ratio = Ratio(baseValue, value);
		if (double.IsNaN(ratio))
		{
			return "—";
		}
		return ratio.ToString("0.00", CultureInfo.InvariantCulture) + "×";
	}
}
