using System;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class ReleaseBuildCard
{
	public string ReleaseTag { get; set; } = string.Empty;

	public DateTime ReleaseDate { get; set; }

	public bool IsPrerelease { get; set; }

	public ReleaseAsset Asset { get; set; }

	public BuildType BuildType { get; set; }

	public string BuildDescription { get; set; } = string.Empty;

	public string SizeDisplay { get; set; } = string.Empty;

	public string AssetName { get; set; } = string.Empty;

	public string InstallBtnText { get; set; } = "Install";

	public bool IsInstalled { get; set; }
}
