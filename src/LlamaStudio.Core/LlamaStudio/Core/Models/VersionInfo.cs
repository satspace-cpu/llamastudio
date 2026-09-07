using System;
using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class VersionInfo
{
	public string Version { get; set; } = string.Empty;

	public string BuildType { get; set; } = string.Empty;

	public string Commit { get; set; } = string.Empty;

	public string Directory { get; set; } = string.Empty;

	/// <summary>Full first line of `llama-server --version`, e.g.
	/// "version: 0.3.0-dev (build 1, commit 0f3a71b)".</summary>
	public string VersionBanner { get; set; } = string.Empty;

	/// <summary>Build number reported by the binary (local builds report 1).</summary>
	public string BuildNumber { get; set; } = string.Empty;

	/// <summary>Commit hash reported by the binary itself.</summary>
	public string CommitHash { get; set; } = string.Empty;

	/// <summary>Short one-line summary for UI: commit · build · version banner.</summary>
	public string VersionDetail
	{
		get
		{
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(CommitHash))
				parts.Add("commit " + CommitHash);
			if (!string.IsNullOrEmpty(BuildNumber))
				parts.Add("build " + BuildNumber);
			if (!string.IsNullOrEmpty(VersionBanner))
				parts.Add(VersionBanner);
			return string.Join(" · ", parts);
		}
	}

	public string VersionKey
	{
		get
		{
			if (!string.IsNullOrEmpty(BuildType))
			{
				return Version + "|" + BuildType;
			}
			return Version;
		}
	}

	public string Display
	{
		get
		{
			if (!string.IsNullOrEmpty(BuildType))
			{
				return Version + " (" + BuildType + ")";
			}
			return Version;
		}
	}
}
