using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LlamaStudio.Infrastructure.Llama;

public class GpuMonitorService : IGpuMonitor
{
    bool? _isAvailable;
    public string LastRawOutput { get; private set; } = "";

    public bool IsAvailable
    {
        get
        {
            if (_isAvailable.HasValue) return _isAvailable.Value;
            try
            {
                using var p = Process.Start(new ProcessStartInfo("nvidia-smi", "--version")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                if (p != null)
                {
                    p.WaitForExit(3000);
                    _isAvailable = p.ExitCode == 0;
                }
                else
                {
                    _isAvailable = false;
                }
            }
            catch
            {
                _isAvailable = false;
            }
            return _isAvailable.Value;
        }
    }

    public async Task<GpuInfo?> GetGpuInfoAsync()
    {
        if (!IsAvailable) return null;

        try
        {
            var startInfo = new ProcessStartInfo("nvidia-smi",
                "--query-gpu=name,temperature.gpu,temperature.memory,utilization.gpu,memory.used,memory.total,power.draw,power.limit,fan.speed,clocks.current.graphics,driver_version" +
                " --format=csv,noheader")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("nvidia-smi not found");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return null;

            var line = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (line == null) return null;

            LastRawOutput = line;

            // Split by comma — handle GPU name with commas via CSV-aware split
            var parts = SplitCsvLine(line);
            
            if (parts.Length < 11) return null;

            var info = new GpuInfo
            {
                Name = parts[0].Trim().Trim('"'),
                TemperatureCelsius = ExtractNumber(parts[1]),
                MemoryTemperatureCelsius = ExtractNumber(parts[2]),
                GpuUtilization = ExtractNumber(parts[3]),
                MemoryUsedGb = ExtractNumber(parts[4]) / 1024.0,
                MemoryTotalGb = ExtractNumber(parts[5]) / 1024.0,
                PowerDrawWatts = ExtractNumber(parts[6]),
                PowerLimitWatts = ExtractNumber(parts[7]),
                FanSpeed = (int)ExtractNumber(parts[8]),
                ClockMhz = (int)ExtractNumber(parts[9]),
                DriverVersion = parts[10].Trim().Trim('"')
            };

            return info;
        }
        catch
        {
            return null;
        }
    }

    // Split CSV line handling quoted fields (GPU name may contain commas)
    static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result.ToArray();
    }

    static double ExtractNumber(string s)
    {
        var match = Regex.Match(s, @"[\d]+\.?[\d]*");
        if (!match.Success) return 0;
        return double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
