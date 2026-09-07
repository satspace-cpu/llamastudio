using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class HelpParser : IHelpParser
{
	private static readonly Regex[] _argPatterns = new Regex[2]
	{
		new Regex("^[\\s]*(--?\\w[\\w-]*)", RegexOptions.Multiline | RegexOptions.Compiled),
		new Regex("[-](\\w[\\w-]*(?:\\s+\\w+)?)\\s*,\\s*(--\\w[\\w-]*(?:\\s+\\w+)?)", RegexOptions.Compiled)
	};

	private static readonly Dictionary<string, string> _knownAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "m", "--model" },
		{ "model", "--model" },
		{ "ngl", "-ngl" },
		{ "gpu-layers", "-ngl" },
		{ "fa", "-fa" },
		{ "flash-attn", "-fa" },
		{ "flash-attention", "-fa" },
		{ "c", "-c" },
		{ "ctx-size", "-c" },
		{ "t", "-t" },
		{ "threads", "-t" },
		{ "tb", "--threads-batch" },
		{ "threads-batch", "--threads-batch" },
		{ "b", "--batch" },
		{ "batch", "--batch" },
		{ "ubatch", "--ubatch" },
		{ "temp", "--temp" },
		{ "temperature", "--temp" },
		{ "top-k", "--top-k" },
		{ "topk", "--top-k" },
		{ "top-p", "--top-p" },
		{ "topp", "--top-p" },
		{ "min-p", "--min-p" },
		{ "minp", "--min-p" },
		{ "typical-p", "--typical-p" },
		{ "repeat-penalty", "--repeat-penalty" },
		{ "repeat-last-n", "--repeat-last-n" },
		{ "presence-penalty", "--presence-penalty" },
		{ "frequency-penalty", "--frequency-penalty" },
		{ "mirostat", "--mirostat" },
		{ "mirostat-tau", "--mirostat-tau" },
		{ "mirostat-eta", "--mirostat-learn-rate" },
		{ "mirostat-learn-rate", "--mirostat-learn-rate" },
		{ "dry-multiplier", "--dry-multiplier" },
		{ "dry-base", "--dry-base" },
		{ "dynatemp-range", "--dynatemp-range" },
		{ "xtc-probability", "--xtc-probability" },
		{ "xtc-threshold", "--xtc-threshold" },
		{ "speculative", "--speculative" },
		{ "spec-type", "--spec-type" },
		{ "ngld", "-ngld" },
		{ "spec-draft-n-max", "--spec-draft-n-max" },
		{ "spec-draft-n-min", "--spec-draft-n-min" },
		{ "spec-draft-p-split", "--spec-draft-p-split" },
		{ "spec-draft-p-min", "--spec-draft-p-min" },
		{ "rope-scaling", "--rope-scaling" },
		{ "rope-frequency-base", "--rope-frequency-base" },
		{ "rope-frequency-scale", "--rope-frequency-scale" },
		{ "yarn-orig-ctx", "--yarn-orig-ctx" },
		{ "yarn-ext-factor", "--yarn-ext-factor" },
		{ "yarn-attn-factor", "--yarn-attn-factor" },
		{ "yarn-beta-fast", "--yarn-beta-fast" },
		{ "yarn-beta-slow", "--yarn-beta-slow" },
		{ "numa", "--numa" },
		{ "cache-prompt", "--cache-prompt" },
		{ "cont-batching", "--cont-batching" },
		{ "verbose", "--verbose" },
		{ "ui", "--ui" },
		{ "metrics", "--metrics" },
		{ "reasoning", "-rea" },
		{ "rea", "-rea" },
		{ "embedding", "--embedding" },
		{ "pooling", "--pooling" },
		{ "mmproj", "--mmproj" },
		{ "draft-model", "--draft-model" },
		{ "host", "--host" },
		{ "port", "--port" },
		{ "timeout", "-to" },
		{ "np", "-np" },
		{ "parallel", "-np" },
		{ "slots", "--slots" },
		{ "predict", "--predict" },
		{ "max-tokens", "--max-tokens" },
		{ "seed", "-s" },
		{ "mmap", "--mmap" },
		{ "mlock", "--mlock" },
		{ "no-mmq", "--no-mmq" },
		{ "no-kv-offload", "--no-kv-offload" },
		{ "main-gpu", "--main-gpu" },
		{ "gpu-split", "--gpu-split" },
		{ "tensor-split", "--tensor-split" },
		{ "cache-type-k", "--cache-type-k" },
		{ "cache-type-v", "--cache-type-v" },
		{ "cram", "--cache-ram" },
		{ "cache-ram", "--cache-ram" },
		{ "cache-reuse", "--cache-reuse" },
		{ "reasoning-budget", "--reasoning-budget" },
		{ "fit", "--fit" },
		{ "fit-target", "-fitt" },
		{ "fitt", "-fitt" },
		{ "fitc", "-fitc" }
	};

	public async Task<LlamaHelpInfo?> ParseAsync(string serverExePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(serverExePath) || !File.Exists(serverExePath))
		{
			return null;
		}
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = serverExePath,
				Arguments = "--help",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			Process process = Process.Start(startInfo) ?? throw new FileNotFoundException("Failed to start process", serverExePath);
			try
			{
				using (cancellationToken.Register(delegate
				{
					process.Kill();
				}))
				{
					Task<string>[] tasks = new Task<string>[2]
					{
						process.StandardOutput.ReadToEndAsync(),
						process.StandardError.ReadToEndAsync()
					};
					if (!(await WaitExitAsync(process, cancellationToken).WaitAsync(cancellationToken)))
					{
						process.Kill();
						return null;
					}
					await Task.WhenAll(tasks);
					string standardOutput = tasks[0].Result;
					string standardError = tasks[1].Result;
					string combinedOutput = standardOutput + standardError;
					if (string.IsNullOrWhiteSpace(combinedOutput))
					{
						return null;
					}
					return ParseHelpText(combinedOutput);
				}
			}
			finally
			{
				if (process != null)
				{
					((IDisposable)process).Dispose();
				}
			}
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static async Task<bool> WaitExitAsync(Process process, CancellationToken ct)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		process.EnableRaisingEvents = true;
		process.Exited += delegate
		{
			tcs.TrySetResult(result: true);
		};
		if (process.HasExited)
		{
			return true;
		}
		using (ct.Register(delegate
		{
			tcs.TrySetCanceled();
		}))
		{
			return await tcs.Task.WaitAsync(ct);
		}
	}

	private LlamaHelpInfo ParseHelpText(string output)
	{
		HashSet<string> supportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in _argPatterns[0].Matches(output))
		{
			string argName = match.Groups[1].Value.Trim();
			if (argName.Length >= 2 && !char.IsDigit(argName[1]))
			{
				supportedFlags.Add(argName);
			}
		}
		foreach (Match match in _argPatterns[1].Matches(output))
		{
			string shortFlag = "-" + match.Groups[1].Value.Split(' ')[0].Trim();
			string longFlag = match.Groups[2].Value.Split(' ')[0].Trim();
			supportedFlags.Add(shortFlag);
			supportedFlags.Add(longFlag);
		}
		string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].TrimStart();
			if (!line.StartsWith("--") && (!line.StartsWith("-") || line.Length <= 1 || !char.IsLetter(line[1])))
			{
				continue;
			}
			string[] lineTokens = line.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (lineTokens.Length != 0)
			{
				supportedFlags.Add(lineTokens[0]);
			}
			string[] parts = line.Split(',');
			for (int j = 0; j < parts.Length; j++)
			{
				string part = parts[j].TrimStart();
				if ((part.StartsWith("--") || (part.StartsWith("-") && part.Length > 1)) && char.IsLetter(part[1]))
				{
					string[] partTokens = part.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (partTokens.Length != 0)
					{
						supportedFlags.Add(partTokens[0]);
					}
				}
			}
		}
		foreach (string flag in supportedFlags.ToList())
		{
			string key = flag.TrimStart('-');
			if (_knownAliases.TryGetValue(key, out string canonicalFlag))
			{
				supportedFlags.Add(canonicalFlag);
			}
		}
		Dictionary<string, CliFlagInfo> flags = ParseStructuredFlags(output);
		return new LlamaHelpInfo
		{
			SupportedFlags = supportedFlags,
			RawOutput = output,
			Flags = flags
		};
	}

	private static int FindAliasComma(string line)
	{
		for (int i = 0; i < line.Length; i++)
		{
			if (line[i] == ',')
			{
				string afterComma = line.Substring(i + 1).TrimStart();
				if (afterComma.StartsWith("--") || (afterComma.StartsWith("-") && afterComma.Length > 1 && char.IsLetter(afterComma[1])))
				{
					return i;
				}
			}
		}
		return -1;
	}

	private static Dictionary<string, CliFlagInfo> ParseStructuredFlags(string output)
	{
		Dictionary<string, CliFlagInfo> flags = new Dictionary<string, CliFlagInfo>(StringComparer.OrdinalIgnoreCase);
		Regex defaultPattern = new Regex("\\(default:\\s*([^)]+)\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].TrimStart();
			if (!line.StartsWith("--") && (!line.StartsWith("-") || line.Length <= 1 || !char.IsLetter(line[1])))
			{
				continue;
			}
			int commaIndex = FindAliasComma(line);
			string description;
			string shortName;
			string flagName;
			if (commaIndex > 0)
			{
				string aliasPart = line.Substring(0, commaIndex).Trim();
				description = line.Substring(commaIndex + 1).Trim();
				string[] aliasTokens = aliasPart.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				shortName = ((aliasTokens.Length != 0) ? aliasTokens[0] : null);
				string[] descriptionTokens = description.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				flagName = ((descriptionTokens.Length != 0) ? descriptionTokens[0] : line);
				int flagNameIndex = description.IndexOf(flagName);
				if (flagNameIndex >= 0)
				{
					string rawDescription = description.Substring(flagNameIndex + flagName.Length).Trim();
					if (rawDescription.Length > 0 && char.IsUpper(rawDescription[0]) && !rawDescription.StartsWith("-"))
					{
						int spaceIndex = rawDescription.IndexOf(' ');
						if (spaceIndex > 0)
						{
							rawDescription = rawDescription.Substring(spaceIndex).Trim();
						}
						else if ((spaceIndex < 0 && rawDescription.All(char.IsUpper)) || rawDescription.All((char c) => char.IsUpper(c) || c == '-'))
						{
							rawDescription = string.Empty;
						}
					}
					description = rawDescription;
				}
			}
			else
			{
				string[] lineTokens = line.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				flagName = lineTokens[0];
				shortName = null;
				if (lineTokens.Length > 1)
				{
					string secondToken = lineTokens[1];
					description = ((!secondToken.All((char c) => char.IsUpper(c) || c == '-') || secondToken.Length <= 1 || secondToken.StartsWith("-")) ? string.Join(" ", lineTokens.Skip(1)) : ((lineTokens.Length > 2) ? string.Join(" ", lineTokens.Skip(2)) : string.Empty));
				}
				else
				{
					description = string.Empty;
				}
			}
			string cleanDescription = description;
			Match match = defaultPattern.Match(cleanDescription);
			string defaultValue = null;
			if (match.Success)
			{
				defaultValue = match.Groups[1].Value.Trim();
				cleanDescription = defaultPattern.Replace(cleanDescription, "").Trim();
			}
			cleanDescription = cleanDescription.Trim().TrimEnd('-').Trim();
			bool takesValue = defaultValue != null || line.Contains(" MODEL") || line.Contains(" INT") || line.Contains(" FLOAT") || line.Contains(" STR") || line.Contains(" STRING") || line.Contains(" BOOL") || line.Contains(" FILE") || line.Contains(" PATH") || line.Contains(" DIR") || line.Contains(" URL") || line.Contains(" REPO") || line.Contains(" TEMPLATE") || line.Contains(" FILE") || line.Contains(" ENUM") || line.Contains(" N") || line.Contains(" SIZE") || line.Contains(" TYPE") || line.Contains(" MODE") || line.Contains(" VAL") || line.Contains(" VALUE") || line.Contains(" [");
			CliFlagInfo flagInfo = new CliFlagInfo
			{
				Name = flagName,
				ShortName = shortName,
				Description = cleanDescription,
				TakesValue = takesValue,
				DefaultValue = (defaultValue ?? string.Empty)
			};
			flags[flagName] = flagInfo;
			if (shortName != null && !flags.ContainsKey(shortName))
			{
				flags[shortName] = new CliFlagInfo
				{
					Name = shortName,
					Description = cleanDescription,
					TakesValue = takesValue,
					DefaultValue = (defaultValue ?? string.Empty)
				};
			}
		}
		return flags;
	}

	public bool IsFlagSupported(LlamaHelpInfo? helpInfo, string flag)
	{
		if (helpInfo == null || string.IsNullOrWhiteSpace(flag))
		{
			return true;
		}
		return helpInfo.IsSupported(flag);
	}
}
