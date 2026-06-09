using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IChatService
{
    Task SendChatAsync(
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
        CancellationToken cancellationToken = default);

    Task<ChatMessage?> SendChatWithToolsAsync(
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
        CancellationToken cancellationToken = default);
}
