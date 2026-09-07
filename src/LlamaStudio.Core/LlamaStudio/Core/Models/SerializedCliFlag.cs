namespace LlamaStudio.Core.Models;

public class SerializedCliFlag
{
	public string Name { get; set; } = string.Empty;

	public string? ShortName { get; set; }

	public string Description { get; set; } = string.Empty;

	public bool TakesValue { get; set; }

	public string DefaultValue { get; set; } = string.Empty;
}
