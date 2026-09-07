using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Infrastructure.Mcp;

namespace LlamaStudio.Infrastructure.Chat;

public class ChatService : IChatService
{
	private readonly ILogService _log;

	public ChatService(ILogService log)
	{
		_log = log;
	}

	public async Task SendChatAsync(string host, int port, string systemPrompt, List<ChatMessage> history, string userMessage, List<string> imageAttachments, bool mcpToolsEnabled, List<McpToolDefinition>? tools, float temperature, float topP, int topK, float minP, float repeatPenalty, float presencePenalty, float frequencyPenalty, int seed, int maxTokens, List<string> stopSequences, Action<string> onToken, Action<ToolCall> onToolCall, Action<string>? onReasoning = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		string baseUrl = $"http://{host}:{port}";
		string requestBody = BuildRequestBody((imageAttachments.Count > 0) ? BuildMessagesWithImages(systemPrompt, history, userMessage, imageAttachments) : BuildMessages(systemPrompt, history, userMessage), mcpToolsEnabled, tools, temperature, topP, topK, minP, repeatPenalty, presencePenalty, frequencyPenalty, seed, maxTokens, stopSequences, stream: true);
		using HttpClient client = CreateClient();
		StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		_log.Information($"[CHAT] POST {baseUrl}/v1/chat/completions, body len={requestBody.Length}", "ChatService");
		_log.Information("[CHAT] Request body: " + requestBody.Substring(0, Math.Min(500, requestBody.Length)), "ChatService");
		try
		{
			HttpResponseMessage response = await client.PostAsync(baseUrl + "/v1/chat/completions", content, cancellationToken);
			_log.Information($"[CHAT] HTTP response: {response.StatusCode} {response.ReasonPhrase}", "ChatService");
			if (!response.IsSuccessStatusCode)
			{
				string errBody = await response.Content.ReadAsStringAsync();
				_log.Error($"[CHAT] HTTP error {response.StatusCode}: {errBody}", "ChatService");
				if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
				{
					throw new Exception($"Server returned {response.StatusCode}: {errBody}");
				}
				int retries = 3;
				for (int i = 0; i < retries; i++)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						throw new OperationCanceledException();
					}
					_log.Information($"[CHAT] Model loading, retry {i + 1}/{retries} in 5s...", "ChatService");
					await Task.Delay(5000, cancellationToken);
					if (cancellationToken.IsCancellationRequested)
					{
						throw new OperationCanceledException();
					}
					response = await client.PostAsync(baseUrl + "/v1/chat/completions", content, cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						break;
					}
					errBody = await response.Content.ReadAsStringAsync();
					_log.Information($"[CHAT] Retry {i + 1}/{retries} still {response.StatusCode}", "ChatService");
				}
				if (!response.IsSuccessStatusCode)
				{
					throw new Exception("Server is loading the model. Please wait and try again. (" + errBody + ")");
				}
			}
			await ParseSseStream(await response.Content.ReadAsStreamAsync(cancellationToken), onToken, onToolCall, onReasoning, cancellationToken);
			_log.Information("[CHAT] SendChatAsync completed successfully", "ChatService");
		}
		catch (Exception ex)
		{
			_log.Error(ex, "[CHAT] SendChatAsync exception: " + ex.Message, "ChatService");
			throw;
		}
	}

	public async Task<ChatMessage?> SendChatWithToolsAsync(string host, int port, string systemPrompt, List<ChatMessage> history, string userMessage, List<string> imageAttachments, IMcpToolsService mcpTools, float temperature, float topP, int topK, float minP, float repeatPenalty, float presencePenalty, float frequencyPenalty, int seed, int maxTokens, List<string> stopSequences, Action<string> onToken, Action<ChatMessage> onMessage, CancellationToken cancellationToken = default(CancellationToken))
	{
		string baseUrl = $"http://{host}:{port}";
		List<McpToolDefinition> availableTools = mcpTools.GetAvailableTools();
		List<OpenAiMessage> messages = ((imageAttachments.Count > 0) ? BuildMessagesWithImages(systemPrompt, history, userMessage, imageAttachments) : BuildMessages(systemPrompt, history, userMessage));
		using HttpClient client = CreateClient();
		HashSet<string> seenToolCalls = new HashSet<string>();
		for (int round = 0; round < 5; round++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string roundContent = "";
			List<ToolCall> roundToolCalls = new List<ToolCall>();
			Dictionary<int, (string Id, StringBuilder Name, StringBuilder Args)> toolCallBuffers = new Dictionary<int, (string, StringBuilder, StringBuilder)>();
			string requestBody = BuildRequestBody(messages, includeTools: true, availableTools, temperature, topP, topK, minP, repeatPenalty, presencePenalty, frequencyPenalty, seed, maxTokens, stopSequences, stream: true);
			_log.Information("[CHAT-TOOLS] Request body (first 800): " + requestBody.Substring(0, Math.Min(800, requestBody.Length)), "ChatService");
			StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await client.PostAsync(baseUrl + "/v1/chat/completions", content, cancellationToken);
			response.EnsureSuccessStatusCode();
			Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			_log.Information($"[CHAT-TOOLS] Parsing SSE stream, round={round}", "ChatService");
			StringBuilder reasoningBuffer = new StringBuilder();
			await ParseSseStreamWithTools(stream, delegate(string token)
			{
				roundContent += token;
				onToken(token);
			}, delegate(int index, string id, string toolName, string toolArgs)
			{
				if (!toolCallBuffers.ContainsKey(index))
				{
					toolCallBuffers[index] = ("", new StringBuilder(), new StringBuilder());
				}
				(string, StringBuilder, StringBuilder) part = toolCallBuffers[index];
				if (string.IsNullOrEmpty(part.Item1) && !string.IsNullOrEmpty(id))
				{
					part.Item1 = id;
				}
				if (!string.IsNullOrEmpty(toolName))
				{
					part.Item2.Append(toolName);
				}
				if (!string.IsNullOrEmpty(toolArgs))
				{
					part.Item3.Append(toolArgs);
				}
				toolCallBuffers[index] = (part.Item1, part.Item2, part.Item3);
			}, delegate
			{
				foreach (var (index, buffer) in toolCallBuffers)
				{
					roundToolCalls.Add(new ToolCall
					{
						Id = (buffer.Item1 ?? $"call_{index}"),
						Name = buffer.Item2.ToString(),
						Arguments = buffer.Item3.ToString()
					});
				}
				_log.Information($"[CHAT-TOOLS] Stream done, collected {roundToolCalls.Count} tool calls", "ChatService");
				foreach (ToolCall toolCall in roundToolCalls)
				{
					_log.Information("[CHAT-TOOLS] Tool: " + toolCall.Name + " args=" + toolCall.Arguments, "ChatService");
				}
			}, delegate(string reasoningToken)
			{
				reasoningBuffer.Append(reasoningToken);
			}, cancellationToken);
			_log.Information($"[CHAT-TOOLS] Round {round}: content='{roundContent.Substring(0, Math.Min(100, roundContent.Length))}', toolCalls={roundToolCalls.Count}", "ChatService");
			if (roundToolCalls.Count == 0)
			{
				if (string.IsNullOrWhiteSpace(roundContent))
				{
					break;
				}
				string reasoning = reasoningBuffer.ToString();
				string thinkingContent = null;
				string responseContent = roundContent;
				string thinkingStartTag = "<thinking";
				string thinkingEndTag = "</thinking>";
				int start = roundContent.IndexOf(thinkingStartTag, StringComparison.OrdinalIgnoreCase);
				if (start >= 0)
				{
					int end = roundContent.IndexOf(thinkingEndTag, start, StringComparison.OrdinalIgnoreCase);
					if (end >= 0)
					{
						int length = end - (start + thinkingStartTag.Length);
						thinkingContent = roundContent.Substring(start + thinkingStartTag.Length, length).Trim();
						responseContent = roundContent.Substring(0, start) + roundContent.Substring(end + thinkingEndTag.Length);
						responseContent = responseContent.Trim();
					}
				}
				string finalReasoning = (string.IsNullOrEmpty(reasoning) ? thinkingContent : (string.IsNullOrEmpty(thinkingContent) ? reasoning : (reasoning + "\n" + thinkingContent)));
				_log.Information($"[CHAT-TOOLS] Final response: reasoning_len={finalReasoning?.Length ?? 0}, content_len={responseContent.Length}", "ChatService");
				return new ChatMessage
				{
					Role = ChatRole.Assistant,
					Content = responseContent,
					Reasoning = finalReasoning,
					Timestamp = DateTime.Now
				};
			}
			foreach (ToolCall toolCall in roundToolCalls)
			{
				string toolCallKey = toolCall.Name + ":" + toolCall.Arguments.Substring(0, Math.Min(100, toolCall.Arguments.Length));
				if (seenToolCalls.Contains(toolCallKey))
				{
					_log.Warning("[CHAT-TOOLS] Detected tool loop: " + toolCallKey + ", breaking", "ChatService");
					roundToolCalls.Clear();
					break;
				}
				seenToolCalls.Add(toolCallKey);
			}
			ChatMessage assistantMessage = new ChatMessage
			{
				Role = ChatRole.Assistant,
				Content = roundContent,
				ToolCalls = roundToolCalls,
				Timestamp = DateTime.Now
			};
			onMessage(assistantMessage);
			messages.Add(new OpenAiMessage
			{
				Role = "assistant",
				Content = roundContent,
				ToolCalls = roundToolCalls.Select((ToolCall tc) => new OpenAiToolCall
				{
					Id = tc.Id,
					Name = tc.Name,
					Arguments = tc.Arguments
				}).ToList()
			});
			foreach (ToolCall toolCall in roundToolCalls)
			{
				_log.Information("[CHAT-TOOLS] Executing tool: " + toolCall.Name + " args=" + toolCall.Arguments, "ChatService");
				string toolResult = McpToolsService.CleanToolResult((await mcpTools.ExecuteToolAsync(toolCall.Name, toolCall.Arguments)).Content, toolCall.Name);
				string truncatedResult = ((toolResult.Length > 2000) ? (toolResult.Substring(0, 2000) + $"\n\n... [truncated, total {toolResult.Length} chars]") : toolResult);
				string logPreview = ((truncatedResult.Length > 500) ? (truncatedResult.Substring(0, 500) + "...") : truncatedResult);
				_log.Information($"[CHAT-TOOLS] Tool result ({toolResult.Length} chars, limited to {truncatedResult.Length}): {logPreview}", "ChatService");
				ChatMessage toolResultMessage = new ChatMessage
				{
					Role = ChatRole.Tool,
					Content = truncatedResult,
					ToolCallId = toolCall.Id,
					ToolName = toolCall.Name,
					Timestamp = DateTime.Now
				};
				onMessage(toolResultMessage);
				messages.Add(new OpenAiMessage
				{
					Role = "tool",
					Content = truncatedResult,
					ToolCallId = toolCall.Id,
					ToolName = toolCall.Name
				});
			}
		}
		return null;
	}

	private async Task ParseSseStream(Stream stream, Action<string> onToken, Action<ToolCall> onToolCall, Action<string>? onReasoning, CancellationToken ct)
	{
		using StreamReader reader = new StreamReader(stream);
		StringBuilder buffer = new StringBuilder();
		int tokenCount = 0;
		_log.Information("[CHAT] SSE parse started", "ChatService");
		while (!ct.IsCancellationRequested)
		{
			string line = await reader.ReadLineAsync(ct);
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}
			if (line == "data: [DONE]")
			{
				_log.Information($"[CHAT] SSE [DONE] received, tokens={tokenCount}", "ChatService");
				break;
			}
			if (!line.StartsWith("data: "))
			{
				continue;
			}
			string dataLine = line;
			string json = dataLine.Substring(6, dataLine.Length - 6);
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(json);
				if (!jsonDocument.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() <= 0 || !choices[0].TryGetProperty("delta", out var delta))
				{
					continue;
				}
				if (onReasoning != null && delta.TryGetProperty("reasoning_content", out var reasoningContent) && reasoningContent.ValueKind == JsonValueKind.String)
				{
					string reasoning = reasoningContent.GetString();
					if (!string.IsNullOrEmpty(reasoning))
					{
						onReasoning(reasoning);
					}
				}
				if (delta.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
				{
					string content = contentElement.GetString();
					if (!string.IsNullOrEmpty(content))
					{
						tokenCount++;
						if (content.Contains("<thinking") || content.Contains("</thinking"))
						{
							_log.Debug($"[CHAT] REASONING TOKEN #{tokenCount}: '{content}'", "ChatService");
						}
						else
						{
							_log.Debug($"[CHAT] Token #{tokenCount}: '{content}'", "ChatService");
						}
						buffer.Append(content);
						onToken(content);
					}
				}
				if (!delta.TryGetProperty("tool_calls", out var toolCalls) || toolCalls.GetArrayLength() <= 0)
				{
					continue;
				}
				foreach (JsonElement toolCallElement in toolCalls.EnumerateArray())
				{
					ToolCall toolCall = new ToolCall();
					if (toolCallElement.TryGetProperty("id", out var idElement))
					{
						toolCall.Id = idElement.GetString() ?? toolCall.Id;
					}
					if (toolCallElement.TryGetProperty("function", out var functionElement))
					{
						if (functionElement.TryGetProperty("name", out var nameElement))
						{
							toolCall.Name = nameElement.GetString() ?? string.Empty;
						}
						if (functionElement.TryGetProperty("arguments", out var argumentsElement))
						{
							toolCall.Arguments = argumentsElement.GetString() ?? string.Empty;
						}
					}
					onToolCall(toolCall);
				}
			}
			catch (JsonException)
			{
			}
		}
	}

	private async Task ParseSseStreamWithTools(Stream stream, Action<string> onToken, Action<int, string, string, string> onToolCallPart, Action onAllToolCallsComplete, Action<string>? onReasoning, CancellationToken ct)
	{
		using StreamReader reader = new StreamReader(stream);
		while (!ct.IsCancellationRequested)
		{
			string line = await reader.ReadLineAsync(ct);
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}
			if (line == "data: [DONE]")
			{
				break;
			}
			if (!line.StartsWith("data: "))
			{
				continue;
			}
			string dataLine = line;
			string json = dataLine.Substring(6, dataLine.Length - 6);
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(json);
				if (!jsonDocument.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() <= 0 || !choices[0].TryGetProperty("delta", out var delta))
				{
					continue;
				}
				if (onReasoning != null && delta.TryGetProperty("reasoning_content", out var reasoningContent) && reasoningContent.ValueKind == JsonValueKind.String)
				{
					string reasoning = reasoningContent.GetString();
					if (!string.IsNullOrEmpty(reasoning))
					{
						_log.Information("[CHAT-TOOLS] Reasoning token: '" + reasoning.Substring(0, Math.Min(50, reasoning.Length)) + "'", "ChatService");
						onReasoning(reasoning);
					}
				}
				if (delta.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
				{
					string content = contentElement.GetString();
					if (!string.IsNullOrEmpty(content))
					{
						if (content.Contains("<thinking") || content.Contains("</thinking>"))
						{
							_log.Information("[CHAT-TOOLS] Thinking tag in content: '" + content.Substring(0, Math.Min(100, content.Length)) + "'", "ChatService");
						}
						onToken(content);
					}
				}
				if (!delta.TryGetProperty("tool_calls", out var toolCalls) || toolCalls.GetArrayLength() <= 0)
				{
					continue;
				}
				foreach (JsonElement toolCallElement in toolCalls.EnumerateArray())
				{
					string id = "";
					string name = "";
					string arguments = "";
					int index = 0;
					if (toolCallElement.TryGetProperty("index", out var indexElement))
					{
						index = indexElement.GetInt32();
					}
					if (toolCallElement.TryGetProperty("id", out var idElement))
					{
						id = idElement.GetString() ?? "";
					}
					if (toolCallElement.TryGetProperty("function", out var functionElement))
					{
						if (functionElement.TryGetProperty("name", out var nameElement))
						{
							name = nameElement.GetString() ?? "";
						}
						if (functionElement.TryGetProperty("arguments", out var argumentsElement))
						{
							arguments = argumentsElement.GetString() ?? "";
						}
					}
					onToolCallPart(index, id, name, arguments);
				}
			}
			catch (JsonException)
			{
			}
		}
		onAllToolCallsComplete();
	}

	private static List<OpenAiMessage> BuildMessages(string systemPrompt, List<ChatMessage> history, string userMessage)
	{
		List<OpenAiMessage> messages = new List<OpenAiMessage>();
		if (!string.IsNullOrWhiteSpace(systemPrompt))
		{
			messages.Add(new OpenAiMessage
			{
				Role = "system",
				Content = systemPrompt
			});
		}
		foreach (ChatMessage message in history)
		{
			OpenAiMessage convertedMessage = BuildOpenAiMessage(message);
			messages.Add(convertedMessage);
		}
		messages.Add(new OpenAiMessage
		{
			Role = "user",
			Content = userMessage
		});
		return messages;
	}

	private static List<OpenAiMessage> BuildMessagesWithImages(string systemPrompt, List<ChatMessage> history, string userMessage, List<string> imageBase64)
	{
		List<OpenAiMessage> messages = new List<OpenAiMessage>();
		if (!string.IsNullOrWhiteSpace(systemPrompt))
		{
			messages.Add(new OpenAiMessage
			{
				Role = "system",
				Content = systemPrompt
			});
		}
		foreach (ChatMessage message in history)
		{
			OpenAiMessage convertedMessage = BuildOpenAiMessage(message);
			messages.Add(convertedMessage);
		}
		OpenAiMessage openAiMessage = new OpenAiMessage
		{
			Role = "user"
		};
		if (imageBase64.Count > 0)
		{
			openAiMessage.ContentParts = new List<Dictionary<string, object>>
			{
				new Dictionary<string, object>
				{
					{ "type", "text" },
					{ "text", userMessage }
				}
			};
			foreach (string imageData in imageBase64)
			{
				string dataUri = (imageData.StartsWith("data:") ? imageData : ("data:image/png;base64," + imageData));
				openAiMessage.ContentParts.Add(new Dictionary<string, object>
				{
					{ "type", "image_url" },
					{
						"image_url",
						new Dictionary<string, string> { { "url", dataUri } }
					}
				});
			}
		}
		else
		{
			openAiMessage.Content = userMessage;
		}
		messages.Add(openAiMessage);
		return messages;
	}

	private static OpenAiMessage BuildOpenAiMessage(ChatMessage msg)
	{
		OpenAiMessage openAiMessage = new OpenAiMessage
		{
			Role = msg.RoleName,
			Content = msg.Content
		};
		if (msg.ToolCallId != null)
		{
			openAiMessage.ToolCallId = msg.ToolCallId;
		}
		if (msg.ToolName != null)
		{
			openAiMessage.ToolName = msg.ToolName;
		}
		if (msg.ToolCalls.Count > 0)
		{
			openAiMessage.ToolCalls = msg.ToolCalls.Select((ToolCall tc) => new OpenAiToolCall
			{
				Id = tc.Id,
				Name = tc.Name,
				Arguments = tc.Arguments
			}).ToList();
		}
		if (msg.ImageAttachments != null && msg.ImageAttachments.Count > 0)
		{
			openAiMessage.ContentParts = new List<Dictionary<string, object>>
			{
				new Dictionary<string, object>
				{
					{ "type", "text" },
					{ "text", msg.Content }
				}
			};
			foreach (string imageAttachment in msg.ImageAttachments)
			{
				string dataUri = (imageAttachment.StartsWith("data:") ? imageAttachment : ("data:image/png;base64," + imageAttachment));
				openAiMessage.ContentParts.Add(new Dictionary<string, object>
				{
					{ "type", "image_url" },
					{
						"image_url",
						new Dictionary<string, string> { { "url", dataUri } }
					}
				});
			}
		}
		return openAiMessage;
	}

	private static string BuildRequestBody(List<OpenAiMessage> messages, bool includeTools, List<McpToolDefinition>? tools, float temperature, float topP, int topK, float minP, float repeatPenalty, float presencePenalty, float frequencyPenalty, int seed, int maxTokens, List<string> stopSequences, bool stream)
	{
		return JsonSerializer.Serialize(new
		{
			model = "local-model",
			messages = messages.Select(delegate(OpenAiMessage m)
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object> { { "role", m.Role } };
				if (m.ContentParts != null && m.ContentParts.Count > 0)
				{
					dictionary["content"] = m.ContentParts;
				}
				else
				{
					dictionary["content"] = m.Content ?? "";
				}
				if (m.ToolCallId != null)
				{
					dictionary["tool_call_id"] = m.ToolCallId;
				}
				if (m.ToolName != null)
				{
					dictionary["tool_name"] = m.ToolName;
				}
				if (m.ToolCalls != null && m.ToolCalls.Count > 0)
				{
					dictionary["tool_calls"] = m.ToolCalls.Select((OpenAiToolCall tc) => new
					{
						id = tc.Id,
						type = "function",
						function = new
						{
							name = tc.Name,
							arguments = tc.Arguments
						}
					}).ToList();
				}
				return dictionary;
			}).ToList(),
			temperature = Math.Max(0.01f, temperature),
			top_p = topP,
			top_k = topK,
			min_p = minP,
			repeat_penalty = repeatPenalty,
			presence_penalty = presencePenalty,
			frequency_penalty = frequencyPenalty,
			seed = ((seed >= 0) ? new int?(seed) : ((int?)null)),
			max_tokens = ((maxTokens > 0) ? new int?(maxTokens) : ((int?)null)),
			stop = ((stopSequences.Count > 0) ? stopSequences : null),
			stream = stream,
			tools = ((includeTools && tools != null) ? tools.Select((McpToolDefinition t) => new
			{
				type = "function",
				function = new
				{
					name = t.Name,
					description = t.Description,
					parameters = new
					{
						type = "object",
						properties = Enumerable.ToDictionary(t.Parameters, (KeyValuePair<string, McpToolParameter> p) => p.Key, (KeyValuePair<string, McpToolParameter> p) => new
						{
							type = p.Value.Type,
							description = p.Value.Description
						}),
						required = (from p in t.Parameters
							where p.Value.Required
							select p.Key).ToList()
					}
				}
			}).ToList() : null)
		});
	}

	private static HttpClient CreateClient()
	{
		return new HttpClient
		{
			Timeout = TimeSpan.FromMinutes(10.0),
			DefaultRequestHeaders = 
			{
				Accept = 
				{
					new MediaTypeWithQualityHeaderValue("application/json")
				}
			}
		};
	}
}
