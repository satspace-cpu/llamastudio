using System;
using System.Collections.Generic;
using System.Text;

namespace LlamaStudio.Core.Models;

public static class CommandLineParser
{
	public static readonly Dictionary<string, ArgumentMapping> KnownArgumentsLookup = new Dictionary<string, ArgumentMapping>(StringComparer.OrdinalIgnoreCase)
	{
		{
			"host",
			new ArgumentMapping("--host", ArgType.String)
		},
		{
			"port",
			new ArgumentMapping("--port", ArgType.Int)
		},
		{
			"to",
			new ArgumentMapping("-to", ArgType.Int)
		},
		{
			"timeout",
			new ArgumentMapping("--timeout", ArgType.Int)
		},
		{
			"hf",
			new ArgumentMapping("-hf", ArgType.String)
		},
		{
			"hff",
			new ArgumentMapping("-hff", ArgType.String)
		},
		{
			"offline",
			new ArgumentMapping("--offline", ArgType.Flag)
		},
		{
			"hfd",
			new ArgumentMapping("-hfd", ArgType.String)
		},
		{
			"m",
			new ArgumentMapping("-m", ArgType.String)
		},
		{
			"model",
			new ArgumentMapping("-m", ArgType.String)
		},
		{
			"a",
			new ArgumentMapping("-a", ArgType.String)
		},
		{
			"alias",
			new ArgumentMapping("--alias", ArgType.String)
		},
		{
			"apikey",
			new ArgumentMapping("--api-key", ArgType.String)
		},
		{
			"api-key",
			new ArgumentMapping("--api-key", ArgType.String)
		},
		{
			"logfilepath",
			new ArgumentMapping("--log-file", ArgType.String)
		},
		{
			"log-file",
			new ArgumentMapping("--log-file", ArgType.String)
		},
		{
			"verboselogging",
			new ArgumentMapping("--verbose", ArgType.Flag)
		},
		{
			"verbose",
			new ArgumentMapping("--verbose", ArgType.Flag)
		},
		{
			"enablewebui",
			new ArgumentMapping("--ui", ArgType.Flag)
		},
		{
			"ui",
			new ArgumentMapping("--ui", ArgType.Flag)
		},
		{
			"enableslots",
			new ArgumentMapping("--slots", ArgType.BoolFlag)
		},
		{
			"slots",
			new ArgumentMapping("-np", ArgType.Int)
		},
		{
			"parallel",
			new ArgumentMapping("-np", ArgType.Int)
		},
		{
			"np",
			new ArgumentMapping("-np", ArgType.Int)
		},
		{
			"noslots",
			new ArgumentMapping("--no-slots", ArgType.Flag)
		},
		{
			"enablemetrics",
			new ArgumentMapping("--metrics", ArgType.Flag)
		},
		{
			"metrics",
			new ArgumentMapping("--metrics", ArgType.Flag)
		},
		{
			"reasoning",
			new ArgumentMapping("--reasoning", ArgType.BoolOnOff)
		},
		{
			"rea",
			new ArgumentMapping("-rea", ArgType.BoolOnOff)
		},
		{
			"reasoningbudget",
			new ArgumentMapping("--reasoning-budget", ArgType.Int)
		},
		{
			"reasoning-budget",
			new ArgumentMapping("--reasoning-budget", ArgType.Int)
		},
		{
			"seed",
			new ArgumentMapping("--seed", ArgType.Int)
		},
		{
			"presencepenalty",
			new ArgumentMapping("--presence-penalty", ArgType.Float)
		},
		{
			"presence-penalty",
			new ArgumentMapping("--presence-penalty", ArgType.Float)
		},
		{
			"frequencypenalty",
			new ArgumentMapping("--frequency-penalty", ArgType.Float)
		},
		{
			"frequency-penalty",
			new ArgumentMapping("--frequency-penalty", ArgType.Float)
		},
		{
			"maxtokens",
			new ArgumentMapping("--predict", ArgType.Int)
		},
		{
			"max-tokens",
			new ArgumentMapping("--predict", ArgType.Int)
		},
		{
			"cacheprompt",
			new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff)
		},
		{
			"cache-prompt",
			new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff)
		},
		{
			"contbatching",
			new ArgumentMapping("--cont-batching", ArgType.BoolOnOff)
		},
		{
			"cont-batching",
			new ArgumentMapping("--cont-batching", ArgType.BoolOnOff)
		},
		{
			"gpulayers",
			new ArgumentMapping("-ngl", ArgType.String)
		},
		{
			"gpu-layers",
			new ArgumentMapping("-ngl", ArgType.String)
		},
		{
			"ngl",
			new ArgumentMapping("-ngl", ArgType.String)
		},
		{
			"sm",
			new ArgumentMapping("-sm", ArgType.String)
		},
		{
			"gpusplitmode",
			new ArgumentMapping("--gpu-split", ArgType.Int)
		},
		{
			"gpu-split",
			new ArgumentMapping("--gpu-split", ArgType.Int)
		},
		{
			"ts",
			new ArgumentMapping("-ts", ArgType.String)
		},
		{
			"tensorsplit",
			new ArgumentMapping("--tensor-split", ArgType.String)
		},
		{
			"tensor-split",
			new ArgumentMapping("--tensor-split", ArgType.String)
		},
		{
			"mg",
			new ArgumentMapping("-mg", ArgType.Int)
		},
		{
			"maingpu",
			new ArgumentMapping("--main-gpu", ArgType.Int)
		},
		{
			"main-gpu",
			new ArgumentMapping("--main-gpu", ArgType.Int)
		},
		{
			"flashattention",
			new ArgumentMapping("-fa", ArgType.String)
		},
		{
			"flash-attn",
			new ArgumentMapping("-fa", ArgType.String)
		},
		{
			"fa",
			new ArgumentMapping("-fa", ArgType.String)
		},
		{
			"no-mmap",
			new ArgumentMapping("--no-mmap", ArgType.Flag)
		},
		{
			"nommap",
			new ArgumentMapping("--no-mmap", ArgType.Flag)
		},
		{
			"mmap",
			new ArgumentMapping("--mmap", ArgType.BoolOnOff)
		},
		{
			"mlock",
			new ArgumentMapping("--mlock", ArgType.BoolOnOff)
		},
		{
			"nokvoffload",
			new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff)
		},
		{
			"no-kv-offload",
			new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff)
		},
		{
			"c",
			new ArgumentMapping("-c", ArgType.Int)
		},
		{
			"contextsize",
			new ArgumentMapping("-c", ArgType.Int)
		},
		{
			"ctx-size",
			new ArgumentMapping("-c", ArgType.Int)
		},
		{
			"b",
			new ArgumentMapping("-b", ArgType.Int)
		},
		{
			"batch",
			new ArgumentMapping("--batch", ArgType.Int)
		},
		{
			"batchsize",
			new ArgumentMapping("--batch", ArgType.Int)
		},
		{
			"batch-size",
			new ArgumentMapping("--batch", ArgType.Int)
		},
		{
			"ub",
			new ArgumentMapping("-ub", ArgType.Int)
		},
		{
			"ubatch",
			new ArgumentMapping("--ubatch", ArgType.Int)
		},
		{
			"ubatchsize",
			new ArgumentMapping("--ubatch", ArgType.Int)
		},
		{
			"ubatch-size",
			new ArgumentMapping("--ubatch", ArgType.Int)
		},
		{
			"ctk",
			new ArgumentMapping("-ctk", ArgType.String)
		},
		{
			"cachetypek",
			new ArgumentMapping("--cache-type-k", ArgType.String)
		},
		{
			"cache-type-k",
			new ArgumentMapping("--cache-type-k", ArgType.String)
		},
		{
			"ctv",
			new ArgumentMapping("-ctv", ArgType.String)
		},
		{
			"cachetypev",
			new ArgumentMapping("--cache-type-v", ArgType.String)
		},
		{
			"cache-type-v",
			new ArgumentMapping("--cache-type-v", ArgType.String)
		},
		{
			"cache-ram",
			new ArgumentMapping("--cache-ram", ArgType.Int)
		},
		{
			"cacheram",
			new ArgumentMapping("--cache-ram", ArgType.Int)
		},
		{
			"cache-reuse",
			new ArgumentMapping("--cache-reuse", ArgType.Int)
		},
		{
			"cachereuse",
			new ArgumentMapping("--cache-reuse", ArgType.Int)
		},
		{
			"t",
			new ArgumentMapping("-t", ArgType.Int)
		},
		{
			"threads",
			new ArgumentMapping("--threads", ArgType.Int)
		},
		{
			"tb",
			new ArgumentMapping("-tb", ArgType.Int)
		},
		{
			"threadsbatch",
			new ArgumentMapping("--threads-batch", ArgType.Int)
		},
		{
			"threads-batch",
			new ArgumentMapping("--threads-batch", ArgType.Int)
		},
		{
			"temperature",
			new ArgumentMapping("--temp", ArgType.Float)
		},
		{
			"temp",
			new ArgumentMapping("--temp", ArgType.Float)
		},
		{
			"topk",
			new ArgumentMapping("--top-k", ArgType.Int)
		},
		{
			"top-k",
			new ArgumentMapping("--top-k", ArgType.Int)
		},
		{
			"topp",
			new ArgumentMapping("--top-p", ArgType.Float)
		},
		{
			"top-p",
			new ArgumentMapping("--top-p", ArgType.Float)
		},
		{
			"minp",
			new ArgumentMapping("--min-p", ArgType.Float)
		},
		{
			"min-p",
			new ArgumentMapping("--min-p", ArgType.Float)
		},
		{
			"typicalp",
			new ArgumentMapping("--typical-p", ArgType.Float)
		},
		{
			"typical-p",
			new ArgumentMapping("--typical-p", ArgType.Float)
		},
		{
			"repeatpenalty",
			new ArgumentMapping("--repeat-penalty", ArgType.Float)
		},
		{
			"repeat-penalty",
			new ArgumentMapping("--repeat-penalty", ArgType.Float)
		},
		{
			"repeatlastn",
			new ArgumentMapping("--repeat-last-n", ArgType.Int)
		},
		{
			"repeat-last-n",
			new ArgumentMapping("--repeat-last-n", ArgType.Int)
		},
		{
			"n",
			new ArgumentMapping("-n", ArgType.Int)
		},
		{
			"mirostateeta",
			new ArgumentMapping("--mirostat-ent", ArgType.Float)
		},
		{
			"mirostat-ent",
			new ArgumentMapping("--mirostat-ent", ArgType.Float)
		},
		{
			"mirostattau",
			new ArgumentMapping("-mt", ArgType.Float)
		},
		{
			"mirostat-tau",
			new ArgumentMapping("-mt", ArgType.Float)
		},
		{
			"mirostat-learn-rate",
			new ArgumentMapping("--mirostat-lr", ArgType.Float)
		},
		{
			"mlr",
			new ArgumentMapping("-mlr", ArgType.Float)
		},
		{
			"mirostateeta2",
			new ArgumentMapping("--mirostat-lr", ArgType.Float)
		},
		{
			"drymultiplier",
			new ArgumentMapping("--dry-multiplier", ArgType.Float)
		},
		{
			"dry-multiplier",
			new ArgumentMapping("--dry-multiplier", ArgType.Float)
		},
		{
			"drybase",
			new ArgumentMapping("--dry-base", ArgType.Float)
		},
		{
			"dry-base",
			new ArgumentMapping("--dry-base", ArgType.Float)
		},
		{
			"dynatempstddev",
			new ArgumentMapping("--dynatemp-range", ArgType.Float)
		},
		{
			"dynatemp-range",
			new ArgumentMapping("--dynatemp-range", ArgType.Float)
		},
		{
			"xtcprobability",
			new ArgumentMapping("--xtc-probability", ArgType.Float)
		},
		{
			"xtc-probability",
			new ArgumentMapping("--xtc-probability", ArgType.Float)
		},
		{
			"xtcthreshold",
			new ArgumentMapping("--xtc-threshold", ArgType.Float)
		},
		{
			"xtc-threshold",
			new ArgumentMapping("--xtc-threshold", ArgType.Float)
		},
		{
			"predictcount",
			new ArgumentMapping("--predict", ArgType.Int)
		},
		{
			"predict",
			new ArgumentMapping("--predict", ArgType.Int)
		},
		{
			"specdraftgpulayers",
			new ArgumentMapping("--ngld", ArgType.String)
		},
		{
			"ngld",
			new ArgumentMapping("--ngld", ArgType.String)
		},
		{
			"spec-draft-device",
			new ArgumentMapping("--spec-draft-device", ArgType.String)
		},
		{
			"specdraftnmax",
			new ArgumentMapping("--spec-draft-n-max", ArgType.Int)
		},
		{
			"spec-draft-n-max",
			new ArgumentMapping("--spec-draft-n-max", ArgType.Int)
		},
		{
			"specdraftnmin",
			new ArgumentMapping("--spec-draft-n-min", ArgType.Int)
		},
		{
			"spec-draft-n-min",
			new ArgumentMapping("--spec-draft-n-min", ArgType.Int)
		},
		{
			"specdraftpsplit",
			new ArgumentMapping("--spec-draft-p-split", ArgType.Float)
		},
		{
			"spec-draft-p-split",
			new ArgumentMapping("--spec-draft-p-split", ArgType.Float)
		},
		{
			"specdraftpmin",
			new ArgumentMapping("--spec-draft-p-min", ArgType.Float)
		},
		{
			"spec-draft-p-min",
			new ArgumentMapping("--spec-draft-p-min", ArgType.Float)
		},
		{
			"ropefreqbase",
			new ArgumentMapping("--rope-freq-base", ArgType.Float)
		},
		{
			"rope-frequency-base",
			new ArgumentMapping("--rope-freq-base", ArgType.Float)
		},
		{
			"ropefreqscale",
			new ArgumentMapping("--rope-freq-scale", ArgType.Float)
		},
		{
			"rope-frequency-scale",
			new ArgumentMapping("--rope-freq-scale", ArgType.Float)
		},
		{
			"yarnoriginalcontext",
			new ArgumentMapping("--yarn-orig-ctx", ArgType.Int)
		},
		{
			"yarn-orig-ctx",
			new ArgumentMapping("--yarn-orig-ctx", ArgType.Int)
		},
		{
			"yarnextfactor",
			new ArgumentMapping("--yarn-ext-factor", ArgType.Float)
		},
		{
			"yarn-ext-factor",
			new ArgumentMapping("--yarn-ext-factor", ArgType.Float)
		},
		{
			"yarnattnfactor",
			new ArgumentMapping("--yarn-attn-factor", ArgType.Float)
		},
		{
			"yarn-attn-factor",
			new ArgumentMapping("--yarn-attn-factor", ArgType.Float)
		},
		{
			"yarnbetafast",
			new ArgumentMapping("--yarn-beta-fast", ArgType.Float)
		},
		{
			"yarn-beta-fast",
			new ArgumentMapping("--yarn-beta-fast", ArgType.Float)
		},
		{
			"yarbetaslow",
			new ArgumentMapping("--yarn-beta-slow", ArgType.Float)
		},
		{
			"yarn-beta-slow",
			new ArgumentMapping("--yarn-beta-slow", ArgType.Float)
		},
		{
			"embeddingmode",
			new ArgumentMapping("--embedding", ArgType.Flag)
		},
		{
			"embedding",
			new ArgumentMapping("--embedding", ArgType.Flag)
		},
		{
			"poolingtype",
			new ArgumentMapping("--pooling", ArgType.String)
		},
		{
			"pooling",
			new ArgumentMapping("--pooling", ArgType.String)
		},
		{
			"numa",
			new ArgumentMapping("--numa", ArgType.BoolOnOff)
		},
		{
			"no-ui",
			new ArgumentMapping("--no-ui", ArgType.Flag)
		},
		{
			"noui",
			new ArgumentMapping("--no-ui", ArgType.Flag)
		},
		{
			"speculativedecoding",
			new ArgumentMapping("--speculative", ArgType.BoolOnOff)
		},
		{
			"speculative",
			new ArgumentMapping("--speculative", ArgType.BoolOnOff)
		},
		{
			"spectype",
			new ArgumentMapping("--spec-type", ArgType.String)
		},
		{
			"spec-type",
			new ArgumentMapping("--spec-type", ArgType.String)
		},
		{
			"jinja",
			new ArgumentMapping("--jinja", ArgType.Flag)
		},
		{
			"no-jinja",
			new ArgumentMapping("--no-jinja", ArgType.Flag)
		},
		{
			"ropescaling",
			new ArgumentMapping("--rope-scaling", ArgType.String)
		},
		{
			"rope-scaling",
			new ArgumentMapping("--rope-scaling", ArgType.String)
		},
		{
			"mmproj",
			new ArgumentMapping("--mmproj", ArgType.String)
		},
		{
			"mmprojpath",
			new ArgumentMapping("--mmproj", ArgType.String)
		},
		{
			"md",
			new ArgumentMapping("-md", ArgType.String)
		},
		{
			"draftmodel",
			new ArgumentMapping("-md", ArgType.String)
		},
		{
			"draft-model",
			new ArgumentMapping("-md", ArgType.String)
		},
		{
			"draftmodelpath",
			new ArgumentMapping("-md", ArgType.String)
		}
	};

	public static string NormalizeSpecialCharacters(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return input;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (char.IsUpper(c) && i > 0)
			{
				stringBuilder.Append('-');
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
			else if (c == '_')
			{
				stringBuilder.Append('-');
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public static (Dictionary<string, string> CustomArgs, HashSet<string> CustomFlags) ParseArguments(Dictionary<string, string> customArguments)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (customArguments == null || customArguments.Count == 0)
		{
			return (CustomArgs: dictionary, CustomFlags: hashSet);
		}
		foreach (KeyValuePair<string, string> customArgument in customArguments)
		{
			customArgument.Deconstruct(out var key, out var value);
			string input = key;
			string value2 = value;
			string canonicalName = GetCanonicalName(NormalizeSpecialCharacters(input));
			if (string.IsNullOrWhiteSpace(value2))
			{
				hashSet.Add(canonicalName);
			}
			else
			{
				dictionary[canonicalName] = value2;
			}
		}
		return (CustomArgs: dictionary, CustomFlags: hashSet);
	}

	private static string GetCanonicalName(string normalized)
	{
		if (KnownArgumentsLookup.TryGetValue(normalized, out ArgumentMapping value))
		{
			return value.Canonical;
		}
		if (normalized.StartsWith("-"))
		{
			return normalized;
		}
		return "--" + normalized;
	}

	public static List<string> GetArgumentValues(Dictionary<string, string> customArguments, string canonicalName)
	{
		List<string> list = new List<string>();
		if (customArguments == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, string> customArgument in customArguments)
		{
			customArgument.Deconstruct(out var key, out var value);
			string input = key;
			string text = value;
			string key2 = NormalizeSpecialCharacters(input);
			ArgumentMapping valueOrDefault = KnownArgumentsLookup.GetValueOrDefault(key2);
			if (valueOrDefault != null && string.Equals(valueOrDefault.Canonical, canonicalName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	public static bool GetArgumentFlags(Dictionary<string, string> customArguments, string canonicalName)
	{
		if (customArguments == null)
		{
			return false;
		}
		foreach (string key2 in customArguments.Keys)
		{
			string key = NormalizeSpecialCharacters(key2);
			ArgumentMapping valueOrDefault = KnownArgumentsLookup.GetValueOrDefault(key);
			if (valueOrDefault != null && string.Equals(valueOrDefault.Canonical, canonicalName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static ArgType? GetArgType(string key)
	{
		string key2 = NormalizeSpecialCharacters(key);
		return KnownArgumentsLookup.GetValueOrDefault(key2)?.ArgType;
	}

	public static bool ValidateArgumentValue(string key, string? value)
	{
		return ValidateValue(GetArgType(key), value);
	}

	private static bool ValidateValue(ArgType? argType, string? value)
	{
		switch (argType)
		{
		case ArgType.Int:
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			int result2;
			return int.TryParse(value, out result2);
		}
		case ArgType.Float:
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			float result;
			return float.TryParse(value, out result);
		}
		case ArgType.BoolOnOff:
		{
			string text = (string.IsNullOrEmpty(value) ? "" : value.ToLowerInvariant());
			if (text == "on" || text == "off")
			{
				return true;
			}
			return false;
		}
		case ArgType.Flag:
			return true;
		default:
			return true;
		}
	}
}
