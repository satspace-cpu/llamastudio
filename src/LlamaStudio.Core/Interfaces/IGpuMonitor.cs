using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IGpuMonitor
{
    Task<GpuInfo?> GetGpuInfoAsync();
    bool IsAvailable { get; }
    string LastRawOutput { get; }
}
