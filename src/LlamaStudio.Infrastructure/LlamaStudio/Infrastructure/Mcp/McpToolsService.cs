using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Mcp;

public class McpToolsService : IMcpToolsService, IDisposable
{
	private readonly ILogService _log;

	private readonly ISettings _settings;

	private readonly ILocalizationService? _loc;

	private readonly Dictionary<string, McpServerConfig> _servers = new Dictionary<string, McpServerConfig>();

	private readonly Dictionary<string, IMcpClient> _clients = new Dictionary<string, IMcpClient>();

	private readonly object _serversLock = new object();

	private bool _disposed;

	private static readonly List<McpToolDefinition> s_builtinTools = new List<McpToolDefinition>
	{
		new McpToolDefinition
		{
			Name = "read_file",
			Description = "Read the contents of a file from the filesystem. Returns the file content as text.",
			Parameters = new Dictionary<string, McpToolParameter> { 
			{
				"path",
				new McpToolParameter
				{
					Type = "string",
					Description = "Absolute path to the file",
					Required = true
				}
			} }
		},
		new McpToolDefinition
		{
			Name = "write_file",
			Description = "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.",
			Parameters = new Dictionary<string, McpToolParameter>
			{
				{
					"path",
					new McpToolParameter
					{
						Type = "string",
						Description = "Absolute path to the file",
						Required = true
					}
				},
				{
					"content",
					new McpToolParameter
					{
						Type = "string",
						Description = "Content to write to the file",
						Required = true
					}
				}
			}
		},
		new McpToolDefinition
		{
			Name = "list_directory",
			Description = "List files and directories in the specified path. Returns a structured listing.",
			Parameters = new Dictionary<string, McpToolParameter> { 
			{
				"path",
				new McpToolParameter
				{
					Type = "string",
					Description = "Absolute path to the directory",
					Required = true
				}
			} }
		},
		new McpToolDefinition
		{
			Name = "search_files",
			Description = "Search for files matching a glob pattern in a directory. Recursively searches subdirectories.",
			Parameters = new Dictionary<string, McpToolParameter>
			{
				{
					"directory",
					new McpToolParameter
					{
						Type = "string",
						Description = "Directory to search in",
						Required = true
					}
				},
				{
					"pattern",
					new McpToolParameter
					{
						Type = "string",
						Description = "Glob pattern to match (e.g. *.cs, **/*.json)",
						Required = true
					}
				}
			}
		},
		new McpToolDefinition
		{
			Name = "web_fetch",
			Description = "Fetch content from a URL and return it as markdown or text. Good for reading web pages.",
			Parameters = new Dictionary<string, McpToolParameter>
			{
				{
					"url",
					new McpToolParameter
					{
						Type = "string",
						Description = "URL to fetch",
						Required = true
					}
				},
				{
					"format",
					new McpToolParameter
					{
						Type = "string",
						Description = "Output format: markdown, text, or html. Default: markdown"
					}
				}
			}
		},
		new McpToolDefinition
		{
			Name = "web_search",
			Description = "Search the web using DuckDuckGo. Returns search results with titles, URLs, and snippets.",
			Parameters = new Dictionary<string, McpToolParameter> { 
			{
				"query",
				new McpToolParameter
				{
					Type = "string",
					Description = "Search query",
					Required = true
				}
			} }
		}
	};

	private string ServersConfigPath => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio"), "mcp_servers.json");

	public McpToolsService(ILogService log, ISettings settings, ILocalizationService? loc = null)
	{
		_log = log;
		_settings = settings;
		_loc = loc;
		LoadServersConfig();
	}

	public List<McpToolDefinition> GetAvailableTools()
	{
		List<McpToolDefinition> tools = new List<McpToolDefinition>(s_builtinTools);
		List<(McpServerConfig, IMcpClient)> connectedServerPairs = new List<(McpServerConfig, IMcpClient)>();
		lock (_serversLock)
		{
			foreach (var (key, serverConfig) in _servers)
			{
				if (serverConfig.Enabled && _clients.TryGetValue(key, out IMcpClient client) && client.IsConnected)
				{
					connectedServerPairs.Add((serverConfig, client));
				}
			}
		}
		foreach (var (serverConfig, mcpClient) in connectedServerPairs)
		{
			try
			{
				foreach (var (toolName, description, schema) in mcpClient.GetToolsAsync(CancellationToken.None).GetAwaiter().GetResult())
				{
					tools.Add(new McpToolDefinition
					{
						Name = serverConfig.Name + ":" + toolName,
						Description = description,
						Parameters = ExtractParameters(schema)
					});
				}
			}
			catch (Exception ex)
			{
				_log.Warning("Failed to get tools from MCP server '" + serverConfig.Name + "': " + ex.Message, "McpTools");
			}
		}
		return tools;
	}

