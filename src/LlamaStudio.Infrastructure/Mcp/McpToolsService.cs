using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

using System.Text;

namespace LlamaStudio.Infrastructure.Mcp;

public class McpToolsService : IMcpToolsService, IDisposable
{
    readonly ILogService _log;
    readonly ISettings _settings;

    // External MCP servers
    readonly Dictionary<string, McpServerConfig> _servers = new();
    readonly Dictionary<string, IMcpClient> _clients = new();
    readonly object _serversLock = new();
    bool _disposed;

    // Persistent storage path
    string ServersConfigPath
    {
        get
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
            return Path.Combine(baseDir, "mcp_servers.json");
        }
    }

    public McpToolsService(ILogService log, ISettings settings)
    {
        _log = log;
        _settings = settings;
        LoadServersConfig();
    }

    #region Built-in tools

    static readonly List<McpToolDefinition> s_builtinTools = new()
    {
        new()
        {
            Name = "read_file",
            Description = "Read the contents of a file from the filesystem. Returns the file content as text.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "path", new McpToolParameter { Type = "string", Description = "Absolute path to the file", Required = true } }
            }
        },
        new()
        {
            Name = "write_file",
            Description = "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "path", new McpToolParameter { Type = "string", Description = "Absolute path to the file", Required = true } },
                { "content", new McpToolParameter { Type = "string", Description = "Content to write to the file", Required = true } }
            }
        },
        new()
        {
            Name = "list_directory",
            Description = "List files and directories in the specified path. Returns a structured listing.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "path", new McpToolParameter { Type = "string", Description = "Absolute path to the directory", Required = true } }
            }
        },
        new()
        {
            Name = "search_files",
            Description = "Search for files matching a glob pattern in a directory. Recursively searches subdirectories.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "directory", new McpToolParameter { Type = "string", Description = "Directory to search in", Required = true } },
                { "pattern", new McpToolParameter { Type = "string", Description = "Glob pattern to match (e.g. *.cs, **/*.json)", Required = true } }
            }
        },
        new()
        {
            Name = "web_fetch",
            Description = "Fetch content from a URL and return it as markdown or text. Good for reading web pages.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "url", new McpToolParameter { Type = "string", Description = "URL to fetch", Required = true } },
                { "format", new McpToolParameter { Type = "string", Description = "Output format: markdown, text, or html. Default: markdown" } }
            }
        },
        new()
        {
            Name = "web_search",
            Description = "Search the web using DuckDuckGo. Returns search results with titles, URLs, and snippets.",
            Parameters = new Dictionary<string, McpToolParameter>
            {
                { "query", new McpToolParameter { Type = "string", Description = "Search query", Required = true } }
            }
        }
    };

    #endregion

    #region IMcpToolsService — tools

    public List<McpToolDefinition> GetAvailableTools()
    {
        var tools = new List<McpToolDefinition>(s_builtinTools);

        var connectedClients = new List<(McpServerConfig Server, IMcpClient Client)>();
        lock (_serversLock)
        {
            foreach (var (id, server) in _servers)
            {
                if (!server.Enabled) continue;
                if (!_clients.TryGetValue(id, out var client)) continue;
                if (!client.IsConnected) continue;
                connectedClients.Add((server, client));
            }
        }

        foreach (var (server, client) in connectedClients)
        {
            try
            {
                var serverTools = client.GetToolsAsync(CancellationToken.None).GetAwaiter().GetResult();
                foreach (var (name, desc, schema) in serverTools)
                {
                    tools.Add(new McpToolDefinition
                    {
                        Name = $"{server.Name}:{name}",
                        Description = desc,
                        Parameters = ExtractParameters(schema)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Warning($"Failed to get tools from MCP server '{server.Name}': {ex.Message}", "McpTools");
            }
        }

        return tools;
    }

    public async Task<McpToolResult> ExecuteToolAsync(string toolName, string argumentsJson)
    {
        try
        {
            // Check if it's an external MCP tool (serverName:toolName)
            var colonIndex = toolName.IndexOf(':');
            if (colonIndex > 0)
            {
                var serverName = toolName.Substring(0, colonIndex);
                var actualToolName = toolName.Substring(colonIndex + 1);
                return await ExecuteExternalToolAsync(serverName, actualToolName, argumentsJson);
            }

            // Built-in tool
            _log.Information($"MCP tool executed: {toolName}", "McpTools");

            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson)
                ?? new Dictionary<string, JsonElement>();

            string result = toolName switch
            {
                "read_file" => await ReadFileAsync(args),
                "write_file" => await WriteFileAsync(args),
                "list_directory" => await ListDirectoryAsync(args),
                "search_files" => await SearchFilesAsync(args),
                "web_fetch" => await WebFetchAsync(args),
                "web_search" => await WebSearchAsync(args),
                _ => $"Error: Unknown tool '{toolName}'"
            };

            return new McpToolResult
            {
                ToolName = toolName,
                Content = result,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"MCP tool error: {toolName}", "McpTools");
            return new McpToolResult
            {
                ToolName = toolName,
                Content = $"Error executing {toolName}: {ex.Message}",
                IsError = true
            };
        }
    }

    #endregion

    #region External MCP servers

    public List<McpServerConfig> GetMcpServers()
    {
        lock (_serversLock)
            return _servers.Values.ToList();
    }

    public async Task AddMcpServerAsync(McpServerConfig config)
    {
        lock (_serversLock)
        {
            _servers[config.Id] = config;
        }

        if (config.Enabled)
            await ConnectServerAsync(config);

        SaveServersConfig();
    }

    public async Task RemoveMcpServerAsync(string id)
    {
        lock (_serversLock)
        {
            if (_clients.TryGetValue(id, out var client))
            {
                client.Dispose();
                _clients.Remove(id);
            }
            _servers.Remove(id);
        }

        SaveServersConfig();
    }

    public async Task ToggleMcpServerAsync(string id, bool enabled)
    {
        McpServerConfig? server = null;
        lock (_serversLock)
        {
            if (!_servers.TryGetValue(id, out server)) return;
            server.Enabled = enabled;
        }

        if (enabled)
        {
            await ConnectServerAsync(server);
        }
        else
        {
            lock (_serversLock)
            {
                if (_clients.TryGetValue(id, out var client))
                {
                    client.Dispose();
                    _clients.Remove(id);
                }
            }
        }

        SaveServersConfig();
    }

    public async Task RefreshMcpServersAsync()
    {
        var toRefresh = new List<McpServerConfig>();
        lock (_serversLock)
        {
            foreach (var (id, server) in _servers.Where(kvp => kvp.Value.Enabled))
            {
                if (_clients.TryGetValue(id, out var oldClient))
                {
                    oldClient.Dispose();
                    _clients.Remove(id);
                }
                toRefresh.Add(server);
            }
        }

        foreach (var server in toRefresh)
        {
            try
            {
                await ConnectServerAsync(server);
                server.LastError = null;
            }
            catch (Exception ex)
            {
                server.LastError = ex.Message;
                server.IsConnected = false;
                _log.Warning($"MCP server '{server.Name}' reconnect failed: {ex.Message}", "McpTools");
            }
        }
    }

      async Task ConnectServerAsync(McpServerConfig config)
        {
            try
            {
                IMcpClient client = config.TransportType switch
                {
                    McpTransportType.Sse => new McpSseClient(config.Name, config.Url),
                    _ => new McpStdioClient(config.Name, config.Command, config.Args, config.Env)
                };

                var tools = await client.GetToolsAsync();

                lock (_serversLock)
                {
                    _clients[config.Id] = client;
                    config.IsConnected = true;
                    config.ToolsCount = tools.Count;
                    config.LastError = null;
                }

                _log.Information($"MCP server '{config.Name}' connected ({config.TransportType}), tools: {tools.Count}", "McpTools");
            }
            catch (Exception ex)
            {
                string errorMsg = NormalizeErrorMessage(ex, config);
                lock (_serversLock)
                {
                    if (_clients.ContainsKey(config.Id))
                    {
                        _clients[config.Id].Dispose();
                        _clients.Remove(config.Id);
                    }
                    config.IsConnected = false;
                    config.LastError = errorMsg;
                }
                _log.Error($"MCP server '{config.Name}' connection failed: {errorMsg}", "McpTools");
            }
        }

    static string NormalizeErrorMessage(Exception ex, McpServerConfig config)
    {
        var msg = ex.Message;

        // Handle "file not found" errors with friendly messages (stdio only)
        if (config.TransportType == McpTransportType.Stdio &&
            (ex is System.ComponentModel.Win32Exception ||
            (msg.Contains("trying to start process") && msg.Contains("Не удается найти"))))
        {
            return $"Команда \"{config.Command}\" не найдена в PATH. Установите Node.js с https://nodejs.org (выберите LTS), затем перезапустите LlamaStudio.";
        }

        // SSE connection errors
        if (config.TransportType == McpTransportType.Sse)
        {
            if (msg.Contains("connect") || msg.Contains("connection") || msg.Contains(" refused"))
                return $"Не удалось подключиться к SSE-серверу по адресу {config.Url}. Проверьте, что сервер запущен и доступен.";
        }

        // Try to fix encoding issues: Windows may return OEM (CP866) encoded strings
        msg = Regex.Replace(msg, "[\\x00-\\x1F]", " ");

        return msg;
    }

    async Task<McpToolResult> ExecuteExternalToolAsync(string serverName, string toolName, string argumentsJson)
    {
        McpServerConfig? targetServer = null;
        IMcpClient? targetClient = null;

        lock (_serversLock)
        {
            targetServer = _servers.Values.FirstOrDefault(s => s.Name == serverName);
            if (targetServer == null)
                return new McpToolResult { ToolName = $"{serverName}:{toolName}", Content = $"Error: MCP server '{serverName}' not found", IsError = true };

            if (!_clients.TryGetValue(targetServer.Id, out targetClient))
                return new McpToolResult { ToolName = $"{serverName}:{toolName}", Content = $"Error: MCP server '{serverName}' not connected", IsError = true };
        }

        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)
                ?? new Dictionary<string, object>();

            var result = await targetClient.CallToolAsync(toolName, args);

            return new McpToolResult
            {
                ToolName = $"{serverName}:{toolName}",
                Content = result,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"MCP external tool error: {serverName}:{toolName}", "McpTools");
            return new McpToolResult
            {
                ToolName = $"{serverName}:{toolName}",
                Content = $"Error: {ex.Message}",
                IsError = true
            };
        }
    }

    #endregion

    #region Persistence

    void LoadServersConfig()
    {
        if (!File.Exists(ServersConfigPath)) return;

        try
        {
            var json = File.ReadAllText(ServersConfigPath, System.Text.Encoding.UTF8);
            var servers = JsonSerializer.Deserialize<List<McpServerConfig>>(json);
            if (servers != null)
            {
                lock (_serversLock)
                {
                    foreach (var s in servers)
                    {
                        s.IsConnected = false; // Will reconnect if enabled
                        _servers[s.Id] = s;
                    }
                }

                // Connect enabled servers
                lock (_serversLock)
                {
                    foreach (var server in _servers.Values.Where(s => s.Enabled))
                    {
                        _ = ConnectServerAsync(server).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                                _log.Error(t.Exception!.InnerException ?? t.Exception!, $"MCP server init: {server.Name}", "McpTools");
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load MCP servers config", "McpTools");
        }
    }

    void SaveServersConfig()
    {
        try
        {
            var toSave = new List<McpServerConfig>();
            lock (_serversLock)
            {
                foreach (var server in _servers.Values)
                {
                    toSave.Add(new McpServerConfig
                    {
                        Id = server.Id,
                        Name = server.Name,
                        TransportType = server.TransportType,
                        Command = server.Command,
                        Args = server.Args,
                        Env = server.Env,
                        Url = server.Url,
                        Enabled = server.Enabled,
                        Description = server.Description,
                        CreatedAt = server.CreatedAt
                    });
                }
            }

            var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(ServersConfigPath)!);
            File.WriteAllText(ServersConfigPath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save MCP servers config", "McpTools");
        }
    }

    #endregion

    #region Built-in tool implementations

    async Task<string> ReadFileAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || string.IsNullOrWhiteSpace(pathEl.GetString()))
            return "Error: 'path' parameter is required";

        var path = pathEl.GetString()!;
        if (!File.Exists(path))
            return $"Error: File not found: {path}";

        var content = await File.ReadAllTextAsync(path);
        if (content.Length > 50000)
            return content[..50000] + "\n\n... (truncated, file too large)";
        return content;
    }

    async Task<string> WriteFileAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || string.IsNullOrWhiteSpace(pathEl.GetString()))
            return "Error: 'path' parameter is required";

        if (!args.TryGetValue("content", out var contentEl))
            return "Error: 'content' parameter is required";

        var path = pathEl.GetString()!;
        var content = contentEl.GetString() ?? string.Empty;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir!);

        await File.WriteAllTextAsync(path, content);
        return $"Successfully wrote {content.Length} characters to {path}";
    }

    async Task<string> ListDirectoryAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || string.IsNullOrWhiteSpace(pathEl.GetString()))
            return "Error: 'path' parameter is required";

        var path = pathEl.GetString()!;
        if (!Directory.Exists(path))
            return $"Error: Directory not found: {path}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Directory: {path}");
        sb.AppendLine(new string('-', 50));

        try
        {
            var dirs = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            foreach (var d in dirs.OrderBy(x => x))
                sb.AppendLine($"[DIR]  {Path.GetFileName(d)}");

            foreach (var f in files.OrderBy(x => x))
            {
                try
                {
                    var info = new FileInfo(f);
                    var size = FormatSize(info.Length);
                    sb.AppendLine($"[FILE] {size,12}  {Path.GetFileName(f)}");
                }
                catch
                {
                    sb.AppendLine($"[FILE] {Path.GetFileName(f)}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return $"Error: Access denied to {path}";
        }

        return sb.ToString();
    }

    async Task<string> SearchFilesAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("directory", out var dirEl) || string.IsNullOrWhiteSpace(dirEl.GetString()))
            return "Error: 'directory' parameter is required";

        if (!args.TryGetValue("pattern", out var patEl) || string.IsNullOrWhiteSpace(patEl.GetString()))
            return "Error: 'pattern' parameter is required";

        var directory = dirEl.GetString()!;
        var pattern = patEl.GetString()!;

        if (!Directory.Exists(directory))
            return $"Error: Directory not found: {directory}";

        try
        {
            var simplePattern = pattern.Replace("**/", "");
            var files = Directory.GetFiles(directory, simplePattern, SearchOption.AllDirectories);

            if (files.Length == 0)
                return $"No files matching '{pattern}' found in {directory}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Found {files.Length} file(s) matching '{pattern}':");
            sb.AppendLine(new string('-', 50));
            foreach (var f in files.Take(100))
                sb.AppendLine(f);
            if (files.Length > 100)
                sb.AppendLine($"... and {files.Length - 100} more");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching files: {ex.Message}";
        }
    }

    async Task<string> WebFetchAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("url", out var urlEl) || string.IsNullOrWhiteSpace(urlEl.GetString()))
            return "Error: 'url' parameter is required";

        var url = urlEl.GetString()!;
        var format = "text";
        if (args.TryGetValue("format", out var fmtEl))
            format = fmtEl.GetString() ?? "text";

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        try
        {
            // Try Jina AI reader first — it extracts clean text from any webpage
            var jinaResult = await TryJinaReaderAsync(url);
            if (!string.IsNullOrEmpty(jinaResult) && jinaResult.Length > 50)
            {
                var cleaned = CleanToolResult(jinaResult);
                return cleaned.Length > 2000 ? cleaned[..2000] : cleaned;
            }

            // Fallback: direct fetch with aggressive HTML cleaning
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return $"Error: HTTP {response.StatusCode}";

            var html = await response.Content.ReadAsStringAsync();

            // Skip if response is mostly JS (SPA page)
            if (html.Contains("<script") && !html.Contains("<title>"))
                return $"Error: Page is JavaScript-rendered, cannot extract content";

            if (format == "html")
                return html.Length > 2000 ? html[..2000] : html;

            // Text format: use Readability-like extraction
            var text = ExtractMainContent(html);
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = Regex.Replace(text, @"[ \t]+", " ");
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l) && l.Length > 3).ToList();
            var result = string.Join("\n", lines).Trim();

            return result.Length > 2000 ? result[..2000] + "..." : result;
        }
        catch (Exception ex)
        {
            return $"Error fetching {url}: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts main content from HTML using Readability-like algorithm.
    /// </summary>
    static string ExtractMainContent(string html)
    {
        // Remove scripts and styles
        var text = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "");
        text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "");
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", "");
        
        // Try to find main content area
        var mainMatch = Regex.Match(text, @"<(?:main|article|div)[^>]*class=""[^""]*(?:content|main|post|article|entry)[^""]*""[^>]*>([\s\S]*?)</(?:main|article|div)>", RegexOptions.IgnoreCase);
        if (mainMatch.Success)
        {
            text = mainMatch.Groups[1].Value;
        }
        else
        {
            // Fallback: strip all HTML tags
            text = StripHtml(text);
        }
        
        return text;
    }

    async Task<string?> TryJinaReaderAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var jinaUrl = $"https://r.jina.ai/{url}";
            var response = await client.GetAsync(jinaUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            var text = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text) || text.Length < 50)
                return null;

            // Clean up Jina output: remove JSON-LD, navigation menus, and other noise
            text = CleanJinaOutput(text);
            
            if (text.Length < 30)
                return null;

            _log.Information($"[web_fetch] Jina reader success for {url} ({text.Length} chars cleaned)", "McpTools");
            return text;
        }
        catch (Exception ex)
        {
            _log.Debug($"[web_fetch] Jina reader failed: {ex.Message}, using fallback", "McpTools");
            return null;
        }
    }

    static string CleanJinaOutput(string text)
    {
        // Remove JSON-LD blocks (schema.org structured data)
        text = Regex.Replace(text, @"\{[^}]*""@context""[^}]*\}[^}]*(?:\}[^}]*)*\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"\{[^}]*""@type""[^}]*\}[^}]*\}", "", RegexOptions.Singleline);
        
        // Remove FAQPage/Question/Answer JSON structures
        text = Regex.Replace(text, @"\{[^}]*""type""[^}]*""FAQPage""[^}]*\}[\s\S]*?(?:\{[^}]*\}[\s\S]*)*\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"""(acceptedAnswer|Question|Answer)""[^}]*\}[^}]*\}", "", RegexOptions.Singleline);
        
        // Remove image references like ![Image N](blob:...) or ![Image N: ...](url)
        text = Regex.Replace(text, @"!\[Image \d+[:^\]]*\]\([^)]*\)", "", RegexOptions.IgnoreCase);
        
        // Remove navigation menu items (common patterns with colons and values)
        text = Regex.Replace(text, @"(?:Погода|Ветер|Давление|Влажность|Пыльца|Луна|Прогноз)[^:\n]*:[^,\n]*(?:,?[^,\n]*)*", "", RegexOptions.IgnoreCase);
        
        // Remove lines that are just navigation keywords (with optional trailing content)
        text = Regex.Replace(text, @"^\s*(?:Погода|Ветер|Давление|Влажность|Пыльца|Луна|Прогноз|Карта|Журнал|FAQ|На месяц|На карту|На 10 дней|На сегодня|На завтра)[\s:]*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        
        // Remove standalone URLs that aren't part of meaningful content (but keep [text](url) links)
        text = Regex.Replace(text, @"(?:^|\n)https?://[^\s,;)}\]]+", "", RegexOptions.Multiline);
        
        // Remove lines with only image placeholders or broken references
        text = Regex.Replace(text, @"^\s*\[Image \d+[\]^:]*.*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        
        // Clean up excessive whitespace
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"[ \t]+", " ");
        
        // Trim each line and remove empty ones
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
        text = string.Join("\n", lines);
        
        return text.Trim();
    }

    async Task<string> WebSearchAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("query", out var queryEl) || string.IsNullOrWhiteSpace(queryEl.GetString()))
            return "Error: 'query' parameter is required";

        var query = queryEl.GetString()!;

        try
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");

            // Try DuckDuckGo first (disabled - CAPTCHA)
            var ddgResults = TryDuckDuckGoAsync(client, query);
            if (ddgResults.Count > 0)
                return FormatSearchResults(query, ddgResults);

            // Fallback: Bing search
            var bingResults = await TryBingSearchAsync(client, query);
            if (bingResults.Count > 0)
                return FormatSearchResults(query, bingResults);

            return $"No results found for '{query}'";
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[web_search] Error searching '{query}': {ex.Message}", "McpTools");
            return $"Error searching '{query}': {ex.Message}";
        }
    }

 // DuckDuckGo blocked automated requests with CAPTCHA, using Bing only
    static List<(string Title, string Link, string Snippet)> TryDuckDuckGoAsync(HttpClient client, string query)
    {
        return new List<(string, string, string)>();
    }

