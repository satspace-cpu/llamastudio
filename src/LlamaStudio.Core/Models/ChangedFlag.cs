namespace LlamaStudio.Core.Models;

/// <summary>
/// A flag that exists in both old and new version but has changed description or default value.
/// </summary>
public class ChangedFlag
{
    public string FlagName { get; set; } = string.Empty;
    public string OldDescription { get; set; } = string.Empty;
    public string NewDescription { get; set; } = string.Empty;
    public string OldDefault { get; set; } = string.Empty;
    public string NewDefault { get; set; } = string.Empty;
}
