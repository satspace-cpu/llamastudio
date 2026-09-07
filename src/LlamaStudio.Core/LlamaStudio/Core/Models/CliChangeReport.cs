using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class CliChangeReport
{
	public string OldVersion { get; set; } = string.Empty;

	public string NewVersion { get; set; } = string.Empty;

	public List<CliFlagInfo> AddedFlags { get; set; } = new List<CliFlagInfo>();

	public List<string> RemovedFlags { get; set; } = new List<string>();

	public List<ChangedFlag> ModifiedFlags { get; set; } = new List<ChangedFlag>();

	public List<CriticalChange> CriticalChanges { get; set; } = new List<CriticalChange>();

	public bool HasCriticalChanges => CriticalChanges.Count > 0;
}
