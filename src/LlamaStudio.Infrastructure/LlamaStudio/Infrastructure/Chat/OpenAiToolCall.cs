using System.Text.Json.Serialization;

namespace LlamaStudio.Infrastructure.Chat;

internal class OpenAiToolCall
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("arguments")]
	public string Arguments { get; set; } = "";
}
