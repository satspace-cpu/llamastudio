using System.Threading.Tasks;
using System.Collections.Generic;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IGpuMonitor
{
	bool IsAvailable { get; }

	string LastRawOutput { get; }

	Task<GpuInfo?> GetGpuInfoAsync();

	Task<IReadOnlyList<GpuInfo>> GetGpuInfosAsync();
}
