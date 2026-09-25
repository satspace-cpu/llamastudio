using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LlamaStudio.Core.Services;

/// <summary>
/// r101: context point planning.
/// The maximum depth NEVER comes from hardcoded constants — it is derived from
/// the active launch profile (ContextSize), with a safe reserve for the output
/// plus a small service margin:
///   SafeMax = MaxContext − OutputTokens − round(MaxContext × 2%)
/// Auto points are fractions of the safe max: 6.25 / 12.5 / 25 / 50 / 75 / 90%.
/// </summary>
public static class BenchmarkContextPointGenerator
{
	public const double ServiceMarginRatio = 0.02;
	private static readonly double[] AutoFractions = { 0.0625, 0.125, 0.25, 0.50, 0.75, 0.90 };
	public const int MinPoint = 256;

	/// <summary>
	/// Safe maximum prompt length in tokens for a profile with the given context.
	/// OutputTokens is reserved for the generated answer; a 2% service margin
	/// (context shift bookkeeping, KV alignment) is kept free.
	/// </summary>
	public static int ComputeSafeMaxContext(int maxContext, int outputTokens)
	{
		if (maxContext <= 0)
		{
			return 0;
		}
		int margin = (int)(maxContext * ServiceMarginRatio);
		int safe = maxContext - Math.Max(0, outputTokens) - margin;
		return Math.Max(0, Math.Min(safe, maxContext - 1));
	}

	/// <summary>
	/// Auto context points for the given safe max (actual target tokens).
	/// Deduplicated and rounded down; never above the safe max; at least one
	/// point is always returned (256) when the safe max is positive.
	/// </summary>
	public static int[] GenerateAutoPoints(int safeMaxContext)
	{
		return GenerateAutoPoints(safeMaxContext, AutoFractions.Length);
	}

	/// <summary>
	/// r108: auto points over a WINDOW of the fractions: <paramref name="count"/>
	/// fractions starting at <paramref name="startIndex"/> (cheapest → expensive).
	/// The ENGINE (llama-bench) sweep uses fractions 12.5/25/50/75%: the 6.25%
	/// micro-point is statistically useless (flat PP/TG) and the 90% point alone
	/// adds an hour+ of GPU time on a 27B model. Deep context is covered by the
	/// cheap, server-based Depth test — and by «Свои точки» here.
	/// </summary>
	public static int[] GenerateAutoPoints(int safeMaxContext, int count, int startIndex = 0)
	{
		if (safeMaxContext <= 0 || count <= 0)
		{
			return Array.Empty<int>();
		}
		int start = Math.Max(0, Math.Min(startIndex, AutoFractions.Length - 1));
		int n = Math.Min(count, AutoFractions.Length - start);
		var points = new List<int>();
		for (int i = start; i < start + n; i++)
		{
			int candidate = (int)(safeMaxContext * AutoFractions[i]);
			if (candidate < MinPoint)
			{
				candidate = MinPoint;
			}
			candidate = Math.Min(candidate, safeMaxContext);
			if (candidate >= MinPoint && (points.Count == 0 || candidate > points[^1]))
			{
				points.Add(candidate);
			}
		}
		return points.Count > 0 ? points.ToArray() : new[] { Math.Min(MinPoint, safeMaxContext) };
	}

	/// <summary>
	/// Parses manual points ("15000, 32000; 64000" / newlines accepted).
	/// Validates each value against the safe max BEFORE launching.
	/// Returns (points, errors); points is empty when invalid.
	/// </summary>
	public static (int[] Points, List<string> Errors) ParseManualPoints(string? text, int safeMaxContext)
	{
		var errors = new List<string>();
		var points = new List<int>();
		if (string.IsNullOrWhiteSpace(text))
		{
			errors.Add("Введите хотя бы одно значение, например: 16000, 32000, 64000, 128000");
			return (Array.Empty<int>(), errors);
		}
		foreach (string raw in text.Split(new[] { ',', ';', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string token = raw.Trim().ToLowerInvariant().TrimEnd('k');
			double multiplier = 1.0;
			if (raw.Trim().EndsWith("K", StringComparison.OrdinalIgnoreCase) || raw.Trim().EndsWith("k"))
			{
				multiplier = 1024.0;
			}
			if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value <= 0)
			{
				errors.Add("Некорректное значение: «" + raw.Trim() + "»");
				continue;
			}
			int candidate = (int)(value * multiplier);
			if (candidate < MinPoint)
			{
				errors.Add(raw.Trim() + " — меньше минимума " + MinPoint);
				continue;
			}
			if (safeMaxContext > 0 && candidate > safeMaxContext)
			{
				errors.Add(raw.Trim() + " — больше безопасного максимума " + safeMaxContext);
				continue;
			}
			if (!points.Contains(candidate))
			{
				points.Add(candidate);
			}
		}
		points.Sort();
		if (errors.Count > 0)
		{
			return (Array.Empty<int>(), errors);
		}
		return (points.ToArray(), errors);
	}

	/// <summary>Human label for a point: "16K", "128K", "1 234".</summary>
	public static string FormatPoint(int tokens)
	{
		if (tokens >= 1024 && tokens % 1024 == 0)
		{
			return (tokens / 1024) + "K";
		}
		if (tokens >= 1024)
		{
			return (tokens / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + "K";
		}
		return tokens.ToString(CultureInfo.InvariantCulture);
	}
}
