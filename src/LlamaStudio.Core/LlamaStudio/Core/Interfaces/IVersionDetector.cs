using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IVersionDetector
{
	Task<VersionInfo?> DetectAsync(string directory, CancellationToken ct = default(CancellationToken));
}
