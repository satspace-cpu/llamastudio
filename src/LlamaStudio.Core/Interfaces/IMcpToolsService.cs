using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IMcpToolsService
{
    List<McpToolDefinition> GetAvailableTools();
    Task<McpToolResult> ExecuteToolAsync(string toolName, string argumentsJson);

    // External MCP servers
    List<McpServerConfig> GetMcpServers();
    Task AddMcpServerAsync(McpServerConfig config);
    Task RemoveMcpServerAsync(string id);
    Task ToggleMcpServerAsync(string id, bool enabled);
    Task RefreshMcpServersAsync();
}
