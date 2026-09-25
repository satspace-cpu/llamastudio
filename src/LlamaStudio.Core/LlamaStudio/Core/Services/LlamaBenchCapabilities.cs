using System;
using System.Text;

namespace LlamaStudio.Core.Services;

/// <summary>
/// r101: llama-bench CLI capabilities detected from the REAL binary's
/// `--help` output (never assumed). llama.cpp changes flags between builds,
/// so the runner only uses flags that this specific build reports.
/// </summary>
public class LlamaBenchCapabilities
{
	public bool HelpAvailable { get; set; }
	public bool SupportsPrompt { get; set; }        // -p / --n-prompt
	public bool SupportsGen { get; set; }           // -n / --n-gen
	public bool SupportsDepth { get; set; }         // -d / --n-depth
	public bool SupportsPg { get; set; }            // -pg pp,tg
	public bool SupportsRepetitions { get; set; }   // -r / --repetitions
	public bool SupportsOutput { get; set; }        // -o / --output
	public bool SupportsJsonOutput { get; set; }    // -o json
	public bool SupportsNoWarmup { get; set; }      // --no-warmup (warmup ON by default)
	public bool SupportsModel { get; set; }         // -m / --model
	public bool SupportsNgl { get; set; }           // -ngl
	public bool SupportsFlashAttn { get; set; }     // -fa
	public bool SupportsSplitMode { get; set; }     // -sm
	public bool SupportsMainGpu { get; set; }       // -mg
	public bool SupportsTensorSplit { get; set; }   // -ts
	public bool SupportsThreads { get; set; }       // -t
	public bool SupportsBatch { get; set; }         // -b
	public bool SupportsUbatch { get; set; }        // -ub
	public bool SupportsCacheTypeK { get; set; }    // -ctk
	public bool SupportsCacheTypeV { get; set; }    // -ctv

	/// <summary>Human-readable summary for the UI ("Возможности определены автоматически").</summary>
	public string Summary =>
		"llama-bench: PP" + (SupportsPrompt ? "✓" : "✗") +
		" TG" + (SupportsGen ? "✓" : "✗") +
		" Depth" + (SupportsDepth ? "✓" : "✗") +
		" JSON" + (SupportsJsonOutput ? "✓" : "✗") +
		" Repetitions" + (SupportsRepetitions ? "✓" : "✗");

	/// <summary>Parses llama-bench --help text into capabilities.</summary>
	public static LlamaBenchCapabilities ParseHelp(string? helpText)
	{
		var caps = new LlamaBenchCapabilities();
		if (string.IsNullOrWhiteSpace(helpText))
		{
			return caps;
		}
		caps.HelpAvailable = true;
		caps.SupportsPrompt = HasFlag(helpText, "--n-prompt") || HasFlag(helpText, "-p,");
		caps.SupportsGen = HasFlag(helpText, "--n-gen") || HasFlag(helpText, "-n,");
		caps.SupportsDepth = HasFlag(helpText, "--n-depth") || HasFlag(helpText, "-d,");
		caps.SupportsPg = HasFlag(helpText, "-pg");
		caps.SupportsRepetitions = HasFlag(helpText, "--repetitions") || HasFlag(helpText, "-r,");
		caps.SupportsOutput = HasFlag(helpText, "--output");
		caps.SupportsNoWarmup = HasFlag(helpText, "--no-warmup");
		caps.SupportsModel = HasFlag(helpText, "--model") || HasFlag(helpText, "-m,");
		caps.SupportsNgl = HasFlag(helpText, "--n-gpu-layers") || HasFlag(helpText, "-ngl");
		caps.SupportsFlashAttn = HasFlag(helpText, "--flash-attn") || HasFlag(helpText, "-fa");
		caps.SupportsSplitMode = HasFlag(helpText, "--split-mode") || HasFlag(helpText, "-sm,");
		caps.SupportsMainGpu = HasFlag(helpText, "--main-gpu") || HasFlag(helpText, "-mg");
		caps.SupportsTensorSplit = HasFlag(helpText, "--tensor-split") || HasFlag(helpText, "-ts,");
		caps.SupportsThreads = HasFlag(helpText, "--threads") || HasFlag(helpText, "-t,");
		caps.SupportsBatch = HasFlag(helpText, "--batch-size") || HasFlag(helpText, "-b,");
		caps.SupportsUbatch = HasFlag(helpText, "--ubatch-size") || HasFlag(helpText, "-ub,");
		caps.SupportsCacheTypeK = HasFlag(helpText, "--cache-type-k") || HasFlag(helpText, "-ctk,");
		caps.SupportsCacheTypeV = HasFlag(helpText, "--cache-type-v") || HasFlag(helpText, "-ctv,");
		// JSON support: the -o/--output line must list "json" among its choices.
		if (caps.SupportsOutput)
		{
			int idx = helpText.IndexOf("--output", StringComparison.Ordinal);
			if (idx >= 0)
			{
				string line = ExtractLine(helpText, idx);
				caps.SupportsJsonOutput = line.Contains("json", StringComparison.OrdinalIgnoreCase);
			}
		}
		return caps;
	}

	private static bool HasFlag(string text, string token)
	{
		// Match the token as a standalone flag (followed by space or end of segment).
		int idx = 0;
		while ((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
		{
			int after = idx + token.Length;
			if (after >= text.Length || char.IsWhiteSpace(text[after]) || text[after] == '<')
			{
				return true;
			}
			idx = after;
		}
		return false;
	}

	private static string ExtractLine(string text, int index)
	{
		int start = text.LastIndexOf('\n', index);
		start = start < 0 ? 0 : start + 1;
		int end = text.IndexOf('\n', index);
		end = end < 0 ? text.Length : end;
		return text.Substring(start, end - start);
	}
}
