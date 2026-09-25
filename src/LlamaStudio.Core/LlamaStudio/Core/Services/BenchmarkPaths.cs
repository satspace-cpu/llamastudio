using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace LlamaStudio.Core.Services;

/// <summary>
/// r101: cross-platform executable name/path helpers for the benchmark runner.
/// Pure static — unit-testable (the runner uses OperatingSystem.IsWindows()).
/// </summary>
public static class BenchmarkPaths
{
	/// <summary>
	/// Candidate executable names for a tool ("llama-bench"), in try order.
	/// Windows adds .exe (and .cmd/.bat for completeness); Unix uses the bare name.
	/// </summary>
	public static IEnumerable<string> ExecutableNames(string toolName, bool isWindows)
	{
		yield return toolName;
		if (isWindows && string.IsNullOrEmpty(Path.GetExtension(toolName)))
		{
			yield return toolName + ".exe";
			yield return toolName + ".cmd";
			yield return toolName + ".bat";
		}
	}

	/// <summary>Resolves the first existing executable in a directory (null if none).</summary>
	public static string? ResolveInDirectory(string directory, string toolName, bool isWindows)
	{
		if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
		{
			return null;
		}
		foreach (string name in ExecutableNames(toolName, isWindows))
		{
			string path = Path.Combine(directory, name);
			if (File.Exists(path))
			{
				return Path.GetFullPath(path);
			}
		}
		return null;
	}

	/// <summary>llama-bench path from the directory of the running llama-server executable.</summary>
	public static string? ResolveBenchPath(string? serverExecutablePath, bool isWindows)
	{
		if (string.IsNullOrWhiteSpace(serverExecutablePath))
		{
			return null;
		}
		string? dir = Path.GetDirectoryName(serverExecutablePath);
		return dir == null ? null : ResolveInDirectory(dir, "llama-bench", isWindows);
	}

	/// <summary>llama.cpp build number from a path like "b11053-cuda12x-..." ("" when absent).</summary>
	public static string ExtractBuildNumber(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}
		// Split on BOTH separators explicitly: on Unix AltDirectorySeparatorChar
		// equals '/' (not '\\'), so a Windows path would not split.
		foreach (string segment in path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment.StartsWith("b", StringComparison.Ordinal) && segment.Length > 1 && segment[1] >= '0' && segment[1] <= '9')
			{
				return segment;
			}
		}
		return string.Empty;
	}
}
