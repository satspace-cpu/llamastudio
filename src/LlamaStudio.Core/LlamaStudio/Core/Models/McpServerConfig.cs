using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

public class McpServerConfig
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("transportType")]
	public McpTransportType TransportType { get; set; }

	[JsonPropertyName("command")]
	public string Command { get; set; } = string.Empty;

	[JsonPropertyName("args")]
	public List<string> Args { get; set; } = new List<string>();

	[JsonPropertyName("env")]
	public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();

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
