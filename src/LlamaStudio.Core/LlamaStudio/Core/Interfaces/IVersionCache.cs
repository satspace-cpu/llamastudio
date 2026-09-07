using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IVersionCache
{
	event EventHandler? Changed;

	Task LoadAsync();

	Task ScanAsync(string baseDir, CancellationToken ct = default(CancellationToken));

	void Add(VersionInfo info);

	IReadOnlyList<VersionInfo> GetAll();

	VersionInfo? GetByPath(string path);

	VersionInfo? GetByVersionKey(string versionKey);

	VersionInfo? GetByVersion(string version);

	string? GetFolderByVersionKey(string versionKey);

	string? GetFolderByVersion(string version);

	bool IsVersionInstalled(string versionTag);

	bool IsVersionInstalled(string versionTag, string buildType);

	Task SaveAsync();

	void Clear();
}
