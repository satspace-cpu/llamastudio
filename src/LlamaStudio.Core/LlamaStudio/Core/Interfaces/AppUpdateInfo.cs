using System;

namespace LlamaStudio.Core.Interfaces;

public record AppUpdateInfo(string Version, string DownloadUrl, string Changelog, DateTime PublishedAt, bool IsPrerelease, string Sha256 = "", string DownloadUrlLinux = "")
{
	/// <summary>
	/// r87: the URL for the current OS — the Linux tar.gz when present,
	/// otherwise the (Windows) EXE URL.
	/// </summary>
	public string DownloadUrlForCurrentOS => OperatingSystem.IsLinux() && !string.IsNullOrEmpty(DownloadUrlLinux) ? DownloadUrlLinux : DownloadUrl;
}
