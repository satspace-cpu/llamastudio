using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LlamaStudio.Core.Services;

/// <summary>One parsed llama-bench test row (pp512 / tg128 / +depth variant).</summary>
public class LlamaBenchTestResult
{
	public string Test { get; set; } = string.Empty;
	public int N { get; set; }
	public double? Mean { get; set; }
	public double? Median { get; set; }
	public double TokensPerSecond { get; set; }
	public string Backend { get; set; } = string.Empty;
}

/// <summary>Parsed llama-bench output (JSON preferred, markdown table fallback).</summary>
public class LlamaBenchResult
{
	public List<LlamaBenchTestResult> Results { get; } = new List<LlamaBenchTestResult>();
	public bool ParsedAsJson { get; set; }
	public string RawOutput { get; set; } = string.Empty;
	public string? Error { get; set; }
	/// <summary>r105: llama-bench process exit code (0 = success, null = unknown).
	/// Non-zero means the process died (possibly killed by the user) — the caller
	/// must abort the run instead of spawning the next point.</summary>
	public int? ProcessExitCode { get; set; }

	public LlamaBenchTestResult? Find(string kind, int size, int? depth = null)
	{
		foreach (var r in Results)
		{
			var (k, s, d) = LlamaBenchResultParser.ParseTestName(r.Test);
			if (k == kind && s == size && (depth == null || d == depth))
			{
				return r;
			}
		}
		return null;
	}
}

/// <summary>
/// r101: parsers for llama-bench output. JSON (from -o json) is preferred;
/// the markdown-table parser is the fallback for builds without JSON output.
/// </summary>
public static class LlamaBenchResultParser
{
	/// <summary>
	/// Parses a test name: "pp512" → (pp,512,null); "tg128" → (tg,128,null);
	/// "pp512+d64" / "pp512 @ d64" → (pp,512,64). Unknown names → (raw,0,null).
	/// r108: real llama-bench builds (b11042+) render the depth suffix as
	/// "pp512 @ d100" (space-at-space-d) — that is what this method must handle.
	/// </summary>
	public static (string Kind, int Size, int? Depth) ParseTestName(string test)
	{
		if (string.IsNullOrWhiteSpace(test))
		{
			return ("", 0, null);
		}
		var match = Regex.Match(test, @"^(pp|tg)(\d+)(?:\s*(?:\+|@)\s*d(\d+))?$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			int depth = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
			return (match.Groups[1].Value.ToLowerInvariant(),
				int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
				depth > 0 ? depth : null);
		}
		// Best effort: kind from prefix, size from first number.
		string kind = test.StartsWith("pp", StringComparison.OrdinalIgnoreCase) ? "pp"
			: test.StartsWith("tg", StringComparison.OrdinalIgnoreCase) ? "tg" : test;
		var num = Regex.Match(test, @"(\d+)");
		return (kind, num.Success ? int.Parse(num.Value, CultureInfo.InvariantCulture) : 0, null);
	}

