using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LlamaStudio.Infrastructure.Chat;

internal class OpenAiMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = "";

	[JsonPropertyName("content")]
	public string? Content { get; set; }

	[JsonPropertyName("content_parts")]
	public List<Dictionary<string, object>>? ContentParts { get; set; }

	[JsonPropertyName("tool_call_id")]
	public string? ToolCallId { get; set; }

	[JsonPropertyName("tool_name")]
	public string? ToolName { get; set; }

	[JsonPropertyName("tool_calls")]
	public List<OpenAiToolCall>? ToolCalls { get; set; }
}
