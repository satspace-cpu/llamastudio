using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool
}

public class ChatMessage : INotifyPropertyChanged
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("role")]
    public ChatRole Role { get; set; }

    private string _content = string.Empty;
    [JsonPropertyName("content")]
    public string Content
    {
        get => _content;
        set 
        { 
            _content = value; 
            OnPropertyChanged();
            // Re-parse content parts when content changes
            ContentParts = ContentParser.Parse(value);
            OnPropertyChanged(nameof(ContentParts));
        }
    }

    /// <summary>Parsed content parts (text and code blocks)</summary>
    [JsonIgnore]
    public List<ContentPart> ContentParts { get; private set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("toolCalls")]
    public List<ToolCall> ToolCalls { get; set; } = new();

    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; set; }

    private bool _isStreaming;
    [JsonPropertyName("isStreaming")]
    public bool IsStreaming
    {
        get => _isStreaming;
        set { _isStreaming = value; OnPropertyChanged(); }
    }

    /// <summary>Base64 encoded image attachments for multi-modal (vision) models</summary>
    [JsonPropertyName("imageAttachments")]
    public List<string> ImageAttachments { get; set; } = new();

    /// <summary>Attached file paths</summary>
    [JsonPropertyName("attachedFiles")]
    public List<string> AttachedFiles { get; set; } = new();

    /// <summary>User bookmark flag (not persisted to session JSON)</summary>
    [JsonIgnore]
    public bool IsBookmarked { get; set; }

    /// <summary>Tool messages are collapsed by default — not persisted</summary>
    [JsonIgnore]
    public bool IsToolCollapsed { get; set; } = true;

    private string? _reasoning;
    /// <summary>Reasoning/thinking content extracted from <think> tags (not persisted)</summary>
    [JsonIgnore]
    public string? Reasoning
    {
        get => _reasoning;
        set { _reasoning = value; OnPropertyChanged(); }
    }

    /// <summary>Reasoning block collapsed state — not persisted</summary>
    [JsonIgnore]
    public bool IsReasoningCollapsed { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string RoleName => Role switch
    {
        ChatRole.System => "system",
        ChatRole.User => "user",
        ChatRole.Assistant => "assistant",
        ChatRole.Tool => "tool",
        _ => "assistant"
    };
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
