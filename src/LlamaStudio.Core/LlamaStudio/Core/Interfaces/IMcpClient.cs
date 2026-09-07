using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaStudio.Core.Interfaces;

public interface IMcpClient : IDisposable
{
	string ServerName { get; }

	bool IsConnected { get; }

	Task<List<(string Name, string Description, Dictionary<string, object> InputSchema)>> GetToolsAsync(CancellationToken ct = default(CancellationToken));

	Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default(CancellationToken));
}
