using System;
using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class CliFlagSnapshot
{
	public string Version { get; set; } = string.Empty;

	public DateTime CapturedAt { get; set; }

	public List<SerializedCliFlag> Flags { get; set; } = new List<SerializedCliFlag>();

	public LlamaHelpInfo ToHelpInfo()
	{
		Dictionary<string, CliFlagInfo> dictionary = new Dictionary<string, CliFlagInfo>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SerializedCliFlag flag in Flags)
		{
			CliFlagInfo value = new CliFlagInfo
			{
				Name = flag.Name,
				ShortName = flag.ShortName,
				Description = flag.Description,
				TakesValue = flag.TakesValue,
				DefaultValue = flag.DefaultValue
			};
			dictionary[flag.Name] = value;
			hashSet.Add(flag.Name);
			if (!string.IsNullOrEmpty(flag.ShortName))
			{
				hashSet.Add(flag.ShortName);
				if (!dictionary.ContainsKey(flag.ShortName))
				{
					dictionary[flag.ShortName] = new CliFlagInfo
					{
						Name = flag.ShortName,
						Description = flag.Description
					};
				}
			}
		}
		return new LlamaHelpInfo
		{
			SupportedFlags = hashSet,
			Flags = dictionary
		};
	}

	public static CliFlagSnapshot FromHelpInfo(LlamaHelpInfo helpInfo, string version)
	{
		List<SerializedCliFlag> list = new List<SerializedCliFlag>();
		foreach (KeyValuePair<string, CliFlagInfo> flag in helpInfo.Flags)
		{
			list.Add(new SerializedCliFlag
			{
				Name = flag.Value.Name,
				ShortName = flag.Value.ShortName,
				Description = flag.Value.Description,
				TakesValue = flag.Value.TakesValue,
				DefaultValue = flag.Value.DefaultValue
			});
		}
		return new CliFlagSnapshot
		{
			Version = version,
			CapturedAt = DateTime.UtcNow,
			Flags = list
		};
	}
}
