using System;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class ToolCall
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("arguments")]
	public string Arguments { get; set; } = string.Empty;
}