	public async Task<McpToolResult> ExecuteToolAsync(string toolName, string argumentsJson)
	{
		try
		{
			int colonIndex = toolName.IndexOf(':');
			if (colonIndex > 0)
			{
				string serverName = toolName.Substring(0, colonIndex);
				string plainToolName = toolName.Substring(colonIndex + 1);
				return await ExecuteExternalToolAsync(serverName, plainToolName, argumentsJson);
			}
			_log.Information("MCP tool executed: " + toolName, "McpTools");
			Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson) ?? new Dictionary<string, JsonElement>();
			string content = toolName switch
			{
				"read_file" => await ReadFileAsync(args), 
				"write_file" => await WriteFileAsync(args), 
				"list_directory" => await ListDirectoryAsync(args), 
				"search_files" => await SearchFilesAsync(args), 
				"web_fetch" => await WebFetchAsync(args), 
				"web_search" => await WebSearchAsync(args), 
				_ => "Error: Unknown tool '" + toolName + "'", 
			};
			return new McpToolResult
			{
				ToolName = toolName,
				Content = content,
				IsError = false
			};
		}
		catch (Exception ex)
		{
			_log.Error(ex, "MCP tool error: " + toolName, "McpTools");
			return new McpToolResult
			{
				ToolName = toolName,
				Content = "Error executing " + toolName + ": " + ex.Message,
				IsError = true
			};
		}
	}

	public List<McpServerConfig> GetMcpServers()
	{
		lock (_serversLock)
		{
			return _servers.Values.ToList();
		}
	}

	public async Task AddMcpServerAsync(McpServerConfig config)
	{
		lock (_serversLock)
		{
			_servers[config.Id] = config;
		}
		if (config.Enabled)
		{
			await ConnectServerAsync(config);
		}
		SaveServersConfig();
	}

	public async Task RemoveMcpServerAsync(string id)
	{
		lock (_serversLock)
		{
			if (_clients.TryGetValue(id, out IMcpClient client))
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
		McpServerConfig serverConfig = null;
		lock (_serversLock)
		{
			if (!_servers.TryGetValue(id, out serverConfig))
			{
				return;
			}
			serverConfig.Enabled = enabled;
		}
		if (enabled)
		{
			await ConnectServerAsync(serverConfig);
		}
		else
		{
			lock (_serversLock)
			{
				if (_clients.TryGetValue(id, out IMcpClient client))
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
		List<McpServerConfig> enabledServers = new List<McpServerConfig>();
		lock (_serversLock)
		{
			foreach (var (key, serverConfig) in _servers.Where<KeyValuePair<string, McpServerConfig>>((KeyValuePair<string, McpServerConfig> kvp) => kvp.Value.Enabled))
			{
				if (_clients.TryGetValue(key, out IMcpClient client))
				{
					client.Dispose();
					_clients.Remove(key);
				}
				enabledServers.Add(serverConfig);
			}
		}
		foreach (McpServerConfig server in enabledServers)
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
				_log.Warning("MCP server '" + server.Name + "' reconnect failed: " + ex.Message, "McpTools");
			}
		}
	}

	private async Task ConnectServerAsync(McpServerConfig config)
	{
		try
		{
			IMcpClient mcpClient = ((config.TransportType != McpTransportType.Sse) ? ((IMcpClient)new McpStdioClient(config.Name, config.Command, config.Args, config.Env)) : ((IMcpClient)new McpSseClient(config.Name, config.Url)));
			IMcpClient client = mcpClient;
			List<(string, string, Dictionary<string, object>)> tools = await client.GetToolsAsync();
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
			string errorMessage = NormalizeErrorMessage(ex, config);
			lock (_serversLock)
			{
				if (_clients.ContainsKey(config.Id))
				{
					_clients[config.Id].Dispose();
					_clients.Remove(config.Id);
				}
				config.IsConnected = false;
				config.LastError = errorMessage;
			}
			_log.Error("MCP server '" + config.Name + "' connection failed: " + errorMessage, "McpTools");
		}
	}

	private string NormalizeErrorMessage(Exception ex, McpServerConfig config)
	{
		string message = ex.Message;
		if (config.TransportType == McpTransportType.Stdio && (ex is Win32Exception || (message.Contains("trying to start process") && message.Contains("Не удается найти"))))
		{
			return string.Format(_loc?.T("mcp.cmd_not_found") ?? "Command \"{0}\" not found in PATH. Install Node.js from https://nodejs.org (choose LTS), then restart LlamaStudio.", config.Command);
		}
		if (config.TransportType == McpTransportType.Sse && (message.Contains("connect") || message.Contains("connection") || message.Contains(" refused")))
		{
			return string.Format(_loc?.T("mcp.sse_connect_failed") ?? "Failed to connect to SSE server at {0}. Check that the server is running and accessible.", config.Url);
		}
		return Regex.Replace(message, "[\\x00-\\x1F]", " ");
	}

	private async Task<McpToolResult> ExecuteExternalToolAsync(string serverName, string toolName, string argumentsJson)
	{
		IMcpClient client = null;
		lock (_serversLock)
		{
			McpServerConfig mcpServerConfig = _servers.Values.FirstOrDefault((McpServerConfig s) => s.Name == serverName);
			if (mcpServerConfig == null)
			{
				return new McpToolResult
				{
					ToolName = serverName + ":" + toolName,
					Content = "Error: MCP server '" + serverName + "' not found",
					IsError = true
				};
			}
			if (!_clients.TryGetValue(mcpServerConfig.Id, out client))
			{
				return new McpToolResult
				{
					ToolName = serverName + ":" + toolName,
					Content = "Error: MCP server '" + serverName + "' not connected",
					IsError = true
				};
			}
		}
		try
		{
			Dictionary<string, object> arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson) ?? new Dictionary<string, object>();
			string content = await client.CallToolAsync(toolName, arguments);
			return new McpToolResult
			{
				ToolName = serverName + ":" + toolName,
				Content = content,
				IsError = false
			};
		}
		catch (Exception ex)
		{
			_log.Error(ex, "MCP external tool error: " + serverName + ":" + toolName, "McpTools");
			return new McpToolResult
			{
				ToolName = serverName + ":" + toolName,
				Content = "Error: " + ex.Message,
				IsError = true
			};
		}
	}

	private void LoadServersConfig()
	{
		if (!File.Exists(ServersConfigPath))
		{
			return;
		}
		try
		{
			List<McpServerConfig> servers = JsonSerializer.Deserialize<List<McpServerConfig>>(File.ReadAllText(ServersConfigPath, Encoding.UTF8));
			if (servers == null)
			{
				return;
			}
			lock (_serversLock)
			{
				foreach (McpServerConfig server in servers)
				{
					server.IsConnected = false;
					_servers[server.Id] = server;
				}
			}
			lock (_serversLock)
			{
				foreach (McpServerConfig server in _servers.Values.Where((McpServerConfig s) => s.Enabled))
				{
					ConnectServerAsync(server).ContinueWith(delegate(Task t)
					{
						if (t.IsFaulted)
						{
							_log.Error(t.Exception.InnerException ?? t.Exception, "MCP server init: " + server.Name, "McpTools");
						}
					});
				}
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to load MCP servers config", "McpTools");
		}
	}

	private void SaveServersConfig()
	{
		try
		{
			List<McpServerConfig> servers = new List<McpServerConfig>();
			lock (_serversLock)
			{
				foreach (McpServerConfig server in _servers.Values)
				{
					servers.Add(new McpServerConfig
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
			string contents = JsonSerializer.Serialize(servers, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			Directory.CreateDirectory(Path.GetDirectoryName(ServersConfigPath));
			File.WriteAllText(ServersConfigPath, contents, Encoding.UTF8);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to save MCP servers config", "McpTools");
		}
	}

	private async Task<string> ReadFileAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("path", out var pathValue) || string.IsNullOrWhiteSpace(pathValue.GetString()))
		{
			return "Error: 'path' parameter is required";
		}
		string path = pathValue.GetString();
		if (!File.Exists(path))
		{
			return "Error: File not found: " + path;
		}
		string fileContent = await File.ReadAllTextAsync(path);
		if (fileContent.Length > 50000)
		{
			return fileContent.Substring(0, 50000) + "\n\n... (truncated, file too large)";
		}
		return fileContent;
	}

	private async Task<string> WriteFileAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("path", out var pathValue) || string.IsNullOrWhiteSpace(pathValue.GetString()))
		{
			return "Error: 'path' parameter is required";
		}
		if (!args.TryGetValue("content", out var contentValue))
		{
			return "Error: 'content' parameter is required";
		}
		string path = pathValue.GetString();
		string content = contentValue.GetString() ?? string.Empty;
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		await File.WriteAllTextAsync(path, content);
		return $"Successfully wrote {content.Length} characters to {path}";
	}

	private async Task<string> ListDirectoryAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("path", out var pathValue) || string.IsNullOrWhiteSpace(pathValue.GetString()))
		{
			return "Error: 'path' parameter is required";
		}
		string directory = pathValue.GetString();
		if (!Directory.Exists(directory))
		{
			return "Error: Directory not found: " + directory;
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("Directory: ");
		handler.AppendFormatted(directory);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine(new string('-', 50));
		try
		{
			string[] directories = Directory.GetDirectories(directory);
			string[] files = Directory.GetFiles(directory);
			foreach (string subDirectory in directories.OrderBy((string x) => x))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
				handler.AppendLiteral("[DIR]  ");
				handler.AppendFormatted(Path.GetFileName(subDirectory));
				stringBuilder4.AppendLine(ref handler);
			}
			foreach (string file in files.OrderBy((string x) => x))
			{
				try
				{
					string size = FormatSize(new FileInfo(file).Length);
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
					handler.AppendLiteral("[FILE] ");
					handler.AppendFormatted<string>(size, 12);
					handler.AppendLiteral("  ");
					handler.AppendFormatted(Path.GetFileName(file));
					stringBuilder5.AppendLine(ref handler);
				}
				catch
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder6 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
					handler.AppendLiteral("[FILE] ");
					handler.AppendFormatted(Path.GetFileName(file));
					stringBuilder6.AppendLine(ref handler);
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
			return "Error: Access denied to " + directory;
		}
		return stringBuilder.ToString();
	}

	private async Task<string> SearchFilesAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("directory", out var directoryValue) || string.IsNullOrWhiteSpace(directoryValue.GetString()))
		{
			return "Error: 'directory' parameter is required";
		}
		if (!args.TryGetValue("pattern", out var patternValue) || string.IsNullOrWhiteSpace(patternValue.GetString()))
		{
			return "Error: 'pattern' parameter is required";
		}
		string directory = directoryValue.GetString();
		string pattern = patternValue.GetString();
		if (!Directory.Exists(directory))
		{
			return "Error: Directory not found: " + directory;
		}
		try
		{
			string searchPattern = pattern.Replace("**/", "");
			string[] files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
			if (files.Length == 0)
			{
				return "No files matching '" + pattern + "' found in " + directory;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, stringBuilder2);
			handler.AppendLiteral("Found ");
			handler.AppendFormatted(files.Length);
			handler.AppendLiteral(" file(s) matching '");
			handler.AppendFormatted(pattern);
			handler.AppendLiteral("':");
			stringBuilder3.AppendLine(ref handler);
			stringBuilder.AppendLine(new string('-', 50));
			foreach (string file in files.Take(100))
			{
				stringBuilder.AppendLine(file);
			}
			if (files.Length > 100)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
				handler.AppendLiteral("... and ");
				handler.AppendFormatted(files.Length - 100);
				handler.AppendLiteral(" more");
				stringBuilder4.AppendLine(ref handler);
			}
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return "Error searching files: " + ex.Message;
		}
	}

	private async Task<string> WebFetchAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("url", out var urlValue) || string.IsNullOrWhiteSpace(urlValue.GetString()))
		{
			return "Error: 'url' parameter is required";
		}
		string url = urlValue.GetString();
		string format = "text";
		if (args.TryGetValue("format", out var formatValue))
		{
			format = formatValue.GetString() ?? "text";
		}
		if (!url.StartsWith("http://") && !url.StartsWith("https://"))
		{
			url = "https://" + url;
		}
		try
		{
			string jinaContent = await TryJinaReaderAsync(url);
			if (!string.IsNullOrEmpty(jinaContent) && jinaContent.Length > 50)
			{
				string cleanedContent = CleanToolResult(jinaContent);
				return (cleanedContent.Length > 2000) ? cleanedContent.Substring(0, 2000) : cleanedContent;
			}
			HttpClientHandler handler = new HttpClientHandler
			{
				AllowAutoRedirect = true,
				MaxAutomaticRedirections = 5
			};
			using HttpClient client = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");
			HttpResponseMessage httpResponseMessage = await client.GetAsync(url);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return $"Error: HTTP {httpResponseMessage.StatusCode}";
			}
			string html = await httpResponseMessage.Content.ReadAsStringAsync();
			if (html.Contains("<script") && !html.Contains("<title>"))
			{
				return "Error: Page is JavaScript-rendered, cannot extract content";
			}
			if (format == "html")
			{
				return (html.Length > 2000) ? html.Substring(0, 2000) : html;
			}
			List<string> lines = (from l in Regex.Replace(Regex.Replace(ExtractMainContent(html), "\\n{3,}", "\n\n"), "[ \\t]+", " ").Split('\n')
				select l.Trim() into l
				where !string.IsNullOrEmpty(l) && l.Length > 3
				select l).ToList();
			string textContent = string.Join("\n", lines).Trim();
			return (textContent.Length > 2000) ? (textContent.Substring(0, 2000) + "...") : textContent;
		}
		catch (Exception ex)
		{
			return "Error fetching " + url + ": " + ex.Message;
		}
	}

	private static string ExtractMainContent(string html)
	{
		string input = Regex.Replace(html, "<script[^>]*>[\\s\\S]*?</script>", "");
		input = Regex.Replace(input, "<style[^>]*>[\\s\\S]*?</style>", "");
		input = Regex.Replace(input, "<!--[\\s\\S]*?-->", "");
		Match match = Regex.Match(input, "<(?:main|article|div)[^>]*class=\"[^\"]*(?:content|main|post|article|entry)[^\"]*\"[^>]*>([\\s\\S]*?)</(?:main|article|div)>", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			return match.Groups[1].Value;
		}
		return StripHtml(input);
	}

	private async Task<string?> TryJinaReaderAsync(string url)
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			string requestUri = "https://r.jina.ai/" + url;
			HttpResponseMessage httpResponseMessage = await client.GetAsync(requestUri);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return null;
			}
			string response = await httpResponseMessage.Content.ReadAsStringAsync();
			if (string.IsNullOrWhiteSpace(response) || response.Length < 50)
			{
				return null;
			}
			response = CleanJinaOutput(response);
			if (response.Length < 30)
			{
				return null;
			}
			_log.Information($"[web_fetch] Jina reader success for {url} ({response.Length} chars cleaned)", "McpTools");
			return response;
		}
		catch (Exception ex)
		{
			_log.Debug("[web_fetch] Jina reader failed: " + ex.Message + ", using fallback", "McpTools");
			return null;
		}
	}

	private static string CleanJinaOutput(string content)
	{
		content = Regex.Replace(content, "\\{[^}]*\"@context\"[^}]*\\}[^}]*(?:\\}[^}]*)*\\}", "", RegexOptions.Singleline);
		content = Regex.Replace(content, "\\{[^}]*\"@type\"[^}]*\\}[^}]*\\}", "", RegexOptions.Singleline);
		content = Regex.Replace(content, "\\{[^}]*\"type\"[^}]*\"FAQPage\"[^}]*\\}[\\s\\S]*?(?:\\{[^}]*\\}[\\s\\S]*)*\\}", "", RegexOptions.Singleline);
		content = Regex.Replace(content, "\"(acceptedAnswer|Question|Answer)\"[^}]*\\}[^}]*\\}", "", RegexOptions.Singleline);
		content = Regex.Replace(content, "!\\[Image \\d+[:^\\]]*\\]\\([^)]*\\)", "", RegexOptions.IgnoreCase);
		content = Regex.Replace(content, "(?:Погода|Ветер|Давление|Влажность|Пыльца|Луна|Прогноз)[^:\\n]*:[^,\\n]*(?:,?[^,\\n]*)*", "", RegexOptions.IgnoreCase);
		content = Regex.Replace(content, "^\\s*(?:Погода|Ветер|Давление|Влажность|Пыльца|Луна|Прогноз|Карта|Журнал|FAQ|На месяц|На карту|На 10 дней|На сегодня|На завтра)[\\s:]*$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
		content = Regex.Replace(content, "(?:^|\\n)https?://[^\\s,;)}\\]]+", "", RegexOptions.Multiline);
		content = Regex.Replace(content, "^\\s*\\[Image \\d+[\\]^:]*.*$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
		content = Regex.Replace(content, "\\n{3,}", "\n\n");
		content = Regex.Replace(content, "[ \\t]+", " ");
		List<string> lines = (from l in content.Split('\n')
			select l.Trim() into l
			where !string.IsNullOrEmpty(l)
			select l).ToList();
		content = string.Join("\n", lines);
		return content.Trim();
	}

	private async Task<string> WebSearchAsync(Dictionary<string, JsonElement> args)
	{
		if (!args.TryGetValue("query", out var queryValue) || string.IsNullOrWhiteSpace(queryValue.GetString()))
		{
			return "Error: 'query' parameter is required";
		}
		string query = queryValue.GetString();
		try
		{
			HttpClient httpClient = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
			List<(string, string, string)> searchResults = TryDuckDuckGoAsync(httpClient, query);
			if (searchResults.Count > 0)
			{
				return FormatSearchResults(query, searchResults);
			}
			List<(string, string, string)> bingResults = await TryBingSearchAsync(httpClient, query);
			if (bingResults.Count > 0)
			{
				return FormatSearchResults(query, bingResults);
			}
			return "No results found for '" + query + "'";
		}
		catch (Exception ex)
		{
			_log.Error(ex, "[web_search] Error searching '" + query + "': " + ex.Message, "McpTools");
			return "Error searching '" + query + "': " + ex.Message;
		}
	}

	private static List<(string Title, string Link, string Snippet)> TryDuckDuckGoAsync(HttpClient client, string query)
	{
		return new List<(string, string, string)>();
	}

	private async Task<List<(string Title, string Link, string Snippet)>> TryBingSearchAsync(HttpClient client, string query)
	{
		try
		{
			string escapedQuery = Uri.EscapeDataString(query);
			string requestUri = "https://www.bing.com/search?q=" + escapedQuery + "&setlang=ru-RU";
			HttpClientHandler handler = new HttpClientHandler
			{
				AllowAutoRedirect = true,
				MaxAutomaticRedirections = 5
			};
			using HttpClient bingClient = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromSeconds(20.0)
			};
			bingClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			bingClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			bingClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
			string html = await bingClient.GetStringAsync(requestUri);
			_log.Debug($"[web_search] Bing response length: {html.Length}", "McpTools");
			List<(string, string, string)> results = ParseBingResults(html);
			_log.Information($"[web_search] Bing returned {results.Count} results for '{query}'", "McpTools");
			return results;
		}
		catch (Exception ex)
		{
			_log.Warning("[web_search] Bing failed: " + ex.Message, "McpTools");
			return new List<(string, string, string)>();
		}
	}

	private static string FormatSearchResults(string query, List<(string Title, string Link, string Snippet)> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
		handler.AppendLiteral("Search results for '");
		handler.AppendFormatted(query);
		handler.AppendLiteral("':");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine(new string('-', 50));
		for (int i = 0; i < results.Count; i++)
		{
			(string Title, string Link, string Snippet) tuple = results[i];
			string title = tuple.Title;
			string link = tuple.Link;
			string snippet = tuple.Snippet;
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
			handler.AppendLiteral("\n");
			handler.AppendFormatted(i + 1);
			handler.AppendLiteral(". ");
			handler.AppendFormatted(title);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
			handler.AppendLiteral("   ");
			handler.AppendFormatted(link);
			stringBuilder5.AppendLine(ref handler);
			if (!string.IsNullOrWhiteSpace(snippet))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
				handler.AppendLiteral("   ");
				handler.AppendFormatted(snippet);
				stringBuilder6.AppendLine(ref handler);
			}
		}
		return stringBuilder.ToString();
	}

	private static List<(string Title, string Link, string Snippet)> ParseDuckDuckGoResults(string html)
	{
		List<(string, string, string)> results = new List<(string, string, string)>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in new Regex("<a[^>]*class=\"[^\"]*result[^\"]*\"[^>]*href=\"([^\"]+)\"[^>]*>([\\s\\S]*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Matches(html).Take(10))
		{
			string url = match.Groups[1].Value.Trim();
			if (url.StartsWith("//"))
			{
				url = "https:" + url;
			}
			if (url.Contains("duckduckgo.com") || url.Contains("duckduckgo.com/") || hashSet.Contains(url))
			{
				continue;
			}
			hashSet.Add(url);
			string title = StripHtml(match.Groups[2].Value).Trim();
			if (!string.IsNullOrEmpty(title) && title.Length >= 5)
			{
				if (title.Length > 150)
				{
					title = title.Substring(0, 150) + "...";
				}
				results.Add((title, url, ""));
			}
		}
		if (results.Count > 0)
		{
			List<string> snippets = (from m in new Regex("class=\"result__snippet\"[^>]*>([\\s\\S]*?)</", RegexOptions.Singleline).Matches(html)
				select StripHtml(m.Groups[1].Value).Trim() into s
				where s.Length > 10
				select s).ToList();
			for (int i = 0; i < results.Count && i < snippets.Count; i++)
			{
				string snippet = Regex.Replace(snippets[i], "\\s+", " ").Trim();
				if (snippet.Length > 200)
				{
					snippet = snippet.Substring(0, 200) + "...";
				}
				(string, string, string) tuple = results[i];
				string title = tuple.Item1;
				string url = tuple.Item2;
				results[i] = (title, url, snippet);
			}
		}
		if (results.Count == 0)
		{
			foreach (Match match in new Regex("href=\"(https?://(?!duckduckgo)[^\"]{10,})\"", RegexOptions.IgnoreCase).Matches(html).Take(10))
			{
				string url = match.Groups[1].Value.Trim();
				if (hashSet.Add(url))
				{
					int startIndex = Math.Max(0, match.Index - 80);
					string contextText = StripHtml(html.Substring(startIndex, Math.Min(160, html.Length - startIndex)));
					results.Add(((contextText.Trim().Length > 80) ? (contextText.Substring(0, 80) + "...") : contextText.Trim(), url, ""));
				}
			}
		}
		return results;
	}

	private static List<(string Title, string Link, string Snippet)> ParseBingResults(string html)
	{
		List<(string, string, string)> results = new List<(string, string, string)>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match resultMatch in new Regex("<(?:li|div)\\s+class=\"b_algo\"[\\s\\S]*?</(?:li|div)>", RegexOptions.Singleline).Matches(html).Take(10))
		{
			string resultHtml = resultMatch.Value;
			Match match = Regex.Match(resultHtml, "<h2[^>]*>([\\s\\S]*?)</h2>");
			string title = (match.Success ? StripHtml(match.Groups[1].Value).Trim() : "");
			if (string.IsNullOrEmpty(title))
			{
				continue;
			}
			Match snippetMatch = Regex.Match(resultHtml, "<p[^>]*>([\\s\\S]*?)</p>");
			string snippet = (snippetMatch.Success ? StripHtml(snippetMatch.Groups[1].Value).Trim() : "");
			snippet = Regex.Replace(snippet, "\\s+", " ").Trim();
			if (snippet.Length > 200)
			{
				snippet = snippet.Substring(0, 200) + "...";
			}
			Match labelMatch = Regex.Match(resultHtml, "aria-label=\"([^\"]+)\"");
			string domain = (labelMatch.Success ? labelMatch.Groups[1].Value.Trim() : "");
			string link = null;
			if (!string.IsNullOrEmpty(domain))
			{
				link = "https://" + domain + "/";
			}
			if (string.IsNullOrEmpty(link))
			{
				MatchCollection matchCollection = Regex.Matches(resultHtml, "href=\"(https?://(?!bing\\.com|microsoft\\.com)[^\"]+)\"");
				if (matchCollection.Count > 0)
				{
					link = matchCollection[0].Groups[1].Value;
				}
			}
			if (!string.IsNullOrEmpty(link) && !hashSet.Contains(link))
			{
				hashSet.Add(link);
				results.Add((title, link, snippet));
			}
		}
		return results;
	}

	private static Dictionary<string, McpToolParameter> ExtractParameters(Dictionary<string, object> schema)
	{
		Dictionary<string, McpToolParameter> parameters = new Dictionary<string, McpToolParameter>();
		if (schema.TryGetValue("properties", out object propertiesValue) && propertiesValue is Dictionary<string, object> properties)
		{
			List<string> required = new List<string>();
			if (schema.TryGetValue("required", out object requiredValue) && requiredValue is List<object> requiredList)
			{
				required = requiredList.Select((object o) => o.ToString()).ToList();
			}
			foreach (var (parameterName, parameterValue) in properties)
			{
				if (parameterValue is Dictionary<string, object> parameterSchema)
				{
					object typeValue;
					string type = (parameterSchema.TryGetValue("type", out typeValue) ? (typeValue.ToString() ?? "string") : "string");
					object descriptionValue;
					string description = (parameterSchema.TryGetValue("description", out descriptionValue) ? (descriptionValue.ToString() ?? "") : "");
					parameters[parameterName] = new McpToolParameter
					{
						Type = type,
						Description = description,
						Required = required.Contains(parameterName)
					};
				}
			}
		}
		return parameters;
	}

	private static string StripHtml(string html)
	{
		html = Regex.Replace(html, "<[^>]+>", " ");
		html = Regex.Replace(html, "&nbsp;", " ");
		html = Regex.Replace(html, "&amp;", "&");
		html = Regex.Replace(html, "&lt;", "<");
		html = Regex.Replace(html, "&gt;", ">");
		html = Regex.Replace(html, "&quot;", "\"");
		html = Regex.Replace(html, "\\s+", " ").Trim();
		return html;
	}

	private static string HtmlToMarkdown(string html)
	{
		return Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(html, "<script[^>]*>[\\s\\S]*?</script>", "", RegexOptions.IgnoreCase), "<style[^>]*>[\\s\\S]*?</style>", "", RegexOptions.IgnoreCase), "<!--[\\s\\S]*?-->", "", RegexOptions.IgnoreCase), "<noscript[^>]*>[\\s\\S]*?</noscript>", "", RegexOptions.IgnoreCase), "<h1[^>]*>(.*?)</h1>", "# $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<h2[^>]*>(.*?)</h2>", "## $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<h3[^>]*>(.*?)</h3>", "### $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<h4[^>]*>(.*?)</h4>", "#### $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<p[^>]*>(.*?)</p>", "$1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<a[^>]*href=\"([^\"]+)\"[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<b[^>]*>(.*?)</b>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<i[^>]*>(.*?)</i>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<br\\s*/?>", "\n", RegexOptions.IgnoreCase), "<hr\\s*/?>", "\n---\n", RegexOptions.IgnoreCase), "<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<pre[^>]*>(.*?)</pre>", "```\n$1\n```\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<blockquote[^>]*>(.*?)</blockquote>", "> $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline), "<[^>]+>", "", RegexOptions.Singleline), "&nbsp;", " "), "&amp;", "&"), "&lt;", "<"), "&gt;", ">"), "&quot;", "\""), "&#39;", "'"), "\\n\\s*\\n\\s*\\n", "\n\n").Trim();
	}

	private static string FormatSize(long bytes)
	{
		if (bytes < 1048576)
		{
			if (bytes < 1024)
			{
				return $"{bytes} B";
			}
			return $"{(double)bytes / 1024.0:F1} KB";
		}
		if (bytes < 1073741824)
		{
			return $"{(double)bytes / 1048576.0:F1} MB";
		}
		return $"{(double)bytes / 1073741824.0:F1} GB";
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		lock (_serversLock)
		{
			foreach (IMcpClient client in _clients.Values)
			{
				client.Dispose();
			}
			_clients.Clear();
		}
	}

	public static string CleanToolResult(string rawContent, string? toolName = null)
	{
		if (string.IsNullOrEmpty(rawContent))
		{
			return rawContent ?? "";
		}
		rawContent = Regex.Replace(rawContent, "<[^>]+>", " ");
		rawContent = WebUtility.HtmlDecode(rawContent);
		if (new string[10] { "function(", "window.", "document.", "var ", "const ", "let ", "=>", ".addEventListener", ".getElementById", ".querySelector" }.Any((string p) => rawContent.Contains(p)) && rawContent.Length > 2000)
		{
			string paragraph = rawContent.Split(new string[2] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault((string p) => p.Length > 20 && p.Length < 1000 && !p.Contains("function(") && !p.Contains("window.") && !p.Contains("document."));
			if (!string.IsNullOrEmpty(paragraph))
			{
				rawContent = paragraph.Trim();
			}
			else
			{
				rawContent = ((rawContent.Length > 500) ? (rawContent.Substring(0, 500) + "\n... (код обрезан)") : rawContent);
			}
		}
		rawContent = Regex.Replace(rawContent, "\\s+", " ").Trim();
		string[] lines = rawContent.Split('\n');
		List<string> resultLines = new List<string>();
		bool inHeader = true;
		bool contentStarted = false;
		StringBuilder stringBuilder = new StringBuilder();
		bool inCodeBlock = false;
		int blankLineCount = 0;
		foreach (string line in lines)
		{
			string trimmedLine = line.Trim();
			if (inCodeBlock)
			{
				stringBuilder.Append(line);
				stringBuilder.Append("\n");
				if (trimmedLine.StartsWith("```"))
				{
					resultLines.Add(stringBuilder.ToString().TrimEnd('\n'));
					stringBuilder = new StringBuilder();
					inCodeBlock = false;
				}
				blankLineCount = 0;
				continue;
			}
			if (trimmedLine.StartsWith("```"))
			{
				stringBuilder = new StringBuilder(line);
				stringBuilder.Append("\n");
				inCodeBlock = true;
				continue;
			}
			if (inHeader && !contentStarted)
			{
				if (trimmedLine.StartsWith("Title:") || trimmedLine.StartsWith("URL Source:") || trimmedLine.StartsWith("Markdown Content:") || trimmedLine.StartsWith("Warning:") || string.IsNullOrEmpty(trimmedLine))
				{
					continue;
				}
				inHeader = false;
				contentStarted = true;
			}
			if (!contentStarted)
			{
				continue;
			}
			if (string.IsNullOrEmpty(trimmedLine))
			{
				blankLineCount++;
				if (blankLineCount <= 2)
				{
					resultLines.Add(line);
				}
				continue;
			}
			blankLineCount = 0;
			if (!trimmedLine.StartsWith("[[Image") && !trimmedLine.Contains("Перейти на главную") && !trimmedLine.StartsWith("//") && !trimmedLine.Contains("via=") && !Regex.IsMatch(trimmedLine, "^\\s*О\\s+\\w+$") && (trimmedLine.Length >= 15 || !trimmedLine.Contains("карта")) && (!trimmedLine.StartsWith("О ") || trimmedLine.Length >= 30))
			{
				trimmedLine = Regex.Replace(trimmedLine, "`[^`]*`", " ");
				trimmedLine = Regex.Replace(trimmedLine, "\\)\\s*\\w+", " ");
				if (!string.IsNullOrEmpty(trimmedLine))
				{
					resultLines.Add(trimmedLine);
				}
			}
		}
		string result = string.Join("\n", resultLines).Trim();
		if (result.Length < 10)
		{
			if (rawContent.Length <= 2000)
			{
				return rawContent;
			}
			return rawContent.Substring(0, 2000) + "\n... (сокращено)";
		}
		if (result.Length > 8000)
		{
			result = result.Substring(0, 8000) + "\n... (сокращено)";
		}
		return result;
	}
}
