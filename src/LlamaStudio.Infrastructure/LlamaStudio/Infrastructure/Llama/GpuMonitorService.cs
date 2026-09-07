using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class GpuMonitorService : IGpuMonitor
{
	private bool? _isAvailable;
	private IReadOnlyList<GpuInfo> _lastSuccessfulInfos = Array.Empty<GpuInfo>();

	public string LastRawOutput { get; private set; } = "";

	public bool IsAvailable
	{
		get
		{
			if (_isAvailable.HasValue)
			{
				return _isAvailable.Value;
			}
			try
			{
				using Process process = Process.Start(new ProcessStartInfo("nvidia-smi", "--version")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				});
				if (process != null)
				{
					process.WaitForExit(3000);
					_isAvailable = process.ExitCode == 0;
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
		return (await GetGpuInfosAsync()).FirstOrDefault();
	}

	public async Task<IReadOnlyList<GpuInfo>> GetGpuInfosAsync()
	{
		if (!IsAvailable)
		{
			return Array.Empty<GpuInfo>();
		}
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("nvidia-smi", "--query-gpu=index,name,compute_cap,temperature.gpu,temperature.memory,utilization.gpu,memory.used,memory.total,power.draw,power.limit,fan.speed,clocks.current.graphics,driver_version --format=csv,noheader")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("nvidia-smi not found");
			string output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();
			if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
			{
				return _lastSuccessfulInfos;
			}
			string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			LastRawOutput = string.Join(Environment.NewLine, lines);
			List<GpuInfo> result = new List<GpuInfo>();
			foreach (string line in lines)
			{
				string[] array = SplitCsvLine(line);
				if (array.Length < 13)
				{
					continue;
				}
				GpuInfo gpuInfo = new GpuInfo
				{
					Index = (int)ExtractNumber(array[0]),
					Name = array[1].Trim().Trim('"'),
					ComputeCapability = ExtractNumber(array[2]),
					TemperatureCelsius = ExtractNumber(array[3]),
					MemoryTemperatureCelsius = ExtractNumber(array[4]),
					GpuUtilization = ExtractNumber(array[5]),
					MemoryUsedGb = ExtractNumber(array[6]) / 1024.0,
					MemoryTotalGb = ExtractNumber(array[7]) / 1024.0,
					PowerDrawWatts = ExtractNumber(array[8]),
					PowerLimitWatts = ExtractNumber(array[9]),
					FanSpeed = (int)ExtractNumber(array[10]),
					ClockMhz = (int)ExtractNumber(array[11]),
					DriverVersion = array[12].Trim().Trim('"')
				};
				gpuInfo.PerformanceScore = EstimatePerformanceScore(gpuInfo);
				result.Add(gpuInfo);
			}
			if (result.Count == 0)
			{
				return _lastSuccessfulInfos;
			}

			// Keep a complete device list during short nvidia-smi glitches. A GPU
			// selected out of the model command must remain visible in monitoring.
			if (_lastSuccessfulInfos.Count > result.Count)
			{
				Dictionary<int, GpuInfo> current = result.ToDictionary(gpu => gpu.Index);
				foreach (GpuInfo previous in _lastSuccessfulInfos)
				{
					if (!current.ContainsKey(previous.Index))
					{
						current[previous.Index] = previous;
					}
				}
				result = current.Values.OrderBy(gpu => gpu.Index).ToList();
			}
			_lastSuccessfulInfos = result;
			return result;
		}
		catch
		{
			return Array.Empty<GpuInfo>();
		}
	}

	private static double EstimatePerformanceScore(GpuInfo gpu)
	{
		if (gpu.ComputeCapability > 0.0)
		{
			return Math.Max(1.0, gpu.ComputeCapability / 5.0);
		}
		return Math.Max(1.0, gpu.MemoryTotalGb / 8.0);
	}

	private static string[] SplitCsvLine(string line)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (char c in line)
		{
			switch (c)
			{
			case '"':
				flag = !flag;
				continue;
			case ',':
				if (!flag)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
					continue;
				}
				break;
			}
			stringBuilder.Append(c);
		}
		list.Add(stringBuilder.ToString());
		return list.ToArray();
	}

	private static double ExtractNumber(string s)
	{
		Match match = Regex.Match(s, "[\\d]+\\.?[\\d]*");
		if (!match.Success)
		{
			return 0.0;
		}
		if (!double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return 0.0;
		}
		return result;
	}
}
