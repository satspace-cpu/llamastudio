namespace LlamaStudio.Core.Models;

public class CriticalChange
{
	public string FlagName { get; set; } = string.Empty;

	public string OldVersion { get; set; } = string.Empty;

	public string NewVersion { get; set; } = string.Empty;

	public string? SuggestedReplacement { get; set; }
}
