namespace LlamaStudio.Core.Models;

/// <summary>
/// Serializable snapshot of CLI flags for a specific version.
/// Used for comparison between versions.
/// </summary>
public class CliFlagSnapshot
{
    public string Version { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public List<SerializedCliFlag> Flags { get; set; } = new();

    public LlamaHelpInfo ToHelpInfo()
    {
        var flags = new Dictionary<string, CliFlagInfo>(StringComparer.OrdinalIgnoreCase);
        var supportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in Flags)
        {
            var info = new CliFlagInfo
            {
                Name = f.Name,
                ShortName = f.ShortName,
                Description = f.Description,
                TakesValue = f.TakesValue,
                DefaultValue = f.DefaultValue,
            };
            flags[f.Name] = info;
            supportedFlags.Add(f.Name);

            if (!string.IsNullOrEmpty(f.ShortName))
            {
                supportedFlags.Add(f.ShortName);
                if (!flags.ContainsKey(f.ShortName))
                    flags[f.ShortName] = new CliFlagInfo { Name = f.ShortName, Description = f.Description };
            }
        }

        return new LlamaHelpInfo { SupportedFlags = supportedFlags, Flags = flags };
    }

    public static CliFlagSnapshot FromHelpInfo(LlamaHelpInfo helpInfo, string version)
    {
        var serialized = new List<SerializedCliFlag>();
        foreach (var kvp in helpInfo.Flags)
        {
            serialized.Add(new SerializedCliFlag
            {
                Name = kvp.Value.Name,
                ShortName = kvp.Value.ShortName,
                Description = kvp.Value.Description,
                TakesValue = kvp.Value.TakesValue,
                DefaultValue = kvp.Value.DefaultValue,
            });
        }

        return new CliFlagSnapshot
        {
            Version = version,
            CapturedAt = DateTime.UtcNow,
            Flags = serialized,
        };
    }
}

/// <summary>
/// Serializable representation of a CLI flag.
/// </summary>
public class SerializedCliFlag
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool TakesValue { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
}
