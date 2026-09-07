using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class McpToolDefinition
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("parameters")]
	public Dictionary<string, McpToolParameter> Parameters { get; set; } = new Dictionary<string, McpToolParameter>();
}
