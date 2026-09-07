using System;

namespace LlamaStudio.Core.Models;

public class GpuInfo
{
	public int Index { get; set; } = -1;

	public bool IsRemote { get; set; }

	public string RemoteDeviceId { get; set; } = string.Empty;

	public string CudaId => IsRemote ? RemoteDeviceId : (Index >= 0 ? "CUDA" + Index : string.Empty);

	public string Name { get; set; } = string.Empty;

	public double ComputeCapability { get; set; }

	public double PerformanceScore { get; set; } = 1.0;

	public bool IsSelected { get; set; }

	public string DisplayName => string.IsNullOrWhiteSpace(CudaId) ? Name : $"{Name} ({CudaId})";

	public double TemperatureCelsius { get; set; }

	public double MemoryTemperatureCelsius { get; set; }

	public double GpuUtilization { get; set; }

	public double MemoryUsedGb { get; set; }

	public double MemoryTotalGb { get; set; }

	public double PowerDrawWatts { get; set; }

	public double PowerLimitWatts { get; set; }

	public int FanSpeed { get; set; }

	public int ClockMhz { get; set; }

	public string DriverVersion { get; set; } = string.Empty;

	public double MemoryPercent
	{
		get
		{
			if (!(MemoryTotalGb > 0.0))
			{
				return 0.0;
			}
			return Math.Min(MemoryUsedGb / MemoryTotalGb * 100.0, 100.0);
		}
	}

	public double PowerPercent
	{
		get
		{
			if (!(PowerLimitWatts > 0.0))
			{
				return 0.0;
			}
			return Math.Min(PowerDrawWatts / PowerLimitWatts * 100.0, 100.0);
		}
	}
}
