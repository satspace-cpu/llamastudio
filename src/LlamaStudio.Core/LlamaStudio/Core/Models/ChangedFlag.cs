namespace LlamaStudio.Core.Models;

public class ChangedFlag
{
	public string FlagName { get; set; } = string.Empty;

	public string OldDescription { get; set; } = string.Empty;

	public string NewDescription { get; set; } = string.Empty;

	public string OldDefault { get; set; } = string.Empty;

	public string NewDefault { get; set; } = string.Empty;
}
