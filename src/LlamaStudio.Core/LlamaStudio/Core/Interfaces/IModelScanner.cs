using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IModelScanner
{
	Task<List<GgufModelInfo>> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default(CancellationToken));

	Task<GgufModelInfo> AnalyzeModelAsync(string modelPath, CancellationToken cancellationToken = default(CancellationToken));

	string FormatSize(long bytes);

	double EstimateVramUsage(GgufModelInfo model, int gpuLayers);
}
