using System;

namespace LlamaStudio.Core.Models;

public class ReleaseAsset
{
	public string Name { get; set; } = string.Empty;

	public string BrowserDownloadUrl { get; set; } = string.Empty;

	public long Size { get; set; }

	public DateTime UpdatedAt { get; set; }

	public BuildType BuildType { get; set; }

	public string CudaVersion { get; set; } = string.Empty;

	public string DescriptionOverride { get; set; } = string.Empty;

	public string SizeDisplayOverride { get; set; } = string.Empty;

	public string Description => !string.IsNullOrWhiteSpace(DescriptionOverride) ? DescriptionOverride : BuildType switch
	{
		BuildType.Cpu => "CPU only", 
		BuildType.Cuda12x => "CUDA " + CudaVersion, 
		BuildType.Cuda13x => "CUDA 13 (" + CudaVersion + ")", 
		BuildType.Vulkan => "Vulkan GPU", 
		BuildType.OpenVino => "OpenVINO", 
		_ => Name, 
	};

	public string SizeDisplay
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(SizeDisplayOverride))
			{
				return SizeDisplayOverride;
			}
			long size = Size;
			if (size >= 1048576)
			{
				if (size >= 1073741824)
				{
					return $"{(double)Size / 1073741824.0:F1} GB";
				}
				return $"{(double)Size / 1048576.0:F1} MB";
			}
			if (size >= 1024)
			{
				return $"{(double)Size / 1024.0:F1} KB";
			}
			return $"{Size} B";
		}
	}

	public static ReleaseAsset ParseAsset(string name, string url, long size, DateTime updated)
	{
		string text = name.ToLowerInvariant();
		ReleaseAsset releaseAsset = new ReleaseAsset
		{
			Name = name,
			BrowserDownloadUrl = url,
			Size = size,
			UpdatedAt = updated
		};
		if (text.Contains("cuda-13") || text.Contains("cuda13"))
		{
			releaseAsset.BuildType = BuildType.Cuda13x;
			releaseAsset.CudaVersion = ExtractCudaVersion(text, "13");
		}
		else if (text.Contains("cuda-12") || text.Contains("cuda12"))
		{
			releaseAsset.BuildType = BuildType.Cuda12x;
			releaseAsset.CudaVersion = ExtractCudaVersion(text, "12");
		}
		else if (text.Contains("vulkan"))
		{
			releaseAsset.BuildType = BuildType.Vulkan;
		}
		else if (text.Contains("openvino") || text.Contains("open-vino"))
		{
			releaseAsset.BuildType = BuildType.OpenVino;
		}
		else if (!text.Contains("cuda") && !text.Contains("vulkan") && !text.Contains("openvino"))
		{
			releaseAsset.BuildType = BuildType.Cpu;
		}
		return releaseAsset;
	}

	private static string ExtractCudaVersion(string name, string major)
	{
		int num = name.IndexOf(major);
		if (num >= 0 && num + major.Length < name.Length)
		{
			string text = name.Substring(num + major.Length);
			int dotIndex = text.IndexOf('.');
			if (dotIndex > 0 && dotIndex + 1 < text.Length && char.IsDigit(text[dotIndex + 1]))
			{
				return $"{major}.{text[dotIndex + 1]}";
			}
		}
		return major;
	}
}