async Task<List<(string Title, string Link, string Snippet)>> TryBingSearchAsync(HttpClient client, string query)
    {
        try
        {
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://www.bing.com/search?q={encoded}&setlang=ru-RU";
            
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            using var bingClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            bingClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            bingClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            bingClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            
            var html = await bingClient.GetStringAsync(url);
            _log.Debug($"[web_search] Bing response length: {html.Length}", "McpTools");

            var results = ParseBingResults(html);
            _log.Information($"[web_search] Bing returned {results.Count} results for '{query}'", "McpTools");
            return results;
        }
        catch (Exception ex)
        {
            _log.Warning($"[web_search] Bing failed: {ex.Message}", "McpTools");
            return new List<(string, string, string)>();
        }
    }

    static string FormatSearchResults(string query, List<(string Title, string Link, string Snippet)> results)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Search results for '{query}':");
        sb.AppendLine(new string('-', 50));

        for (int i = 0; i < results.Count; i++)
        {
            var (title, link, snippet) = results[i];
            sb.AppendLine($"\n{i + 1}. {title}");
            sb.AppendLine($"   {link}");
            if (!string.IsNullOrWhiteSpace(snippet))
                sb.AppendLine($"   {snippet}");
        }

        return sb.ToString();
    }

    static List<(string Title, string Link, string Snippet)> ParseDuckDuckGoResults(string html)
    {
        var results = new List<(string, string, string)>();
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Strategy 1: Look for result blocks with class containing "result"
        var resultRegex = new Regex(@"<a[^>]*class=""[^""]*result[^""]*""[^>]*href=""([^""]+)""[^>]*>([\s\S]*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match m in resultRegex.Matches(html).Take(10))
        {
            var link = m.Groups[1].Value.Trim();
            if (link.StartsWith("//")) link = "https:" + link;
            if (link.Contains("duckduckgo.com") || link.Contains("duckduckgo.com/")) continue;
            if (seenLinks.Contains(link)) continue;
            seenLinks.Add(link);

            var title = StripHtml(m.Groups[2].Value).Trim();
            if (string.IsNullOrEmpty(title) || title.Length < 5) continue;
            if (title.Length > 150) title = title[..150] + "...";

            results.Add((title, link, ""));
        }

        // Strategy 2: Look for result__snippet
        if (results.Count > 0)
        {
            var snippetRegex = new Regex(@"class=""result__snippet""[^>]*>([\s\S]*?)</", RegexOptions.Singleline);
            var snippets = snippetRegex.Matches(html).Select(m => StripHtml(m.Groups[1].Value).Trim()).Where(s => s.Length > 10).ToList();
            for (int i = 0; i < results.Count && i < snippets.Count; i++)
            {
                var snippet = Regex.Replace(snippets[i], @"\s+", " ").Trim();
                if (snippet.Length > 200) snippet = snippet[..200] + "...";
                var (title, link, _) = results[i];
                results[i] = (title, link, snippet);
            }
        }

        // Strategy 3: Fallback - find any external links
        if (results.Count == 0)
        {
            var linkRegex = new Regex(@"href=""(https?://(?!duckduckgo)[^""]{10,})""", RegexOptions.IgnoreCase);
            foreach (Match m in linkRegex.Matches(html).Take(10))
            {
                var l = m.Groups[1].Value.Trim();
                if (seenLinks.Add(l))
                {
                    var idx = Math.Max(0, m.Index - 80);
                    var context = StripHtml(html.Substring(idx, Math.Min(160, html.Length - idx)));
                    results.Add((context.Trim().Length > 80 ? context[..80] + "..." : context.Trim(), l, ""));
                }
            }
        }

        return results;
    }

    static List<(string Title, string Link, string Snippet)> ParseBingResults(string html)
    {
        var results = new List<(string, string, string)>();
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract b_algo blocks - each result is in a <li class="b_algo"> or <div class="b_algo">
        var algoRegex = new Regex(@"<(?:li|div)\s+class=""b_algo""[\s\S]*?</(?:li|div)>", RegexOptions.Singleline);
        foreach (Match algo in algoRegex.Matches(html).Take(10))
        {
            var block = algo.Value;
            
            // Find title in <h2>
            var titleMatch = Regex.Match(block, @"<h2[^>]*>([\s\S]*?)</h2>");
            var title = titleMatch.Success ? StripHtml(titleMatch.Groups[1].Value).Trim() : "";
            if (string.IsNullOrEmpty(title)) continue;
            
            // Find snippet in <p>
            var snippetMatch = Regex.Match(block, @"<p[^>]*>([\s\S]*?)</p>");
            var snippet = snippetMatch.Success ? StripHtml(snippetMatch.Groups[1].Value).Trim() : "";
            snippet = Regex.Replace(snippet, @"\s+", " ").Trim();
            if (snippet.Length > 200) snippet = snippet[..200] + "...";
            
            // Find link - look for aria-label which contains the domain
            var ariaMatch = Regex.Match(block, @"aria-label=""([^""]+)""");
            var domain = ariaMatch.Success ? ariaMatch.Groups[1].Value.Trim() : "";
            
            // Build link from domain
            string? mainLink = null;
            if (!string.IsNullOrEmpty(domain))
            {
                mainLink = $"https://{domain}/";
            }
            
            // Fallback: look for any external link
            if (string.IsNullOrEmpty(mainLink))
            {
                var linkMatches = Regex.Matches(block, @"href=""(https?://(?!bing\.com|microsoft\.com)[^""]+)""");
                if (linkMatches.Count > 0)
                {
                    mainLink = linkMatches[0].Groups[1].Value;
                }
            }
            
            if (string.IsNullOrEmpty(mainLink) || seenLinks.Contains(mainLink)) continue;
            seenLinks.Add(mainLink);

            results.Add((title, mainLink, snippet));
        }

        return results;
    }

    #endregion

    #region Helpers

    static Dictionary<string, McpToolParameter> ExtractParameters(Dictionary<string, object> schema)
    {
        var parameters = new Dictionary<string, McpToolParameter>();

        if (schema.TryGetValue("properties", out var propertiesObj) && propertiesObj is Dictionary<string, object> properties)
        {
            var required = new List<string>();
            if (schema.TryGetValue("required", out var reqObj) && reqObj is List<object> reqList)
                required = reqList.Select(o => o.ToString()!).ToList();

            foreach (var (key, value) in properties)
            {
                if (value is Dictionary<string, object> prop)
                {
                    var type = prop.TryGetValue("type", out var t) ? t.ToString() ?? "string" : "string";
                    var desc = prop.TryGetValue("description", out var d) ? d.ToString() ?? "" : "";
                    parameters[key] = new McpToolParameter
                    {
                        Type = type,
                        Description = desc,
                        Required = required.Contains(key)
                    };
                }
            }
        }

        return parameters;
    }

    static string StripHtml(string html)
    {
        html = Regex.Replace(html, @"<[^>]+>", " ");
        html = Regex.Replace(html, "&nbsp;", " ");
        html = Regex.Replace(html, "&amp;", "&");
        html = Regex.Replace(html, "&lt;", "<");
        html = Regex.Replace(html, "&gt;", ">");
        html = Regex.Replace(html, "&quot;", "\"");
        html = Regex.Replace(html, @"\s+", " ").Trim();
        return html;
    }

    static string HtmlToMarkdown(string html)
    {
        var text = html;

        // Remove script and style blocks FIRST (before any other processing)
        text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        // Remove HTML comments
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", "", RegexOptions.IgnoreCase);
        // Remove noscript blocks
        text = Regex.Replace(text, @"<noscript[^>]*>[\s\S]*?</noscript>", "", RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"<h1[^>]*>(.*?)</h1>", "# $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<h2[^>]*>(.*?)</h2>", "## $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<h3[^>]*>(.*?)</h3>", "### $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<h4[^>]*>(.*?)</h4>", "#### $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<p[^>]*>(.*?)</p>", "$1\n\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<a[^>]*href=""([^""]+)""[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<b[^>]*>(.*?)</b>", "**$1**", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<i[^>]*>(.*?)</i>", "*$1*", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<hr\s*/?>", "\n---\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<pre[^>]*>(.*?)</pre>", "```\n$1\n```\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<blockquote[^>]*>(.*?)</blockquote>", "> $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "", RegexOptions.Singleline);

        text = Regex.Replace(text, "&nbsp;", " ");
        text = Regex.Replace(text, "&amp;", "&");
        text = Regex.Replace(text, "&lt;", "<");
        text = Regex.Replace(text, "&gt;", ">");
        text = Regex.Replace(text, "&quot;", "\"");
        text = Regex.Replace(text, "&#39;", "'");

        text = Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");
        text = text.Trim();

        return text;
    }

    static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
        };
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_serversLock)
        {
            foreach (var client in _clients.Values)
                client.Dispose();
            _clients.Clear();
        }
    }

    /// <summary>
    /// Cleans Jina Reader / web_fetch metadata from tool results.
    /// Strips "Title:", "URL Source:", "Markdown Content:" headers and normalizes formatting.
    /// </summary>
    public static string CleanToolResult(string rawContent, string? toolName = null)
    {
        if (string.IsNullOrEmpty(rawContent))
            return rawContent ?? "";

        // 1. Strip HTML tags (web_fetch sometimes returns raw HTML)
        rawContent = Regex.Replace(rawContent, "<[^>]+>", " ");
        
        // 2. Decode HTML entities (&#160; → space, &#8212; → em-dash, etc.)
        rawContent = System.Net.WebUtility.HtmlDecode(rawContent);
        
        // 3. Detect and remove large JS/code dumps (minified scripts, tracking code, etc.)
        // If content contains heavy JS patterns, truncate it aggressively
        var jsPatterns = new[] { "function(", "window.", "document.", "var ", "const ", "let ", "=>", ".addEventListener", ".getElementById", ".querySelector" };
        if (jsPatterns.Any(p => rawContent.Contains(p)) && rawContent.Length > 2000)
        {
            // Keep only the first meaningful paragraph before the JS dump
            var paragraphs = rawContent.Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var meaningful = paragraphs.FirstOrDefault(p => 
                p.Length > 20 && p.Length < 1000 && 
                !p.Contains("function(") && !p.Contains("window.") && !p.Contains("document."));
            
            if (!string.IsNullOrEmpty(meaningful))
            {
                rawContent = meaningful.Trim();
            }
            else
            {
                // Fallback: just truncate to first 500 chars
                rawContent = rawContent.Length > 500 ? rawContent[..500] + "\n... (код обрезан)" : rawContent;
            }
        }
        
        // 4. Normalize whitespace
        rawContent = Regex.Replace(rawContent, @"\s+", " ").Trim();

        var lines = rawContent.Split('\n');
        var cleanedLines = new List<string>();
        
        bool skipMetadata = true;
        bool inContent = false;
        StringBuilder codeBlock = new();
        bool inCodeBlock = false;
        int consecutiveEmptyLines = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            
            // Inside a code block - preserve everything
            if (inCodeBlock)
            {
                codeBlock.Append(line);
                codeBlock.Append("\n");
                if (trimmed.StartsWith("```"))
                {
                    cleanedLines.Add(codeBlock.ToString().TrimEnd('\n'));
                    codeBlock = new();
                    inCodeBlock = false;
                }
                consecutiveEmptyLines = 0;
                continue;
            }
            
            // Detect code blocks
            if (trimmed.StartsWith("```"))
            {
                codeBlock = new StringBuilder(line);
                codeBlock.Append("\n");
                inCodeBlock = true;
                continue;
            }
            
            // Skip Jina Reader metadata headers at the start
            if (skipMetadata && !inContent)
            {
                if (trimmed.StartsWith("Title:") || trimmed.StartsWith("URL Source:") || 
                    trimmed.StartsWith("Markdown Content:") || trimmed.StartsWith("Warning:"))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                skipMetadata = false;
                inContent = true;
            }
            
            if (!inContent)
                continue;
            
            // Collapse consecutive empty lines (max 2)
            if (string.IsNullOrEmpty(trimmed))
            {
                consecutiveEmptyLines++;
                if (consecutiveEmptyLines <= 2)
                    cleanedLines.Add(line);
                continue;
            }
            
            consecutiveEmptyLines = 0;
            
            // Aggressive cleaning of navigation/menu artifacts
            if (trimmed.StartsWith("[[Image") || 
                trimmed.Contains("Перейти на главную") ||
                trimmed.StartsWith("//") || // Broken relative links
                trimmed.Contains("via=") || // Navigation tracking params
                Regex.IsMatch(trimmed, @"^\s*О\s+\w+$") || // Menu items like "О Пыльца"
                trimmed.Length < 15 && trimmed.Contains("карта") || // Short menu fragments
                trimmed.StartsWith("О ") && trimmed.Length < 30) // Short menu items
            {
                continue;
            }
            
            // Fix broken markdown links: `text)Link` -> `text`
            trimmed = Regex.Replace(trimmed, @"`[^`]*`", " "); // Remove code artifacts
            trimmed = Regex.Replace(trimmed, @"\)\s*\w+", " "); // Remove trailing link artifacts
            
            if (!string.IsNullOrEmpty(trimmed))
                cleanedLines.Add(trimmed);
        }
        
        var result = string.Join("\n", cleanedLines).Trim();
        
        // If nothing useful extracted, return original (truncated)
        if (result.Length < 10)
            return rawContent.Length > 2000 ? rawContent[..2000] + "\n... (сокращено)" : rawContent;
        
        // Truncate very long results
        if (result.Length > 8000)
            result = result[..8000] + "\n... (сокращено)";
        
        return result;
    }


}
