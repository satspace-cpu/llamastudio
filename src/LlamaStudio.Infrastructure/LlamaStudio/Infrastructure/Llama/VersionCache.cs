using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class VersionCache : IVersionCache
{
	private static string ServerExecutableName => OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server";
	public event EventHandler? Changed;

	private readonly IVersionDetector _detector;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly object _lock = new object();

	private List<VersionInfo> _versions = new List<VersionInfo>();

	public VersionCache(IVersionDetector detector, ISettings settings, ILogService log)
	{
		_detector = detector;
		_settings = settings;
		_log = log;
	}

	public async Task LoadAsync()
	{
		lock (_lock)
		{
			_versions = _settings.InstalledVersionsCache.ToList();
			List<VersionInfo> list = new List<VersionInfo>();
			foreach (VersionInfo version in _versions)
			{
				if (Directory.Exists(version.Directory) && File.Exists(Path.Combine(version.Directory, ServerExecutableName)))
				{
					list.Add(version);
				}
				else
				{
					_log.Information("Removed stale version cache entry: " + version.Directory, "VersionCache");
				}
			}
			_versions = list;
		}
	}

	public async Task ScanAsync(string baseDir, CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(baseDir))
		{
			_log.Warning("Base directory does not exist: " + baseDir, "VersionCache");
			return;
		}
		List<VersionInfo> discovered = new List<VersionInfo>();
		foreach (string subDir in Directory.EnumerateDirectories(baseDir))
		{
			if (ct.IsCancellationRequested)
			{
				break;
			}
			if (!File.Exists(Path.Combine(subDir, ServerExecutableName)))
			{
				continue;
			}
			try
			{
				VersionInfo versionInfo = await _detector.DetectAsync(subDir, ct);
				if (versionInfo != null)
				{
					discovered.Add(versionInfo);
					_log.Information("Detected: " + versionInfo.Display + " in " + versionInfo.Directory, "VersionCache");
				}
				else
				{
					_log.Warning("DetectAsync returned null for " + subDir, "VersionCache");
				}
			}
			catch (Exception ex)
			{
				_log.Warning("Failed to detect version in " + subDir + ": " + ex.Message, "VersionCache");
			}
		}
		lock (_lock)
		{
			foreach (VersionInfo newV in discovered)
			{
				_versions.RemoveAll((VersionInfo v) => v.Directory == newV.Directory);
				_versions.Add(newV);
			}
			foreach (VersionInfo item in _versions.Where((VersionInfo v) => !Directory.Exists(v.Directory) || !File.Exists(Path.Combine(v.Directory, ServerExecutableName))).ToList())
			{
				_log.Information("Removed stale version: " + item.Directory, "VersionCache");
				_versions.Remove(item);
			}
		}
		await SaveAsync();
		_log.Information($"Scan complete: {_versions.Count} versions in {baseDir} (discovered: {discovered.Count})", "VersionCache");
		Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Add(VersionInfo info)
	{
		lock (_lock)
		{
			_versions.RemoveAll((VersionInfo v) => v.Directory == info.Directory);
			_versions.Add(info);
		}
	}

	public IReadOnlyList<VersionInfo> GetAll()
	{
		lock (_lock)
		{
			return _versions.OrderByDescending((VersionInfo v) => v.Version).ToList().AsReadOnly();
		}
	}

	public VersionInfo? GetByPath(string path)
	{
		lock (_lock)
		{
			return _versions.FirstOrDefault((VersionInfo v) => v.Directory.Equals(path, StringComparison.OrdinalIgnoreCase));
		}
	}

	public VersionInfo? GetByVersionKey(string versionKey)
	{
		lock (_lock)
		{
			return _versions.FirstOrDefault((VersionInfo v) => v.VersionKey == versionKey);
		}
	}

	public VersionInfo? GetByVersion(string version)
	{
		lock (_lock)
		{
			return _versions.FirstOrDefault((VersionInfo v) => v.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
		}
	}

	public string? GetFolderByVersionKey(string versionKey)
	{
		VersionInfo byVersionKey = GetByVersionKey(versionKey);
		if (byVersionKey == null)
		{
			return null;
		}
		return Path.GetFileName(byVersionKey.Directory);
	}

	public string? GetFolderByVersion(string version)
	{
		VersionInfo byVersion = GetByVersion(version);
		if (byVersion == null)
		{
			return null;
		}
		return Path.GetFileName(byVersion.Directory);
	}

	public bool IsVersionInstalled(string versionTag)
	{
		lock (_lock)
		{
			return _versions.Any((VersionInfo v) => v.Version.Equals(versionTag, StringComparison.OrdinalIgnoreCase));
		}
	}

	public bool IsVersionInstalled(string versionTag, string buildType)
	{
		lock (_lock)
		{
			return _versions.Any((VersionInfo v) => v.Version.Equals(versionTag, StringComparison.OrdinalIgnoreCase) && v.BuildType.Equals(buildType, StringComparison.OrdinalIgnoreCase));
		}
	}

	public async Task SaveAsync()
	{
		lock (_lock)
		{
			_settings.InstalledVersionsCache = new List<VersionInfo>(_versions);
		}
		await _settings.SaveAsync();
	}

	public void Clear()
	{
		lock (_lock)
		{
			_versions.Clear();
		}
	}
}
