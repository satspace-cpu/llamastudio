using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaStudio.Core.Models;

public class LlamaHelpInfo
{
	public HashSet<string> SupportedFlags { get; set; } = new HashSet<string>();

	public string RawOutput { get; set; } = string.Empty;

	public Dictionary<string, CliFlagInfo> Flags { get; set; } = new Dictionary<string, CliFlagInfo>(StringComparer.OrdinalIgnoreCase);

	public bool IsSupported(string flag)
	{
		if (string.IsNullOrWhiteSpace(flag))
		{
			return true;
		}
		string normalized = flag.TrimStart('-');
		foreach (string supportedFlag in SupportedFlags)
		{
			if (supportedFlag.TrimStart('-').Equals(normalized, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
		dictionary.Add("cache-ram", new string[2] { "cram", "cache-ram" });
		dictionary.Add("cache-reuse", new string[1] { "cache-reuse" });
		dictionary.Add("cache-type-k", new string[2] { "ctk", "cache-type-k" });
		dictionary.Add("cache-type-v", new string[2] { "ctv", "cache-type-v" });
		dictionary.Add("flash-attn", new string[3] { "fa", "flash-attn", "flash-attention" });
		dictionary.Add("gpu-layers", new string[2] { "ngl", "gpu-layers" });
		dictionary.Add("ctx-size", new string[2] { "c", "ctx-size" });
		foreach (var (_, source) in dictionary)
		{
			if (!source.Any((string a) => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			foreach (string supportedFlag2 in SupportedFlags)
			{
				string sNormalized2 = supportedFlag2.TrimStart('-');
				if (source.Any((string a) => sNormalized2.Equals(a, StringComparison.OrdinalIgnoreCase)))
				{
					return true;
				}
			}
		}
		return false;
	}

	public List<string> GetUnsupported(IEnumerable<string> flags)
	{
		List<string> list = new List<string>();
		foreach (string rawFlag in flags)
		{
			string flag = rawFlag.Split(' ')[0];
			if (!IsSupported(flag))
			{
				list.Add(flag);
			}
		}
		return list;
	}
}
