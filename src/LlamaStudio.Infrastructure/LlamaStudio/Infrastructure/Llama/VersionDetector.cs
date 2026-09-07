using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class VersionDetector : IVersionDetector
{
	private readonly ILogService _log;

	public VersionDetector(ILogService log)
	{
		_log = log;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint SetErrorMode(uint uMode);

	public async Task<VersionInfo?> DetectAsync(string directory, CancellationToken ct = default(CancellationToken))
	{
		string exePath = Path.Combine(directory, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server");
		if (!File.Exists(exePath))
		{
			return null;
		}
		string folderName = Path.GetFileName(directory);
		// cafe-llama.cpp installs to a fixed, non-versioned folder and reports a
		// generic "0.3.0-dev (commit ...)" banner, so neither the folder-name tag
		// regex nor the --version regex identify it.  Recognize the dedicated
		// folder directly so it shows up as a distinct, stable installed version.
		if (folderName.StartsWith("cafe-llama-", StringComparison.OrdinalIgnoreCase))
		{
		(string, string, string, string, string) cafeInfo = await GetVersionFromExeAsync(exePath, ct);
		return new VersionInfo
		{
		Version = "cafe-main",
		BuildType = "Cuda12x",
		Commit = cafeInfo.Item1,
		Directory = directory,
		VersionBanner = cafeInfo.Item3,
		BuildNumber = cafeInfo.Item4,
		CommitHash = cafeInfo.Item5
		};
		}
		// r73: Unsloth MTP builds (a separate upstream fork with MTP support for
		// models such as Qwen3.8-Flash-Next). Folders are named
		// b####-mix-<sha>-unsloth-mtp-cuda12x, and the UI name must stay explicitly
		// distinguishable from the official llama.cpp builds.
		if (folderName.Contains("-unsloth-mtp-", StringComparison.OrdinalIgnoreCase))
		{
			Match unslothMatch = Regex.Match(folderName, @"^(?<v>b\d+(?:-[0-9a-z]+)+?)(?:-cuda\d+x)?$", RegexOptions.IgnoreCase);
			(string, string, string, string, string) unslothInfo = await GetVersionFromExeAsync(exePath, ct);
			return new VersionInfo
			{
			Version = unslothMatch.Success ? unslothMatch.Groups["v"].Value.ToLowerInvariant() : "unsloth-mtp",
			BuildType = "Cuda12x",
			Commit = unslothInfo.Item5,
			Directory = directory,
			VersionBanner = unslothInfo.Item3,
			BuildNumber = unslothInfo.Item4,
			CommitHash = unslothInfo.Item5
			};
		}
		// llama.cpp moved from rolling bNNNNN builds to semantic tags such as
		// v0.2.0.  The folder is the reliable release identity for locally built
		// Linux CUDA/RPC versions, because --version reports a generic build 1.
		Match folderMatch = Regex.Match(folderName, @"^(?<tag>b\d+|v\d+(?:\.\d+){1,3}(?:[-+][0-9A-Za-z][0-9A-Za-z._-]*)?)", RegexOptions.IgnoreCase);
		(string, string, string, string, string) exeInfo = await GetVersionFromExeAsync(exePath, ct);
		// Folder names created by LlamaStudio preserve the actual release tag
		// (for example b10361-cuda12x). Prefer it over llama.cpp's generic
		// "version: 1 (b10361)" banner.
		string version = folderMatch.Success ? folderMatch.Groups["tag"].Value.ToLowerInvariant() : exeInfo.Item1;
		string commit = exeInfo.Item2;
		string buildType = DetectBuildTypeFromDlls(directory);
		if (string.IsNullOrEmpty(version))
		{
			_log.Warning("Cannot determine version for " + directory + " (--version failed, folder name doesn't match pattern)", "VersionDetector");
			return null;
		}
		return new VersionInfo
		{
			Version = version,
			BuildType = buildType,
			Commit = commit,
			Directory = directory,
			VersionBanner = exeInfo.Item3,
			BuildNumber = exeInfo.Item4,
			CommitHash = exeInfo.Item5
		};
	}

	private async Task<(string version, string commit, string banner, string buildNumber, string commitHash)> GetVersionFromExeAsync(string exePath, CancellationToken ct)
	{
		try
		{
			string exeDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
			string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			string newPath = exeDirectory + Path.PathSeparator + currentPath;
			uint oldMode = 0u;
			if (OperatingSystem.IsWindows())
			{
				oldMode = SetErrorMode(2u);
				SetErrorMode(oldMode | 2);
			}
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo
				{
					FileName = OperatingSystem.IsWindows() ? "cmd.exe" : exePath,
					Arguments = OperatingSystem.IsWindows() ? "/c \"" + exePath + "\" --version" : "--version",
					WorkingDirectory = exeDirectory,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
				{
					processStartInfo.Environment[(string)environmentVariable.Key] = (string)environmentVariable.Value;
				}
				processStartInfo.Environment["PATH"] = newPath;
				// Locally built Linux packages keep their shared libraries next to
				// llama-server.  Supplying that directory here makes the scanner work
				// even for older builds that had an absolute CMake RUNPATH.
				if (OperatingSystem.IsLinux())
				{
					string libraryPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
					processStartInfo.Environment["LD_LIBRARY_PATH"] = exeDirectory + Path.PathSeparator + libraryPath;
				}
				using Process proc = Process.Start(processStartInfo);
				if (proc == null)
				{
					return (version: string.Empty, commit: string.Empty, banner: string.Empty, buildNumber: string.Empty, commitHash: string.Empty);
				}
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(TimeSpan.FromSeconds(10.0));
				try
				{
					await proc.WaitForExitAsync(cts.Token);
				}
				catch (OperationCanceledException)
				{
					try
					{
						proc.Kill();
					}
					catch
					{
					}
					return (version: string.Empty, commit: string.Empty, banner: string.Empty, buildNumber: string.Empty, commitHash: string.Empty);
				}
				string versionOutput = await proc.StandardOutput.ReadToEndAsync() + await proc.StandardError.ReadToEndAsync();
				Match releaseMatch = Regex.Match(versionOutput, @"\b(v\d+(?:\.\d+){1,3}(?:[-+][0-9A-Za-z][0-9A-Za-z._-]*)?)\b|\b(b\d+)\b", RegexOptions.IgnoreCase);
				// r71: parse the full version banner so the UI can show the real
				// commit/build of the binary that lives in the folder.
				string banner = string.Empty;
				string buildNumber = string.Empty;
				string commitHash = string.Empty;
				Match bannerMatch = Regex.Match(versionOutput, @"^\s*version:\s*(?<banner>\S.*?)\s*(?:\(\s*build\s+(?<build>\d+)\s*,\s*commit\s+(?<commit>[0-9a-fA-F]{6,40})\s*\))?\s*$", RegexOptions.Multiline);
				if (bannerMatch.Success)
				{
					banner = bannerMatch.Groups["banner"].Value.Trim();
					buildNumber = bannerMatch.Groups["build"].Success ? bannerMatch.Groups["build"].Value : string.Empty;
					commitHash = bannerMatch.Groups["commit"].Success ? bannerMatch.Groups["commit"].Value.ToLowerInvariant() : string.Empty;
				}
				if (releaseMatch.Success)
				{
					return (version: releaseMatch.Value.ToLowerInvariant(), commit: string.Empty, banner: banner, buildNumber: buildNumber, commitHash: commitHash);
				}
				return (version: string.Empty, commit: string.Empty, banner: banner, buildNumber: buildNumber, commitHash: commitHash);
			}
			finally
			{
				if (OperatingSystem.IsWindows())
				{
					SetErrorMode(oldMode);
				}
			}
		}
		catch (Exception ex)
		{
			_log.Warning("Failed to detect version from " + exePath + ": " + ex.Message, "VersionDetector");
			return (version: string.Empty, commit: string.Empty, banner: string.Empty, buildNumber: string.Empty, commitHash: string.Empty);
		}
	}

	private static string DetectBuildTypeFromDlls(string directory)
	{
		try
		{
			// On Linux libcudart is often supplied by the system and does not live
			// beside llama-server.  Preserve the selected CUDA toolkit from the
			// folder name created by the installer before falling back to libraries.
			string folderName = Path.GetFileName(directory).ToLowerInvariant();
			if (folderName.Contains("cuda13"))
			{
				return "Cuda13x";
			}
			if (folderName.Contains("cuda12"))
			{
				return "Cuda12x";
			}
			IEnumerable<string> source = Directory.GetFiles(directory, OperatingSystem.IsWindows() ? "*.dll" : "*", SearchOption.AllDirectories)
				.Select(Path.GetFileName)
				.Where((string? name) => !string.IsNullOrWhiteSpace(name))
				.Select((string name) => name.ToLowerInvariant());
			if (source.Any((string n) => n.Contains("cudart64_13")))
			{
				return "Cuda13x";
			}
			if (source.Any((string n) => n.Contains("cudart64_12") || n.Contains("libggml-cuda") || n.Contains("ggml-cuda")))
			{
				return "Cuda12x";
			}
			if (source.Any((string n) => n.Contains("ggml-vulkan") || n.Contains("vulkan-1")))
			{
				return "Vulkan";
			}
			if (source.Any((string n) => n.Contains("cudart") || n.Contains("cublas")))
			{
				return "Cuda";
			}
			return "CPU";
		}
		catch
		{
			return "Unknown";
		}
	}
}
