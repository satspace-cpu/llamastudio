namespace LlamaStudio.Core.Models;

public class ContentPart
{
	public enum PartType
	{
		Text,
		Code
	}

	public PartType Type { get; set; }

	public string Content { get; set; } = string.Empty;

	public string Language { get; set; } = string.Empty;
}
