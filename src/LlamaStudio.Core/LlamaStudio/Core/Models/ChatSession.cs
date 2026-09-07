using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class ChatSession
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

	[JsonPropertyName("name")]
	public string Name { get; set; } = "New Chat";

	[JsonPropertyName("systemPrompt")]
	public string SystemPrompt { get; set; } = string.Empty;

	[JsonPropertyName("messages")]
	public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[JsonPropertyName("updatedAt")]
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	[JsonPropertyName("temperature")]
	public float Temperature { get; set; } = 0.8f;

	[JsonPropertyName("topP")]
	public float TopP { get; set; } = 0.95f;

	[JsonPropertyName("topK")]
	public int TopK { get; set; } = 40;

	[JsonPropertyName("minP")]
	public float MinP { get; set; } = 0.05f;

	[JsonPropertyName("repeatPenalty")]
	public float RepeatPenalty { get; set; } = 1f;

	[JsonPropertyName("presencePenalty")]
	public float PresencePenalty { get; set; }

	[JsonPropertyName("frequencyPenalty")]
	public float FrequencyPenalty { get; set; }

	[JsonPropertyName("seed")]
	public int Seed { get; set; } = -1;

	[JsonPropertyName("maxTokens")]
	public int MaxTokens { get; set; } = -1;

	[JsonPropertyName("stopSequences")]
	public List<string> StopSequences { get; set; } = new List<string>();

	[JsonPropertyName("mcpToolsEnabled")]
	public bool McpToolsEnabled { get; set; } = true;

	[JsonPropertyName("hideReasoning")]
	public bool HideReasoning { get; set; } = true;

	[JsonPropertyName("order")]
	public int Order { get; set; }

	public void AddMessage(ChatMessage message)
	{
		Messages.Add(message);
		UpdatedAt = DateTime.Now;
	}

	public string GetDisplayName()
	{
		ChatMessage chatMessage = Messages.FirstOrDefault((ChatMessage m) => m.Role == ChatRole.User);
		if (chatMessage != null)
		{
			if (chatMessage.Content.Length <= 40)
			{
				return chatMessage.Content.Trim();
			}
			return chatMessage.Content.Substring(0, 40).Trim() + "...";
		}
		return Name;
	}
}
