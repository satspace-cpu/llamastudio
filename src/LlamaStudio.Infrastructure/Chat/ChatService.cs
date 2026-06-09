using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using LlamaStudio.Infrastructure.Mcp;

namespace LlamaStudio.Infrastructure.Chat;

public class ChatService : IChatService
{
    readonly ILogService _log;

    public ChatService(ILogService log)
    {
        _log = log;
    }

    public async Task SendChatAsync(
        string host,
        int port,
        string systemPrompt,
        List<ChatMessage> history,
        string userMessage,
        List<string> imageAttachments,
        bool mcpToolsEnabled,
        List<McpToolDefinition>? tools,
        float temperature,
        float topP,
        int topK,
        float minP,
        float repeatPenalty,
        float presencePenalty,
        float frequencyPenalty,
        int seed,
        int maxTokens,
        List<string> stopSequences,
        Action<string> onToken,
        Action<ToolCall> onToolCall,
        Action<string>? onReasoning = null,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = $"http://{host}:{port}";
        var messages = imageAttachments.Count > 0
            ? BuildMessagesWithImages(systemPrompt, history, userMessage, imageAttachments)
            : BuildMessages(systemPrompt, history, userMessage);
        var requestBody = BuildRequestBody(messages, mcpToolsEnabled, tools, temperature, topP, topK, minP,
            repeatPenalty, presencePenalty, frequencyPenalty, seed, maxTokens, stopSequences, stream: true);

        using var client = CreateClient();
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        _log.Information($"[CHAT] POST {baseUrl}/v1/chat/completions, body len={requestBody.Length}", "ChatService");
        _log.Information($"[CHAT] Request body: {requestBody.Substring(0, Math.Min(500, requestBody.Length))}", "ChatService");
        
        try
        {
            var response = await client.PostAsync($"{baseUrl}/v1/chat/completions", content, cancellationToken);
            _log.Information($"[CHAT] HTTP response: {(int)response.StatusCode} {response.ReasonPhrase}", "ChatService");

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                _log.Error($"[CHAT] HTTP error {(int)response.StatusCode}: {errBody}", "ChatService");

                // 503 = model loading, retry up to 3 times with 5s delay
                if ((int)response.StatusCode == 503)
                {
                    var retries = 3;
                    for (int i = 0; i < retries; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        _log.Information($"[CHAT] Model loading, retry {i + 1}/{retries} in 5s...", "ChatService");
                        await Task.Delay(5000, cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();

                        response = await client.PostAsync($"{baseUrl}/v1/chat/completions", content, cancellationToken);
                        if (response.IsSuccessStatusCode)
                            break;

                        errBody = await response.Content.ReadAsStringAsync();
                        _log.Information($"[CHAT] Retry {i + 1}/{retries} still {(int)response.StatusCode}", "ChatService");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Server is loading the model. Please wait and try again. ({errBody})");
                    }
                }
                else
                {
                    throw new Exception($"Server returned {(int)response.StatusCode}: {errBody}");
                }
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await ParseSseStream(stream, onToken, onToolCall, onReasoning, cancellationToken);
            _log.Information($"[CHAT] SendChatAsync completed successfully", "ChatService");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[CHAT] SendChatAsync exception: {ex.Message}", "ChatService");
            throw;
        }
    }

    public async Task<ChatMessage?> SendChatWithToolsAsync(
        string host,
        int port,
        string systemPrompt,
        List<ChatMessage> history,
        string userMessage,
        List<string> imageAttachments,
        IMcpToolsService mcpTools,
        float temperature,
        float topP,
        int topK,
        float minP,
        float repeatPenalty,
        float presencePenalty,
        float frequencyPenalty,
        int seed,
        int maxTokens,
        List<string> stopSequences,
        Action<string> onToken,
        Action<ChatMessage> onMessage,
        CancellationToken cancellationToken = default)
    {
          const int maxToolRounds = 5;
        var baseUrl = $"http://{host}:{port}";
        var availableTools = mcpTools.GetAvailableTools();
        var messages = imageAttachments.Count > 0
            ? BuildMessagesWithImages(systemPrompt, history, userMessage, imageAttachments)
            : BuildMessages(systemPrompt, history, userMessage);

        using var client = CreateClient();
        var seenToolCalls = new HashSet<string>(); // Detect tool call loops

        for (int round = 0; round < maxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string roundContent = "";
            List<ToolCall> roundToolCalls = new();
            var toolCallBuffers = new Dictionary<int, (string Id, StringBuilder Name, StringBuilder Args)>();

            var requestBody = BuildRequestBody(messages, true, availableTools, temperature, topP, topK, minP,
                repeatPenalty, presencePenalty, frequencyPenalty, seed, maxTokens, stopSequences, stream: true);

            _log.Information($"[CHAT-TOOLS] Request body (first 800): {requestBody.Substring(0, Math.Min(800, requestBody.Length))}", "ChatService");
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/v1/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            _log.Information($"[CHAT-TOOLS] Parsing SSE stream, round={round}", "ChatService");
            var reasoningBuffer2 = new StringBuilder();
            await ParseSseStreamWithTools(stream,
                (token) =>
                {
                    roundContent += token;
                    onToken(token);
                },
                (index, id, toolName, toolArgs) =>
                {
                    if (!toolCallBuffers.ContainsKey(index))
                        toolCallBuffers[index] = ("", new StringBuilder(), new StringBuilder());

                    var buf = toolCallBuffers[index];

                    // Store ID from first chunk
                    if (string.IsNullOrEmpty(buf.Id) && !string.IsNullOrEmpty(id))
                        buf.Id = id;

                    if (!string.IsNullOrEmpty(toolName))
                        buf.Name.Append(toolName);
                    if (!string.IsNullOrEmpty(toolArgs))
                        buf.Args.Append(toolArgs);

                    toolCallBuffers[index] = (buf.Id, buf.Name, buf.Args);
                },
                () =>
                {
                    foreach (var (idx, buf) in toolCallBuffers)
                    {
                        roundToolCalls.Add(new ToolCall
                        {
                            Id = buf.Id ?? $"call_{idx}",
                            Name = buf.Name.ToString(),
                            Arguments = buf.Args.ToString()
                        });
                    }
                    _log.Information($"[CHAT-TOOLS] Stream done, collected {roundToolCalls.Count} tool calls", "ChatService");
                    foreach (var tc in roundToolCalls)
                        _log.Information($"[CHAT-TOOLS] Tool: {tc.Name} args={tc.Arguments}", "ChatService");
                },
                (reasoningToken) =>
                {
                    reasoningBuffer2.Append(reasoningToken);
                },
                cancellationToken);

              _log.Information($"[CHAT-TOOLS] Round {round}: content='{roundContent.Substring(0, Math.Min(100, roundContent.Length))}', toolCalls={roundToolCalls.Count}", "ChatService");
                if (roundToolCalls.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(roundContent))
                {
                    // Combine SSE reasoning_content with thinking tag extraction
                    var sseReasoning = reasoningBuffer2.ToString();
                    
                    // Extract <thinking> tags from content (fallback for models that include them)
                    string? tagReasoning = null;
                    string cleanContent = roundContent;
                    var openTag = "<thinking";
                    var closeTag = "</thinking>";
                    var openIdx = roundContent.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
                    if (openIdx >= 0)
                    {
                        var closeStart = roundContent.IndexOf(closeTag, openIdx, StringComparison.OrdinalIgnoreCase);
                        if (closeStart >= 0)
                        {
                            int reasoningLen = closeStart - (openIdx + openTag.Length);
                            tagReasoning = roundContent.Substring(openIdx + openTag.Length, reasoningLen).Trim();
                            cleanContent = roundContent.Substring(0, openIdx) + roundContent.Substring(closeStart + closeTag.Length);
                            cleanContent = cleanContent.Trim();
                        }
                    }
                    
                    var reasoning = string.IsNullOrEmpty(sseReasoning) ? tagReasoning : 
                                    string.IsNullOrEmpty(tagReasoning) ? sseReasoning :
                                    sseReasoning + "\n" + tagReasoning;
                    _log.Information($"[CHAT-TOOLS] Final response: reasoning_len={reasoning?.Length ?? 0}, content_len={cleanContent.Length}", "ChatService");
                    return new ChatMessage

                    {
                        Role = ChatRole.Assistant,
                        Content = cleanContent,
                        Reasoning = reasoning,
                        Timestamp = DateTime.Now
                    };
                }
                break;
            }

            // Detect tool call loops - if same tool+args called twice, break the loop
            foreach (var tc in roundToolCalls)
            {
                var callKey = $"{tc.Name}:{tc.Arguments.Substring(0, Math.Min(100, tc.Arguments.Length))}";
                if (seenToolCalls.Contains(callKey))
                {
                    _log.Warning($"[CHAT-TOOLS] Detected tool loop: {callKey}, breaking", "ChatService");
                    roundToolCalls.Clear();
                    break;
                }
                seenToolCalls.Add(callKey);
            }

            var assistantMsg = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = roundContent,
                ToolCalls = roundToolCalls,
                Timestamp = DateTime.Now
            };
            onMessage(assistantMsg);

            messages.Add(new OpenAiMessage { Role = "assistant", Content = roundContent, ToolCalls = roundToolCalls
                .Select(tc => new OpenAiToolCall { Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments })
                .ToList() });

             foreach (var toolCall in roundToolCalls)
                 {
                    _log.Information($"[CHAT-TOOLS] Executing tool: {toolCall.Name} args={toolCall.Arguments}", "ChatService");
                    var result = await mcpTools.ExecuteToolAsync(toolCall.Name, toolCall.Arguments);

                    // Clean and limit tool result to prevent context overflow
                    var cleanedContent = McpToolsService.CleanToolResult(result.Content, toolCall.Name);

                    // Limit to 2000 chars to prevent overwhelming the model with HTML garbage
                    const int maxToolResultLen = 2000;
                    var limitedContent = cleanedContent.Length > maxToolResultLen
                        ? cleanedContent[..maxToolResultLen] + $"\n\n... [truncated, total {cleanedContent.Length} chars]"
                        : cleanedContent;

                    var resultPreview = limitedContent.Length > 500 ? limitedContent[..500] + "..." : limitedContent;
                    _log.Information($"[CHAT-TOOLS] Tool result ({cleanedContent.Length} chars, limited to {limitedContent.Length}): {resultPreview}", "ChatService");

                    var toolMsg = new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        Content = limitedContent,
                        ToolCallId = toolCall.Id,
                        ToolName = toolCall.Name,
                        Timestamp = DateTime.Now
                    };
                    onMessage(toolMsg);

                    messages.Add(new OpenAiMessage
                    {
                        Role = "tool",
                        Content = limitedContent,
                        ToolCallId = toolCall.Id,
                        ToolName = toolCall.Name
                    });
            }
        }

        return null;
    }
    async Task ParseSseStream(Stream stream, Action<string> onToken, Action<ToolCall> onToolCall, Action<string>? onReasoning, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        var buffer = new StringBuilder();
        int tokenCount = 0;
        _log.Information("[CHAT] SSE parse started", "ChatService");

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line))
                continue;

            if (line == "data: [DONE]")
            {
                _log.Information($"[CHAT] SSE [DONE] received, tokens={tokenCount}", "ChatService");
                break;
            }

            if (line.StartsWith("data: "))
            {
                var json = line[6..];
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta))
                        {
                            // Handle reasoning_content from deep thinking models (Qwen, etc.)
                            if (onReasoning != null && delta.TryGetProperty("reasoning_content", out var reasoningEl) && reasoningEl.ValueKind == JsonValueKind.String)
                            {
                                var reasoningToken = reasoningEl.GetString();
                                if (!string.IsNullOrEmpty(reasoningToken))
                                    onReasoning(reasoningToken);
                            }

                            if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                            {
                                var token = contentEl.GetString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    tokenCount++;
                                    if (token.Contains("<thinking") || token.Contains("</thinking"))
                                        _log.Debug($"[CHAT] REASONING TOKEN #{tokenCount}: '{token}'", "ChatService");
                                    else
                                        _log.Debug($"[CHAT] Token #{tokenCount}: '{token}'", "ChatService");
                                    buffer.Append(token);
                                    onToken(token);
                                }
                            }

                            if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                            {
                                foreach (var tc in toolCalls.EnumerateArray())
                                {
                                    var call = new ToolCall();
                                    if (tc.TryGetProperty("id", out var idEl))
                                        call.Id = idEl.GetString() ?? call.Id;
                                    if (tc.TryGetProperty("function", out var fnEl))
                                    {
                                        if (fnEl.TryGetProperty("name", out var nameEl))
                                            call.Name = nameEl.GetString() ?? string.Empty;
                                        if (fnEl.TryGetProperty("arguments", out var argsEl))
                                            call.Arguments = argsEl.GetString() ?? string.Empty;
                                    }
                                    onToolCall(call);
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON
                }
            }
        }
    }

    async Task ParseSseStreamWithTools(
        Stream stream,
        Action<string> onToken,
        Action<int, string, string, string> onToolCallPart,
        Action onAllToolCallsComplete,
        Action<string>? onReasoning,
        CancellationToken ct)
    {
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line))
                continue;

            if (line == "data: [DONE]")
                break;

            if (line.StartsWith("data: "))
            {
                var json = line[6..];
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta))
                        {
                            // Handle reasoning_content from deep thinking models
                            if (onReasoning != null && delta.TryGetProperty("reasoning_content", out var reasoningEl) && reasoningEl.ValueKind == JsonValueKind.String)
                            {
                                var reasoningToken = reasoningEl.GetString();
                                if (!string.IsNullOrEmpty(reasoningToken))
                                {
                                    _log.Information($"[CHAT-TOOLS] Reasoning token: '{reasoningToken.Substring(0, Math.Min(50, reasoningToken.Length))}'", "ChatService");
                                    onReasoning(reasoningToken);
                                }
                            }

                            if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                            {
                                var token = contentEl.GetString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    // Debug: check for thinking tags in MCP stream
                                    if (token.Contains("<thinking") || token.Contains("</thinking>"))
                                        _log.Information($"[CHAT-TOOLS] Thinking tag in content: '{token.Substring(0, Math.Min(100, token.Length))}'", "ChatService");
                                    onToken(token);
                                }
                            }

                            if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                            {
                                foreach (var tc in toolCalls.EnumerateArray())
                                {
                                    string id = "";
                                    string name = "";
                                    string args = "";
                                    int index = 0;

                                    if (tc.TryGetProperty("index", out var idxEl))
                                        index = idxEl.GetInt32();

                                    if (tc.TryGetProperty("id", out var idEl))
                                        id = idEl.GetString() ?? "";

                                    if (tc.TryGetProperty("function", out var fnEl))
                                    {
                                        if (fnEl.TryGetProperty("name", out var nameEl))
                                            name = nameEl.GetString() ?? "";
                                        if (fnEl.TryGetProperty("arguments", out var argsEl))
                                            args = argsEl.GetString() ?? "";
                                    }

                                    onToolCallPart(index, id, name, args);
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON
                }
            }
        }

        onAllToolCallsComplete();
    }

    static List<OpenAiMessage> BuildMessages(string systemPrompt, List<ChatMessage> history, string userMessage)
    {
        var messages = new List<OpenAiMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new OpenAiMessage { Role = "system", Content = systemPrompt });

        foreach (var msg in history)
        {
            var openMsg = BuildOpenAiMessage(msg);
            messages.Add(openMsg);
        }

        messages.Add(new OpenAiMessage { Role = "user", Content = userMessage });
        return messages;
    }

    static List<OpenAiMessage> BuildMessagesWithImages(string systemPrompt, List<ChatMessage> history, string userMessage, List<string> imageBase64)
    {
        var messages = new List<OpenAiMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new OpenAiMessage { Role = "system", Content = systemPrompt });

        foreach (var msg in history)
        {
            var openMsg = BuildOpenAiMessage(msg);
            messages.Add(openMsg);
        }

        // Build multi-modal content for user message with images
        var userMsg = new OpenAiMessage { Role = "user" };
        if (imageBase64.Count > 0)
        {
            userMsg.ContentParts = new List<Dictionary<string, object>>
            {
                new() { { "type", "text" }, { "text", userMessage } }
            };
            foreach (var img in imageBase64)
            {
                var dataUrl = img.StartsWith("data:") ? img : $"data:image/png;base64,{img}";
                userMsg.ContentParts.Add(new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    { "image_url", new Dictionary<string, string> { { "url", dataUrl } } }
                });
            }
        }
        else
        {
            userMsg.Content = userMessage;
        }

        messages.Add(userMsg);
        return messages;
    }

    static OpenAiMessage BuildOpenAiMessage(ChatMessage msg)
    {
        var openMsg = new OpenAiMessage
        {
            Role = msg.RoleName,
            Content = msg.Content
        };

        if (msg.ToolCallId != null)
            openMsg.ToolCallId = msg.ToolCallId;
        if (msg.ToolName != null)
            openMsg.ToolName = msg.ToolName;
        if (msg.ToolCalls.Count > 0)
            openMsg.ToolCalls = msg.ToolCalls
                .Select(tc => new OpenAiToolCall { Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments })
                .ToList();

        // Handle messages with image attachments
        if (msg.ImageAttachments != null && msg.ImageAttachments.Count > 0)
        {
            openMsg.ContentParts = new List<Dictionary<string, object>>
            {
                new() { { "type", "text" }, { "text", msg.Content } }
            };
            foreach (var img in msg.ImageAttachments)
            {
                var dataUrl = img.StartsWith("data:") ? img : $"data:image/png;base64,{img}";
                openMsg.ContentParts.Add(new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    { "image_url", new Dictionary<string, string> { { "url", dataUrl } } }
                });
            }
        }

        return openMsg;
    }

    static string BuildRequestBody(
        List<OpenAiMessage> messages,
        bool includeTools,
        List<McpToolDefinition>? tools,
        float temperature,
        float topP,
        int topK,
        float minP,
        float repeatPenalty,
        float presencePenalty,
        float frequencyPenalty,
        int seed,
        int maxTokens,
        List<string> stopSequences,
        bool stream)
    {
        var body = new
        {
            model = "local-model",
            messages = messages.Select(m =>
            {
                var dict = new Dictionary<string, object> { { "role", m.Role } };
                // Use content_parts (multi-modal) if available, otherwise plain content
                if (m.ContentParts != null && m.ContentParts.Count > 0)
                    dict["content"] = m.ContentParts;
                else
                    dict["content"] = m.Content ?? "";
                if (m.ToolCallId != null) dict["tool_call_id"] = m.ToolCallId;
                if (m.ToolName != null) dict["tool_name"] = m.ToolName;
                if (m.ToolCalls != null && m.ToolCalls.Count > 0)
                    dict["tool_calls"] = m.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new { name = tc.Name, arguments = tc.Arguments }
                    }).ToList();
                return dict;
            }).ToList(),
            temperature = Math.Max(0.01f, temperature),
            top_p = topP,
            top_k = topK,
            min_p = minP,
            repeat_penalty = repeatPenalty,
            presence_penalty = presencePenalty,
            frequency_penalty = frequencyPenalty,
            seed = seed >= 0 ? (int?)seed : null,
            max_tokens = maxTokens > 0 ? (int?)maxTokens : null,
            stop = stopSequences.Count > 0 ? stopSequences : null,
            stream = stream,
            tools = includeTools && tools != null ? tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = new
                    {
                        type = "object",
                        properties = t.Parameters.ToDictionary(
                            p => p.Key,
                            p => new
                            {
                                type = p.Value.Type,
                                description = p.Value.Description
                            }
                        ),
                        required = t.Parameters.Where(p => p.Value.Required).Select(p => p.Key).ToList()
                    }
                }
            }).ToList() : null
        };

        return JsonSerializer.Serialize(body);
    }

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

class OpenAiMessage
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

class OpenAiToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "";
}
