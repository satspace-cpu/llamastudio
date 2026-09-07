using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class McpToolResult
{
	[JsonPropertyName("toolName")]
	public string ToolName { get; set; } = string.Empty;

	[JsonPropertyName("toolCallId")]
	public string ToolCallId { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;

	[JsonPropertyName("isError")]
	public bool IsError { get; set; }
}
