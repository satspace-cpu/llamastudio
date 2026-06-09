namespace LlamaStudio.Core.Interfaces;

/// <summary>
/// Common interface for MCP clients (stdio, SSE, etc.).
/// </summary>
public interface IMcpClient : IDisposable
{
    string ServerName { get; }
    bool IsConnected { get; }

    Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default);
    Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default);
}
