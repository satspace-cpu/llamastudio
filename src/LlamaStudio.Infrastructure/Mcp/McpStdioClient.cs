using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Infrastructure.Mcp;

/// <summary>
/// MCP stdio JSON-RPC client — launches an external MCP server process
/// and communicates via stdin/stdout using JSON-RPC 2.0.
/// </summary>
public class McpStdioClient : IMcpClient
{
    readonly Process _process;
    readonly StreamWriter _writer;
    readonly StreamReader _reader;
    readonly object _lock = new();
    int _requestId;
    bool _disposed;
    readonly List<(string Name, string Description, Dictionary<string, object> InputSchema)> _cachedTools = new();

    public string ServerName { get; }
    public bool IsConnected => !_process.HasExited;

    public McpStdioClient(string name, string command, List<string>? args = null, Dictionary<string, string>? env = null)
    {
        ServerName = name;

        // On Windows, resolve command: if it's a .cmd/.bat file (like npx.cmd),
        // we must use "cmd /c" wrapper because UseShellExecute=false can't run batch files directly.
        var (fileName, wrapperArgs) = ResolveCommand(command);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // If we're using cmd /c wrapper, pass command + args as a single argument string
        if (wrapperArgs != null)
        {
            var allArgs = new List<string>(wrapperArgs);
            if (args != null)
                allArgs.AddRange(args);
            startInfo.Arguments = string.Join(" ", allArgs.Select(a => a.Contains(" ") ? $"\"{a}\"" : a));
        }
        else
        {
            if (args != null)
                foreach (var arg in args)
                    startInfo.ArgumentList.Add(arg);
        }

        if (env != null)
            foreach (var (key, value) in env)
                startInfo.EnvironmentVariables[key] = value;

        _process = new Process { StartInfo = startInfo };
        _process.Start();
        _writer = _process.StandardInput;
        _reader = _process.StandardOutput;
    }

    (string FileName, List<string>? WrapperArgs) ResolveCommand(string command)
    {
        // If it's an absolute path or contains '.', try as-is
        if (command.Contains('.') || Path.IsPathRooted(command) || command.Contains('\\') || command.Contains('/'))
        {
            return (command, null);
        }

        // Search PATH for the command, including PATHEXT extensions
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
        var exts = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? new[] { ".COM", ".EXE", ".BAT", ".CMD" };

        // Try exact match first
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, command);
            if (File.Exists(fullPath))
                return (fullPath, null);
        }

        // Try with extensions
        foreach (var ext in exts)
        {
            foreach (var dir in pathDirs)
            {
                var fullPath = Path.Combine(dir, command + ext);
                if (File.Exists(fullPath))
                {
                    // If it's a .bat or .cmd file, use cmd /c wrapper
                    if (ext.Equals(".BAT", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".CMD", StringComparison.OrdinalIgnoreCase))
                    {
                        return ("cmd.exe", new List<string> { "/c", fullPath });
                    }
                    return (fullPath, null);
                }
            }
        }

        // Not found — return as-is, Process.Start will throw a meaningful error
        return (command, null);
    }

    public async Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default)
    {
        if (_cachedTools.Count > 0)
            return _cachedTools;

        try
        {
            // Initialize
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

            // Initialized notification
            await SendNotificationAsync("notifications/initialized", new Dictionary<string, object>(), ct);

            // List tools
            var response = await SendRequestAsync("tools/list", new Dictionary<string, object>(), ct);

            if (response != null && response.TryGetValue("result", out var result))
            {
                var doc = JsonDocument.Parse(result.ToString() ?? "{}");
                if (doc.RootElement.TryGetProperty("tools", out var tools))
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
            throw new InvalidOperationException($"MCP server '{ServerName}' initialization failed: {ex.Message}", ex);
        }

        return _cachedTools;
    }

    public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default)
    {
        var response = await SendRequestAsync("tools/call", new Dictionary<string, object>
        {
            ["name"] = toolName,
            ["arguments"] = arguments
        }, ct);

        if (response == null)
            return "Error: No response from MCP server";

        if (response.TryGetValue("result", out var result))
        {
            var doc = JsonDocument.Parse(result.ToString() ?? "{}");
            if (doc.RootElement.TryGetProperty("content", out var content))
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

        if (response.TryGetValue("error", out var error))
            return $"Error: {error}";

        return $"Error: Unexpected response for tool '{toolName}'";
    }

    async Task<Dictionary<string, JsonElement>> SendRequestAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _requestId);
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params
        };

        var json = JsonSerializer.Serialize(request) + "\n";

        // Write synchronously inside lock to avoid await-in-lock
        lock (_lock)
        {
            _writer.WriteLine(json);
            _writer.Flush();
        }

        // Read response (skip notifications)
        while (!ct.IsCancellationRequested)
        {
            var line = await _reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line))
                continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("id", out var respId) && respId.GetInt32() == id)
                {
                    return ParseJsonDocument(doc);
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        throw new TimeoutException($"MCP request timeout: {method}");
    }

    async Task SendNotificationAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
    {
        var notification = new
        {
            jsonrpc = "2.0",
            method,
            @params
        };

        var json = JsonSerializer.Serialize(notification) + "\n";

        // Write synchronously inside lock
        lock (_lock)
        {
            _writer.WriteLine(json);
            _writer.Flush();
        }
    }

    static Dictionary<string, JsonElement> ParseJsonDocument(JsonDocument doc)
    {
        var result = new Dictionary<string, JsonElement>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value;
        }
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
            }
        }
        catch { }

        _writer.Dispose();
        _reader.Dispose();
        _process.Dispose();
    }
}
