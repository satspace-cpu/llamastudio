using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class ChatSession
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Chat";

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

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
    public float RepeatPenalty { get; set; } = 1.0f;

    [JsonPropertyName("presencePenalty")]
    public float PresencePenalty { get; set; }

    [JsonPropertyName("frequencyPenalty")]
    public float FrequencyPenalty { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; } = -1;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = -1;

    [JsonPropertyName("stopSequences")]
    public List<string> StopSequences { get; set; } = new();

    [JsonPropertyName("mcpToolsEnabled")]
    public bool McpToolsEnabled { get; set; } = true; // Enabled by default

    [JsonPropertyName("hideReasoning")]
    public bool HideReasoning { get; set; } = true; // Hidden by default

    [JsonPropertyName("order")]
    public int Order { get; set; }

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        UpdatedAt = DateTime.Now;
    }

    public string GetDisplayName()
    {
        var userMsg = Messages.FirstOrDefault(m => m.Role == ChatRole.User);
        if (userMsg != null)
        {
            var truncated = userMsg.Content.Length > 40
                ? userMsg.Content[..40].Trim() + "..."
                : userMsg.Content.Trim();
            return truncated;
        }
        return Name;
    }
}
