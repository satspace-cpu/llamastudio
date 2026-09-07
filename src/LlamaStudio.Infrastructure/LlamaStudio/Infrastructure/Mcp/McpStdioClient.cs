using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Infrastructure.Mcp;

public class McpStdioClient : IMcpClient, IDisposable
{
	private readonly Process _process;

	private readonly StreamWriter _writer;

	private readonly StreamReader _reader;

	private readonly object _lock = new object();

	private int _requestId;

	private bool _disposed;

	private readonly List<(string Name, string Description, Dictionary<string, object> InputSchema)> _cachedTools = new List<(string, string, Dictionary<string, object>)>();

	public string ServerName { get; }

	public bool IsConnected => !_process.HasExited;

	public McpStdioClient(string name, string command, List<string>? args = null, Dictionary<string, string>? env = null)
	{
		ServerName = name;
		(string FileName, List<string>? WrapperArgs) resolved = ResolveCommand(command);
		string fileName = resolved.FileName;
		List<string> wrapperArgs = resolved.WrapperArgs;
		ProcessStartInfo processStartInfo = new ProcessStartInfo
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
		if (wrapperArgs != null)
		{
			List<string> argList = new List<string>(wrapperArgs);
			if (args != null)
			{
				argList.AddRange(args);
			}
			processStartInfo.Arguments = string.Join(" ", argList.Select((string a) => (!a.Contains(" ")) ? a : ("\"" + a + "\"")));
		}
		else if (args != null)
		{
			foreach (string arg in args)
			{
				processStartInfo.ArgumentList.Add(arg);
			}
		}
		if (env != null)
		{
			foreach (var (key, value) in env)
			{
				processStartInfo.EnvironmentVariables[key] = value;
			}
		}
		_process = new Process
		{
			StartInfo = processStartInfo
		};
		_process.Start();
		_writer = _process.StandardInput;
		_reader = _process.StandardOutput;
	}

	private (string FileName, List<string>? WrapperArgs) ResolveCommand(string command)
	{
		if (command.Contains('.') || Path.IsPathRooted(command) || command.Contains('\\') || command.Contains('/'))
		{
			return (FileName: command, WrapperArgs: null);
		}
		string[] pathEntries = Environment.GetEnvironmentVariable("PATH")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
		string[] pathExtensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? new string[4] { ".COM", ".EXE", ".BAT", ".CMD" };
		string[] entries = pathEntries;
		for (int i = 0; i < entries.Length; i++)
		{
			string candidatePath = Path.Combine(entries[i], command);
			if (File.Exists(candidatePath))
			{
				return (FileName: candidatePath, WrapperArgs: null);
			}
		}
		entries = pathExtensions;
		foreach (string extension in entries)
		{
			string[] pathDirectories = pathEntries;
			for (int j = 0; j < pathDirectories.Length; j++)
			{
				string candidatePath = Path.Combine(pathDirectories[j], command + extension);
				if (File.Exists(candidatePath))
				{
					if (extension.Equals(".BAT", StringComparison.OrdinalIgnoreCase) || extension.Equals(".CMD", StringComparison.OrdinalIgnoreCase))
					{
						return (FileName: "cmd.exe", WrapperArgs: new List<string> { "/c", candidatePath });
					}
					return (FileName: candidatePath, WrapperArgs: null);
				}
			}
		}
		return (FileName: command, WrapperArgs: null);
	}

	public async Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_cachedTools.Count > 0)
		{
			return _cachedTools;
		}
		try
		{
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
			await SendNotificationAsync("notifications/initialized", new Dictionary<string, object>(), ct);
			Dictionary<string, JsonElement> response = await SendRequestAsync("tools/list", new Dictionary<string, object>(), ct);
			if (response != null && response.TryGetValue("result", out var result) && JsonDocument.Parse(result.ToString() ?? "{}").RootElement.TryGetProperty("tools", out var tools))
			{
				_cachedTools.Clear();
				foreach (JsonElement tool in tools.EnumerateArray())
				{
					JsonElement nameElement;
					string toolName = (tool.TryGetProperty("name", out nameElement) ? (nameElement.GetString() ?? "") : "");
					JsonElement descriptionElement;
					string toolDescription = (tool.TryGetProperty("description", out descriptionElement) ? (descriptionElement.GetString() ?? "") : "");
					Dictionary<string, object> inputSchema = new Dictionary<string, object>();
					if (tool.TryGetProperty("inputSchema", out var schemaElement))
					{
						inputSchema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaElement.GetRawText()) ?? new Dictionary<string, object>();
					}
					_cachedTools.Add((toolName, toolDescription, inputSchema));
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("MCP server '" + ServerName + "' initialization failed: " + ex.Message, ex);
		}
		return _cachedTools;
	}

	public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default(CancellationToken))
	{
		Dictionary<string, JsonElement> response = await SendRequestAsync("tools/call", new Dictionary<string, object>
		{
			["name"] = toolName,
			["arguments"] = arguments
		}, ct);
		if (response == null)
		{
			return "Error: No response from MCP server";
		}
		if (response.TryGetValue("result", out var result) && JsonDocument.Parse(result.ToString() ?? "{}").RootElement.TryGetProperty("content", out var content))
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (JsonElement contentItem in content.EnumerateArray())
			{
				JsonElement typeElement;
				string contentType = (contentItem.TryGetProperty("type", out typeElement) ? (typeElement.GetString() ?? "") : "");
				JsonElement dataElement;
				if (contentType == "text" && contentItem.TryGetProperty("text", out var textElement))
				{
					stringBuilder.AppendLine(textElement.GetString());
				}
				else if (contentType == "image" && contentItem.TryGetProperty("data", out dataElement))
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
					handler.AppendLiteral("[Image: ");
					handler.AppendFormatted(dataElement.GetString()?.Length ?? 0);
					handler.AppendLiteral(" bytes]");
					stringBuilder2.AppendLine(ref handler);
				}
			}
			return stringBuilder.ToString().Trim();
		}
		if (response.TryGetValue("error", out var error))
		{
			return $"Error: {error}";
		}
		return "Error: Unexpected response for tool '" + toolName + "'";
	}

	private async Task<Dictionary<string, JsonElement>> SendRequestAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
	{
		int id = Interlocked.Increment(ref _requestId);
		string requestJson = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = id,
			method = method,
			@params = @params
		}) + "\n";
		lock (_lock)
		{
			_writer.WriteLine(requestJson);
			_writer.Flush();
		}
		while (!ct.IsCancellationRequested)
		{
			string line = await _reader.ReadLineAsync(ct);
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}
			try
			{
				JsonDocument jsonDocument = JsonDocument.Parse(line);
				if (jsonDocument.RootElement.TryGetProperty("id", out var responseId) && responseId.GetInt32() == id)
				{
					return ParseJsonDocument(jsonDocument);
				}
			}
			catch
			{
			}
		}
		throw new TimeoutException("MCP request timeout: " + method);
	}

	private async Task SendNotificationAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
	{
		string notificationJson = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = method,
			@params = @params
		}) + "\n";
		lock (_lock)
		{
			_writer.WriteLine(notificationJson);
			_writer.Flush();
		}
	}

	private static Dictionary<string, JsonElement> ParseJsonDocument(JsonDocument doc)
	{
		Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>();
		foreach (JsonProperty property in doc.RootElement.EnumerateObject())
		{
			result[property.Name] = property.Value;
		}
		return result;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			if (!_process.HasExited)
			{
				_process.Kill(entireProcessTree: true);
			}
		}
		catch
		{
		}
		_writer.Dispose();
		_reader.Dispose();
		_process.Dispose();
	}
}
