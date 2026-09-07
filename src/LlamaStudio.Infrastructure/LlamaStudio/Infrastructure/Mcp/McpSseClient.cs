using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Infrastructure.Mcp;

public class McpSseClient : IMcpClient, IDisposable
{
	private readonly HttpClient _httpClient;

	private readonly string _baseUrl;

	private readonly object _lock = new object();

	private int _requestId;

	private bool _disposed;

	private bool _initialized;

	private CancellationTokenSource? _sseCts;

	private Task? _sseReaderTask;

	private string? _messageEndpoint;

	private readonly Dictionary<int, TaskCompletionSource<JsonDocument>> _pendingRequests = new Dictionary<int, TaskCompletionSource<JsonDocument>>();

	private readonly List<JsonDocument> _receivedNotifications = new List<JsonDocument>();

	private readonly List<(string Name, string Description, Dictionary<string, object> InputSchema)> _cachedTools = new List<(string, string, Dictionary<string, object>)>();

	public string ServerName { get; }

	public bool IsConnected
	{
		get
		{
			if (!_disposed)
			{
				return _messageEndpoint != null;
			}
			return false;
		}
	}

	public McpSseClient(string name, string url)
	{
		ServerName = name;
		_baseUrl = url.TrimEnd('/');
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(30.0)
		};
	}

	public async Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_cachedTools.Count > 0)
		{
			return _cachedTools;
		}
		await EnsureInitializedAsync(ct);
		try
		{
			JsonDocument jsonDocument = await SendRequestAsync("tools/list", new Dictionary<string, object>(), ct);
			if (jsonDocument != null && jsonDocument.RootElement.TryGetProperty("result", out var resultElement) && resultElement.TryGetProperty("tools", out var toolsElement))
			{
				_cachedTools.Clear();
				foreach (JsonElement toolElement in toolsElement.EnumerateArray())
				{
					JsonElement nameElement;
					string toolName = (toolElement.TryGetProperty("name", out nameElement) ? (nameElement.GetString() ?? "") : "");
					JsonElement descriptionElement;
					string toolDescription = (toolElement.TryGetProperty("description", out descriptionElement) ? (descriptionElement.GetString() ?? "") : "");
					Dictionary<string, object> inputSchema = new Dictionary<string, object>();
					if (toolElement.TryGetProperty("inputSchema", out var inputSchemaElement))
					{
						inputSchema = JsonSerializer.Deserialize<Dictionary<string, object>>(inputSchemaElement.GetRawText()) ?? new Dictionary<string, object>();
					}
					_cachedTools.Add((toolName, toolDescription, inputSchema));
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("MCP server '" + ServerName + "' tools/list failed: " + ex.Message, ex);
		}
		return _cachedTools;
	}

	public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default(CancellationToken))
	{
		await EnsureInitializedAsync(ct);
		JsonDocument jsonDocument = await SendRequestAsync("tools/call", new Dictionary<string, object>
		{
			["name"] = toolName,
			["arguments"] = arguments
		}, ct);
		if (jsonDocument == null)
		{
			return "Error: No response from MCP server";
		}
		if (jsonDocument.RootElement.TryGetProperty("result", out var resultElement) && resultElement.TryGetProperty("content", out var contentElement))
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (JsonElement contentItem in contentElement.EnumerateArray())
			{
				JsonElement typeElement;
				string contentType = (contentItem.TryGetProperty("type", out typeElement) ? (typeElement.GetString() ?? "") : "");
				JsonElement dataElement;
				if (contentType == "text" && contentItem.TryGetProperty("text", out var textContent))
				{
					stringBuilder.AppendLine(textContent.GetString());
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
		if (jsonDocument.RootElement.TryGetProperty("error", out var errorElement))
		{
			return $"Error: {errorElement}";
		}
		return "Error: Unexpected response for tool '" + toolName + "'";
	}

	private async Task EnsureInitializedAsync(CancellationToken ct)
	{
		if (!_initialized)
		{
			await StartSseConnectionAsync(ct);
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
			SendNotificationAsync("notifications/initialized", new Dictionary<string, object>());
			_initialized = true;
		}
	}

	private async Task StartSseConnectionAsync(CancellationToken ct)
	{
		_sseCts = new CancellationTokenSource();
		string requestUri = _baseUrl + "/sse";
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
		{
			Version = HttpVersion.Version11
		};
		httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
		HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, _sseCts.Token);
		response.EnsureSuccessStatusCode();
		StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync(_sseCts.Token), Encoding.UTF8);
		_sseReaderTask = Task.Run(async delegate
		{
			try
			{
				StringBuilder buffer = new StringBuilder();
				while (!_sseCts.Token.IsCancellationRequested)
				{
					string sseLine = await reader.ReadLineAsync(_sseCts.Token);
					if (sseLine == null)
					{
						break;
					}
					if (sseLine.Length == 0)
					{
						ProcessSseEvent(buffer.ToString());
						buffer.Clear();
					}
					else if (!sseLine.StartsWith(":"))
					{
						if (sseLine.StartsWith("event:"))
						{
							buffer.Clear();
						}
						else if (sseLine.StartsWith("data:"))
						{
							buffer.Append(sseLine.Substring(5).TrimStart());
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception)
			{
			}
		}, _sseCts.Token);
	}

	private void ProcessSseEvent(string eventData)
	{
		if (string.IsNullOrWhiteSpace(eventData))
		{
			return;
		}
		try
		{
			JsonDocument jsonDocument = JsonDocument.Parse(eventData);
			lock (_lock)
			{
				if (jsonDocument.RootElement.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
				{
					int requestId = idElement.GetInt32();
					if (_pendingRequests.TryGetValue(requestId, out TaskCompletionSource<JsonDocument> tcs))
					{
						_pendingRequests.Remove(requestId);
						tcs.TrySetResult(jsonDocument);
						return;
					}
				}
				JsonElement element = jsonDocument.RootElement;
				if (element.TryGetProperty("result", out var resultElement) && (resultElement.TryGetProperty("capabilities", out element) || resultElement.TryGetProperty("protocolVersion", out element)) && _messageEndpoint == null)
				{
					_messageEndpoint = _baseUrl + "/message";
				}
				_receivedNotifications.Add(jsonDocument);
			}
		}
		catch
		{
		}
	}

	private async Task<JsonDocument?> SendRequestAsync(string method, Dictionary<string, object> @params, CancellationToken ct)
	{
		if (_messageEndpoint == null)
		{
			_messageEndpoint = _baseUrl + "/message";
		}
		int id = Interlocked.Increment(ref _requestId);
		StringContent content = new StringContent(JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = id,
			method = method,
			@params = @params
		}), Encoding.UTF8, "application/json");
		TaskCompletionSource<JsonDocument> tcs = new TaskCompletionSource<JsonDocument>(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_lock)
		{
			_pendingRequests[id] = tcs;
		}
		try
		{
			HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync(_messageEndpoint, content, ct);
			if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
			{
				string responseBody = await httpResponseMessage.Content.ReadAsStringAsync(ct);
				if (!string.IsNullOrWhiteSpace(responseBody))
				{
					try
					{
						JsonDocument result = JsonDocument.Parse(responseBody);
						lock (_lock)
						{
							_pendingRequests.Remove(id);
						}
						return result;
					}
					catch
					{
					}
				}
			}
			using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(TimeSpan.FromSeconds(30.0));
			return await tcs.Task.WaitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException)
		{
			lock (_lock)
			{
				_pendingRequests.Remove(id);
			}
			throw new TimeoutException("MCP SSE request timeout: " + method);
		}
		finally
		{
			lock (_lock)
			{
				_pendingRequests.Remove(id);
			}
		}
	}

	private async Task SendNotificationAsync(string method, Dictionary<string, object> @params)
	{
		if (_messageEndpoint == null)
		{
			return;
		}
		StringContent content = new StringContent(JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = method,
			@params = @params
		}), Encoding.UTF8, "application/json");
		try
		{
			await _httpClient.PostAsync(_messageEndpoint, content);
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_sseCts?.Cancel();
		_sseCts?.Dispose();
		lock (_lock)
		{
			foreach (TaskCompletionSource<JsonDocument> pendingRequest in _pendingRequests.Values)
			{
				pendingRequest.TrySetException(new ObjectDisposedException("McpSseClient"));
			}
			_pendingRequests.Clear();
		}
		_httpClient.Dispose();
	}
}