	/// <summary>
	/// Parses -o json output. r108: supports BOTH schemas:
	///  (a) old:  {"runs":[{"test":"pp512","n":512,"mean":..,"median":..,"tokens_per_second":..,"backend":..}]}
	///  (b) new (real llama.cpp b11042+ builds, verified on the user's binaries):
	///      [ {"pp512 @ d100": 4832.5, "samples_ns":[...], "samples_ts":[...]},
	///        {"tg128 @ d100": 37.4,   "samples_ns":[...], "samples_ts":[...]} ]
	///      — the test NAME is the property key, the tok/s value is the number.
	/// The old r101 parser understood only (a) → on the user's builds every point
	/// silently parsed to Test="" / 0 tok/s → the ENGINE test burned ~2.5 h of
	/// GPU time and produced nothing but "—" in the table.
	/// </summary>
	public static LlamaBenchResult ParseJson(string json)
	{
		var result = new LlamaBenchResult { RawOutput = json };
		try
		{
			// r110: lenient parse — some builds emit slightly-off JSON (e.g. a
			// trailing comma). Try strict first, then strip trailing commas.
			JsonElement root;
			if (!TryParseLenient(json, out root))
			{
				throw new FormatException("некорректный JSON (и после удаления висячих запятых)");
			}
			List<JsonElement> items = new List<JsonElement>();
			if (root.ValueKind == JsonValueKind.Array)
			{
				foreach (var el in root.EnumerateArray())
				{
					items.Add(el);
				}
			}
			else if (root.ValueKind == JsonValueKind.Object)
			{
				if (root.TryGetProperty("runs", out var r) && r.ValueKind == JsonValueKind.Array)
				{
					foreach (var el in r.EnumerateArray())
					{
						items.Add(el);
					}
				}
				else
				{
					// defensive: a single test object at the root
					items.Add(root);
				}
			}
			if (items.Count == 0)
			{
				throw new FormatException("no runs");
			}
			foreach (JsonElement run in items)
			{
				if (run.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				var item = new LlamaBenchTestResult();

				// r111: three known schemas.
				// (a) old: explicit "test" name string + tokens_per_second.
				// (b) REAL b11042+ (verified from captured output): no name key
				//     at all — the test is identified by "n_prompt"/"n_gen"
				//     (one of them is 0) and the speed is "avg_ts", which is
				//     tokens/second (avg_ts = (n_prompt|n_gen) / (avg_ns/1e9)).
				// (c) fallback: a name key matching pp\d+/tg\d+ whose number is
				//     the tok/s.
				string? testName = null;
				double? explicitTps = null;
				if (run.TryGetProperty("test", out var t) && t.ValueKind == JsonValueKind.String)
				{
					testName = t.GetString() ?? "";
				}
				int? nPrompt = null, nGen = null;
				foreach (var prop in run.EnumerateObject())
				{
					if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetDouble(out double v))
					{
						continue;
					}
					switch (prop.Name.ToLowerInvariant())
					{
						case "n": item.N = (int)v; break;
						case "n_prompt": if (v > 0) nPrompt = (int)v; break;
						case "n_gen": if (v > 0) nGen = (int)v; break;
						case "mean": item.Mean ??= v; break;
						case "median": item.Median ??= v; break;
						case "avg_ts": explicitTps ??= v; break;   // b11042+: tokens/second
						case "tokens_per_second":
						case "t/s":
						case "tps":
						case "tokens_per_sec":
							explicitTps ??= v;
							break;
						default:
							// (c) name-key fallback: only accept pp512/tg128-style keys
							if (testName == null && Regex.IsMatch(prop.Name, "^(pp|tg)\\d"))
							{
								testName = prop.Name;
								explicitTps ??= v;
							}
							break;
					}
				}
				if (testName == null)
				{
					// (b) real schema: build the canonical name from n_prompt/n_gen
					if (nPrompt != null) testName = "pp" + nPrompt;
					else if (nGen != null) testName = "tg" + nGen;
				}
				if (string.IsNullOrEmpty(testName))
				{
					continue;
				}
				item.Test = testName;
				item.TokensPerSecond = explicitTps ?? 0;
				if (run.TryGetProperty("backend", out var b) && b.ValueKind == JsonValueKind.String)
				{
					item.Backend = b.GetString() ?? "";
				}
				if (run.TryGetProperty("backends", out var bs) && bs.ValueKind == JsonValueKind.String)
				{
					item.Backend = bs.GetString() ?? "";
				}
				result.Results.Add(item);
			}
			if (result.Results.Count == 0)
			{
				result.Error = "JSON: не найдено ни одной строки результатов (схема не распознана)";
			}
			else
			{
				result.ParsedAsJson = true;
			}
		}
		catch (Exception ex)
		{
			result.Error = "JSON: " + ex.Message;
		}
		return result;
	}

	/// <summary>
	/// r110: lenient JSON parse. Tries strict parse first, then strips trailing
	/// commas (",\s*[}\]]" → "") and retries. Returns a CLONED root element so
	/// the underlying document can be disposed.
	/// </summary>
	internal static bool TryParseLenient(string json, out JsonElement root)
	{
		root = default;
		try
		{
			using var d = JsonDocument.Parse(json);
			root = d.RootElement.Clone();
			return true;
		}
		catch
		{
			// not strict JSON — try again after removing trailing commas
		}
		try
		{
			string cleaned = Regex.Replace(json, ",(\\s*[}\\]])", "$1");
			using var d2 = JsonDocument.Parse(cleaned);
			root = d2.RootElement.Clone();
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Fallback parser for the default markdown table:
	/// | model | size | params | backend | test | t/s |
	/// (column layout varies between builds — locate the "test" column by header).
	/// </summary>
	public static LlamaBenchResult ParseText(string output)
	{
		var result = new LlamaBenchResult { RawOutput = output };
		string[] lines = output.Split('\n');
		int? testCol = null;
		int? tpsCol = null;
		foreach (string line in lines)
		{
			if (!line.StartsWith("|"))
			{
				continue;
			}
			string[] cells = line.Trim('|').Split('|');
			for (int i = 0; i < cells.Length; i++)
			{
				string c = cells[i].Trim().ToLowerInvariant();
				if (testCol == null && (c == "test" || c == "t"))
				{
					testCol = i;
				}
				if (tpsCol == null && (c == "t/s" || c == "tok/s" || c == "tokens/s"))
				{
					tpsCol = i;
				}
			}
			if (testCol != null && tpsCol != null && cells.Length > Math.Max(testCol.Value, tpsCol.Value))
			{
				string testName = cells[testCol.Value].Trim();
				// Skip the header row itself and separator rows.
				if (testName.Length == 0 || Regex.IsMatch(testName, @"^[-: ]+$"))
				{
					continue;
				}
				string tpsText = cells[tpsCol.Value].Trim().Replace(",", ".");
				if (double.TryParse(tpsText, NumberStyles.Float, CultureInfo.InvariantCulture, out double tps) && tps > 0)
				{
					result.Results.Add(new LlamaBenchTestResult { Test = testName, TokensPerSecond = tps });
				}
			}
		}
		if (result.Results.Count == 0)
		{
			result.Error = "Не удалось распознать таблицу результатов llama-bench";
		}
		return result;
	}
}
