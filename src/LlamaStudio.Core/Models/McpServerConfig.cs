using System.Text.Json.Serialization;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

/// <summary>
/// Configuration for an external MCP server (stdio or SSE transport).
/// </summary>
public class McpServerConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("transportType")]
    public McpTransportType TransportType { get; set; } = McpTransportType.Stdio;

    // Stdio transport
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();

    [JsonPropertyName("env")]
    public Dictionary<string, string> Env { get; set; } = new();

    // SSE transport
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; }

    [JsonPropertyName("toolsCount")]
    public int ToolsCount { get; set; }
}
