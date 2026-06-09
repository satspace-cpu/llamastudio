using System.Text;
using System.Text.Json;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Infrastructure.Mcp;

/// <summary>
/// MCP SSE (Server-Sent Events) JSON-RPC client — connects to an MCP server
/// over HTTP using SSE for server-to-client messages and POST for client-to-server.
/// </summary>
public class McpSseClient : IMcpClient
{
    readonly HttpClient _httpClient;
    readonly string _baseUrl;
    readonly object _lock = new();
    int _requestId;
    bool _disposed;
    bool _initialized;

    // SSE connection state
    CancellationTokenSource? _sseCts;
    Task? _sseReaderTask;
    string? _messageEndpoint; // POST endpoint returned by SSE

    // Pending request tracking: id -> TaskCompletionSource
    readonly Dictionary<int, TaskCompletionSource<JsonDocument>> _pendingRequests = new();
    // Notifications and other SSE events
    readonly List<JsonDocument> _receivedNotifications = new();
    readonly List<(string Name, string Description, Dictionary<string, object> InputSchema)> _cachedTools = new();

    public string ServerName { get; }
    public bool IsConnected => !_disposed && _messageEndpoint != null;

    public McpSseClient(string name, string url)
    {
        ServerName = name;
        _baseUrl = url.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default)
    {
        if (_cachedTools.Count > 0)
            return _cachedTools;

        await EnsureInitializedAsync(ct);

        try
        {
            var response = await SendRequestAsync("tools/list", new Dictionary<string, object>(), ct);

            if (response != null && response.RootElement.TryGetProperty("result", out var result))
            {
                if (result.TryGetProperty("tools", out var tools))
                {
                    _cachedTools.Clear();
                    foreach (var tool in tools.EnumerateArray())
                    {
                        var name = tool.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var desc = tool.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        var schema = new Dictionary<string, object>();
                        if (tool.TryGetProperty("inputSchema", out var s))
                        {
                            schema = JsonSerializer.Deserialize<Dictionary<string, object>>(s.GetRawText()) ?? new();
                        }
                        _cachedTools.Add((name, desc, schema));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MCP server '{ServerName}' tools/list failed: {ex.Message}", ex);
        }

        return _cachedTools;
    }

    public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var response = await SendRequestAsync("tools/call", new Dictionary<string, object>
        {
            ["name"] = toolName,
            ["arguments"] = arguments
        }, ct);

        if (response == null)
            return "Error: No response from MCP server";

        if (response.RootElement.TryGetProperty("result", out var result))
        {
            if (result.TryGetProperty("content", out var content))
            {
                var parts = new StringBuilder();
                foreach (var item in content.EnumerateArray())
                {
                    var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    if (type == "text" && item.TryGetProperty("text", out var text))
                    {
                        parts.AppendLine(text.GetString());
                    }
                    else if (type == "image" && item.TryGetProperty("data", out var data))
                    {
                        parts.AppendLine($"[Image: {data.GetString()?.Length ?? 0} bytes]");
                    }
                }
                return parts.ToString().Trim();
            }
        }

        if (response.RootElement.TryGetProperty("error", out var error))
            return $"Error: {error}";

        return $"Error: Unexpected response for tool '{toolName}'";
    }

    async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized)
            return;

        await StartSseConnectionAsync(ct);

        // Initialize handshake
        await SendRequestAsync("initialize", new Dictionary<string, object>
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new Dictionary<string, object>(),
            ["clientInfo"] = new Dictionary<string, object>
            {
                ["name"] = "llama-studio",
                ["version"] = "1.0.0"
            }
        }, ct);

        // Initialized notification (fire-and-forget)
        _ = SendNotificationAsync("notifications/initialized", new Dictionary<string, object>());

