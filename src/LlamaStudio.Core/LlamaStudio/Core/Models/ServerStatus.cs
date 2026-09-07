using System;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

public class ServerStatus
{
	public ServerState State { get; set; }

	public int Port { get; set; }

	public string Host { get; set; } = "127.0.0.1";

	public string? ModelName { get; set; }

	public int ContextSize { get; set; }

	public int Threads { get; set; }

	public int GpuLayers { get; set; }

	public double VramUsedGb { get; set; }

	public double RamUsedGb { get; set; }

	// Private/committed bytes of llama-server. This differs from RamUsedGb when
	// llama.cpp maps the model file: mapped pages are in the working set.
	public double RamCommittedGb { get; set; }

	public double CacheRamLimitGb { get; set; }

	public double TokensPerSecond { get; set; }

	public double PromptTokensPerSecond { get; set; }

	public int QueueSize { get; set; }

	public int ActiveSlots { get; set; }

	public int TotalTokensProcessed { get; set; }

	public TimeSpan Uptime { get; set; }

	public DateTime? StartedAt { get; set; }

	public string? ErrorMessage { get; set; }

	public int? ProcessId { get; set; }
}
