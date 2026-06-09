namespace LlamaStudio.Core.Models;

public class ServerStatus
{
    public Core.Enums.ServerState State { get; set; }
    public int Port { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public string? ModelName { get; set; }
    public int ContextSize { get; set; }
    public int Threads { get; set; }
    public int GpuLayers { get; set; }
    public double VramUsedGb { get; set; }
    public double RamUsedGb { get; set; }
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
