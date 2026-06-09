namespace LlamaStudio.Core.Models;

public enum BuildType
{
    Unknown,
    Cpu,
    Cuda12x,
    Cuda13x,
    Vulkan,
    OpenVino
}

public class LlamaCppRelease
{
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public bool IsPrerelease { get; set; }
    public string Body { get; set; } = string.Empty;
    public List<ReleaseAsset> Assets { get; set; } = new();
}

public class ReleaseAsset
{
    public string Name { get; set; } = string.Empty;
    public string BrowserDownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UpdatedAt { get; set; }
    public BuildType BuildType { get; set; }
    public string CudaVersion { get; set; } = string.Empty;
    public string Description => BuildType switch
    {
        BuildType.Cpu => "CPU only",
        BuildType.Cuda12x => $"CUDA {CudaVersion}",
        BuildType.Cuda13x => $"CUDA 13 ({CudaVersion})",
        BuildType.Vulkan => "Vulkan GPU",
        BuildType.OpenVino => "OpenVINO",
        _ => Name
    };
    public string SizeDisplay => Size switch
    {
        >= 1024L * 1024L * 1024L => $"{Size / (1024.0 * 1024.0 * 1024.0):F1} GB",
        >= 1024L * 1024L => $"{Size / (1024.0 * 1024.0):F1} MB",
        >= 1024L => $"{Size / 1024.0:F1} KB",
        _ => $"{Size} B"
    };

    public static ReleaseAsset ParseAsset(string name, string url, long size, DateTime updated)
    {
        var lower = name.ToLowerInvariant();
        var asset = new ReleaseAsset
        {
            Name = name,
            BrowserDownloadUrl = url,
            Size = size,
            UpdatedAt = updated
        };

        if (lower.Contains("cuda-13") || lower.Contains("cuda13"))
        {
            asset.BuildType = BuildType.Cuda13x;
            asset.CudaVersion = ExtractCudaVersion(lower, "13");
        }
        else if (lower.Contains("cuda-12") || lower.Contains("cuda12"))
        {
            asset.BuildType = BuildType.Cuda12x;
            asset.CudaVersion = ExtractCudaVersion(lower, "12");
        }
        else if (lower.Contains("vulkan"))
        {
            asset.BuildType = BuildType.Vulkan;
        }
        else if (lower.Contains("openvino") || lower.Contains("open-vino"))
        {
            asset.BuildType = BuildType.OpenVino;
        }
        else if (!lower.Contains("cuda") && !lower.Contains("vulkan") && !lower.Contains("openvino"))
        {
            asset.BuildType = BuildType.Cpu;
        }

        return asset;
    }

    static string ExtractCudaVersion(string name, string major)
    {
        var idx = name.IndexOf(major);
        if (idx >= 0 && idx + major.Length < name.Length)
        {
            var rest = name.Substring(idx + major.Length);
            var dotIdx = rest.IndexOf('.');
            if (dotIdx > 0 && dotIdx + 1 < rest.Length && char.IsDigit(rest[dotIdx + 1]))
                return $"{major}.{rest[dotIdx + 1]}";
        }
        return major;
    }
}
