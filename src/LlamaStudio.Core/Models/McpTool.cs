using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class McpToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, McpToolParameter> Parameters { get; set; } = new();
}

public class McpToolParameter
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

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
