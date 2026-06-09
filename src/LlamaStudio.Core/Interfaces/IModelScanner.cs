using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IModelScanner
{
    Task<List<GgufModelInfo>> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default);
    Task<GgufModelInfo> AnalyzeModelAsync(string modelPath, CancellationToken cancellationToken = default);
    string FormatSize(long bytes);
    double EstimateVramUsage(GgufModelInfo model, int gpuLayers);
}
