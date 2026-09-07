using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class McpToolParameter
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "string";

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("required")]
	public bool Required { get; set; }
}
