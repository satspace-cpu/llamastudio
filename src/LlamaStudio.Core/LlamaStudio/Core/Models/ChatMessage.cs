using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class ChatMessage : INotifyPropertyChanged
{
	private string _content = string.Empty;

	private bool _isStreaming;

	private string? _reasoning;

	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

	[JsonPropertyName("role")]
	public ChatRole Role { get; set; }

	[JsonPropertyName("content")]
	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
			ContentParts = ContentParser.Parse(value);
			OnPropertyChanged("ContentParts");
		}
	}

	[JsonIgnore]
	public List<ContentPart> ContentParts { get; private set; } = new List<ContentPart>();

	[JsonPropertyName("timestamp")]
	public DateTime Timestamp { get; set; } = DateTime.Now;

	[JsonPropertyName("toolCalls")]
	public List<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();

	[JsonPropertyName("toolCallId")]
	public string? ToolCallId { get; set; }

	[JsonPropertyName("toolName")]
	public string? ToolName { get; set; }

	[JsonPropertyName("isStreaming")]
	public bool IsStreaming
	{
		get
		{
			return _isStreaming;
		}
		set
		{
			_isStreaming = value;
			OnPropertyChanged("IsStreaming");
		}
	}

	[JsonPropertyName("imageAttachments")]
	public List<string> ImageAttachments { get; set; } = new List<string>();

	[JsonPropertyName("attachedFiles")]
	public List<string> AttachedFiles { get; set; } = new List<string>();

	[JsonIgnore]
	public bool IsBookmarked { get; set; }

	[JsonIgnore]
	public bool IsToolCollapsed { get; set; } = true;

	[JsonIgnore]
	public string? Reasoning
	{
		get
		{
			return _reasoning;
		}
		set
		{
			_reasoning = value;
			OnPropertyChanged("Reasoning");
		}
	}

	[JsonIgnore]
	public bool IsReasoningCollapsed { get; set; }

	public string RoleName => Role switch
	{
		ChatRole.System => "system", 
		ChatRole.User => "user", 
		ChatRole.Assistant => "assistant", 
		ChatRole.Tool => "tool", 
		_ => "assistant", 
	};

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
