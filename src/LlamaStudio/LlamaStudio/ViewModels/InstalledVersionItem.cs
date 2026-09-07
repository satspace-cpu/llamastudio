using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class InstalledVersionItem
{
	public string Version { get; }

	public string FolderName { get; }

	public bool IsActive { get; set; }

	public BuildType BuildType { get; }

	public string Description { get; }

	/// <summary>Full path of the folder containing llama-server.</summary>
	public string Directory { get; }

	/// <summary>"commit … · build … · version: …" from the binary's --version banner.</summary>
	public string VersionDetail { get; }

	public InstalledVersionItem(string version, string folderName, bool isActive, BuildType buildType = BuildType.Unknown, string description = "", string directory = "", string versionDetail = "")
	{
		Version = version;
		FolderName = folderName;
		IsActive = isActive;
		BuildType = buildType;
		Description = description;
		Directory = directory;
		VersionDetail = versionDetail;
	}
}
