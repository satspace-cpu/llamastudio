namespace LlamaStudio.Core.Models;

public class GpuInfo
{
    public string Name { get; set; } = string.Empty;
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
    public double MemoryPercent => MemoryTotalGb > 0 ? Math.Min(MemoryUsedGb / MemoryTotalGb * 100, 100) : 0;
    public double PowerPercent => PowerLimitWatts > 0 ? Math.Min(PowerDrawWatts / PowerLimitWatts * 100, 100) : 0;
}
