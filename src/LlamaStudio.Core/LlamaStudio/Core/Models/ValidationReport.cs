using System;
using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class ValidationReport
{
	public DateTime Timestamp { get; set; }

	public string ServerExePath { get; set; } = string.Empty;

	public string Version { get; set; } = string.Empty;

	public List<RemovedFlag> RemovedFlags { get; set; } = new List<RemovedFlag>();

	public List<CliFlagInfo> NewAvailableFlags { get; set; } = new List<CliFlagInfo>();

	public List<ChangedFlag> ChangedFlags { get; set; } = new List<ChangedFlag>();

	public ValidationStatus Status { get; set; }

	public bool HasIssues
	{
		get
		{
			if (RemovedFlags.Count <= 0)
			{
				return ChangedFlags.Count > 0;
			}
			return true;
		}
	}
}
