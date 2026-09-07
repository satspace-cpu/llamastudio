using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface ICliValidator
{
	Task<ValidationReport> ValidateAllFlagsAsync(string serverExePath, string version);

	Task<CliChangeReport> CompareVersionsAsync(LlamaHelpInfo oldHelp, LlamaHelpInfo newHelp, string oldVersion, string newVersion);

	Task SaveSnapshotAsync(LlamaHelpInfo helpInfo, string version);

	Task<LlamaHelpInfo?> LoadSnapshotAsync(string version);

	Task<LlamaHelpInfo?> LoadLatestSnapshotAsync();

	string GetSnapshotsDirectory();
}
