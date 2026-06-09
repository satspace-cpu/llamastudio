namespace LlamaStudio.Core.Models;

/// <summary>
/// Structured information about a single CLI flag parsed from --help output.
/// </summary>
public class CliFlagInfo
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool TakesValue { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
}