        _initialized = true;
    }

    async Task StartSseConnectionAsync(CancellationToken ct)
    {
        _sseCts = new CancellationTokenSource();

        // Connect to SSE endpoint — typically /sse
        var sseUrl = _baseUrl + "/sse";
        var request = new HttpRequestMessage(HttpMethod.Get, sseUrl)
        {
            Version = System.Net.HttpVersion.Version11
        };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _sseCts.Token);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(_sseCts.Token);
        var reader = new StreamReader(stream, Encoding.UTF8);

        _sseReaderTask = Task.Run(async () =>
        {
            try
            {
                var buffer = new StringBuilder();
                while (!_sseCts!.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(_sseCts.Token);
                    if (line == null)
                        break;

                    // SSE: empty line terminates an event
                    if (line.Length == 0)
                    {
                        ProcessSseEvent(buffer.ToString());
                        buffer.Clear();
                        continue;
                    }

                    // SSE comment — skip
                    if (line.StartsWith(":"))
                        continue;

                    // Accumulate event data
                    if (line.StartsWith("event:"))
                    {
                        // Store event type if needed, but we mainly care about "data:"
                        buffer.Clear();
                        continue;
                    }

                    if (line.StartsWith("data:"))
                    {
                        buffer.Append(line.Substring(5).TrimStart());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose
            }
            catch (Exception)
            {
                // Connection lost
            }
        }, _sseCts.Token);
    }

    void ProcessSseEvent(string eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData))
            return;

        try
        {
            var doc = JsonDocument.Parse(eventData);

            lock (_lock)
            {
                // Check if this is a response to a pending request
                if (doc.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number)
                {
                    var requestId = id.GetInt32();
                    if (_pendingRequests.TryGetValue(requestId, out var tcs))
                    {
                        _pendingRequests.Remove(requestId);
                        tcs.TrySetResult(doc);
                        return;
                    }
                }

                // Check for message endpoint in initialize response
                if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("capabilities", out _) ||
                        result.TryGetProperty("protocolVersion", out _))
                    {
                        // Initialize response — the message endpoint is typically /message
                        // Some servers return it in the SSE event, others use a fixed path
                        if (_messageEndpoint == null)
                        {
                            _messageEndpoint = _baseUrl + "/message";
                        }
                    }
                }

                // Otherwise it's a notification
                _receivedNotifications.Add(doc);
            }
        }
        catch
        {
            // Malformed JSON — skip
        }
    }

    async Task<JsonDocument?> SendRequestAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
    {
        // For SSE transport, we need the message endpoint
        if (_messageEndpoint == null)
        {
            // Fallback: try /message
            _messageEndpoint = _baseUrl + "/message";
        }

        var id = Interlocked.Increment(ref _requestId);
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Register TCS for response
        var tcs = new TaskCompletionSource<JsonDocument>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            _pendingRequests[id] = tcs;
        }

        try
        {
            // Send POST request
            var response = await _httpClient.PostAsync(_messageEndpoint, content, ct);
            // For SSE, the POST may return 202 Accepted or 200 OK with no body
            // The actual response comes back via SSE

            // Some MCP servers return the response directly in the POST response
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(body);
                        lock (_lock)
                        {
                            _pendingRequests.Remove(id);
                        }
                        return doc;
                    }
                    catch
                    {
                        // Not JSON, response will come via SSE
                    }
                }
            }

            // Wait for SSE response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            var result = await tcs.Task.WaitAsync(timeoutCts.Token);
            return result;
        }
        catch (OperationCanceledException)
        {
            lock (_lock)
            {
                _pendingRequests.Remove(id);
            }
            throw new TimeoutException($"MCP SSE request timeout: {method}");
        }
        finally
        {
            lock (_lock)
            {
                _pendingRequests.Remove(id);
            }
        }
    }

    async Task SendNotificationAsync(string method, Dictionary<string, object> @params)
    {
        if (_messageEndpoint == null)
            return;

        var notification = new
        {
            jsonrpc = "2.0",
            method,
            @params
        };

        var json = JsonSerializer.Serialize(notification);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            await _httpClient.PostAsync(_messageEndpoint, content);
        }
        catch
        {
            // Fire-and-forget notification
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sseCts?.Cancel();
        _sseCts?.Dispose();

        lock (_lock)
        {
            foreach (var tcs in _pendingRequests.Values)
                tcs.TrySetException(new ObjectDisposedException(nameof(McpSseClient)));
            _pendingRequests.Clear();
        }

        _httpClient.Dispose();
    }
}
