using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Models;

public class AppSettings : ISettings
{
	private string _settingsPath;

	private readonly ILogService? _log;

	public string LlamaCppBaseDirectory { get; set; } = string.Empty;

	public string LlamaCppDirectory { get; set; } = GetDefaultServerPath();

	public string ModelsDirectory { get; set; } = string.Empty;

	public string AdditionalModelsDirectories { get; set; } = string.Empty;

	public string ActiveLlamaCppVersion { get; set; } = string.Empty;

	public string Theme { get; set; } = "Dark";

	public string Language { get; set; } = "en";

	public bool AutoCheckUpdates { get; set; } = true;

	public bool AutoStartWithOS { get; set; }

	public bool ShowTrayIcon { get; set; } = true;

	public bool MinimizeToTray { get; set; } = true;

	public bool StartMinimized { get; set; }

	public bool StartMonitoringWindow { get; set; } = true;

	public bool ShowDesktopWindow { get; set; } = true;

	public bool KeepServerOnExit { get; set; }

	public bool AutoStartServer { get; set; }

	public bool AutoAttachExternalServer { get; set; } = true;

	public bool EncryptSessions { get; set; }

	public bool ClearClipboardOnExit { get; set; }

	public string HfDownloadDirectory { get; set; } = string.Empty;

	public string LastSelectedProfileId { get; set; } = string.Empty;

	public int MaxLogEntries { get; set; } = 5000;

	public string DefaultHost { get; set; } = "127.0.0.1";

	public int DefaultPort { get; set; } = 8080;

	public bool WebUiEnabled { get; set; } = true;

	public string WebUiHost { get; set; } = "0.0.0.0";

	public int WebUiPort { get; set; } = 8090;

	public bool WebUiRequireLogin { get; set; }

	public string WebUiUsername { get; set; } = string.Empty;

	public string WebUiPassword { get; set; } = string.Empty;

	public string DefaultGpuLayers { get; set; } = "all";

	public bool FlashAttention { get; set; } = true;

	public UpdateChannel UpdateChannel { get; set; }

	public double MonitorOpacity { get; set; } = 0.92;

	public bool MonitorAlwaysOnTop { get; set; } = true;

	public string MonitorDisplayStyle { get; set; } = "Bars";

	public bool MonitorShowTps { get; set; } = true;

	public bool MonitorShowVram { get; set; } = true;

	public bool MonitorShowRam { get; set; } = true;

	public bool MonitorShowGpuTemp { get; set; } = true;

	public bool MonitorShowPowerDraw { get; set; } = true;

	public bool MonitorShowGpuCore { get; set; } = true;

	public bool MonitorShowFanSpeed { get; set; } = true;

	public List<VersionInfo> InstalledVersionsCache { get; set; } = new List<VersionInfo>();

	public event Action? SettingsChanged;

	public string GetSettingsPath()
	{
		return _settingsPath;
	}

	public AppSettings(ILogService? log = null)
	{
		_log = log;
		_settingsPath = GetDefaultSettingsPath();
	}

	private static string GetDefaultServerPath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
	}

	private static string GetDefaultSettingsPath()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, "settings.json");
	}

	public async Task LoadAsync()
	{
		try
		{
			if (File.Exists(_settingsPath))
			{
				string json = await File.ReadAllTextAsync(_settingsPath);
				JsonSerializerOptions options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true,
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				};
				Dictionary<string, JsonElement> dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
				if (dictionary == null)
				{
					return;
				}
				if (dictionary.TryGetValue("llamaCppBaseDirectory", out var value))
				{
					LlamaCppBaseDirectory = value.GetString() ?? string.Empty;
				}
				if (dictionary.TryGetValue("llamaCppDirectory", out var value2))
				{
					string text = value2.GetString();
					if (!string.IsNullOrWhiteSpace(text))
					{
						LlamaCppDirectory = text;
					}
				}
				if (dictionary.TryGetValue("modelsDirectory", out var value3))
				{
					ModelsDirectory = value3.GetString() ?? string.Empty;
				}
				if (dictionary.TryGetValue("additionalModelsDirectories", out var value4))
				{
					AdditionalModelsDirectories = value4.GetString() ?? string.Empty;
				}
				if (dictionary.TryGetValue("activeLlamaCppVersion", out var value5))
				{
					ActiveLlamaCppVersion = value5.GetString() ?? string.Empty;
				}
				// Theme selection is temporarily disabled; keep old settings compatible but always use Dark.
				if (dictionary.ContainsKey("theme"))
				{
					Theme = "Dark";
				}
				if (dictionary.TryGetValue("language", out var value7))
				{
					Language = value7.GetString() ?? "en";
				}
				if (dictionary.TryGetValue("autoCheckUpdates", out var value8))
				{
					AutoCheckUpdates = value8.GetBoolean();
				}
				if (dictionary.TryGetValue("autoStartWithOS", out var value9))
				{
					AutoStartWithOS = value9.GetBoolean();
				}
				if (dictionary.TryGetValue("showTrayIcon", out var value10))
				{
					ShowTrayIcon = value10.GetBoolean();
				}
				if (dictionary.TryGetValue("minimizeToTray", out var value11))
				{
					MinimizeToTray = value11.GetBoolean();
				}
				if (dictionary.TryGetValue("startMinimized", out var value12))
				{
					StartMinimized = value12.GetBoolean();
				}
				if (dictionary.TryGetValue("startMonitoringWindow", out var value13))
				{
					StartMonitoringWindow = value13.GetBoolean();
				}
				if (dictionary.TryGetValue("showDesktopWindow", out var valueShowDesk)) ShowDesktopWindow = valueShowDesk.GetBoolean();
				if (dictionary.TryGetValue("keepServerOnExit", out var value14))
				{
					KeepServerOnExit = value14.GetBoolean();
				}
				if (dictionary.TryGetValue("autoStartServer", out var value15))
				{
					AutoStartServer = value15.GetBoolean();
				}
				if (dictionary.TryGetValue("autoAttachExternalServer", out var value16))
				{
					AutoAttachExternalServer = value16.GetBoolean();
				}
				if (dictionary.TryGetValue("encryptSessions", out var value17))
				{
					EncryptSessions = value17.GetBoolean();
				}
				if (dictionary.TryGetValue("clearClipboardOnExit", out var value18))
				{
					ClearClipboardOnExit = value18.GetBoolean();
				}
				if (dictionary.TryGetValue("hfDownloadDirectory", out var value19))
				{
					HfDownloadDirectory = value19.GetString() ?? string.Empty;
				}
				if (dictionary.TryGetValue("lastSelectedProfileId", out var value20))
				{
					LastSelectedProfileId = value20.GetString() ?? string.Empty;
				}
				if (dictionary.TryGetValue("maxLogEntries", out var value21))
				{
					MaxLogEntries = value21.GetInt32();
				}
				if (dictionary.TryGetValue("defaultHost", out var value22))
				{
					DefaultHost = value22.GetString() ?? "127.0.0.1";
				}
				if (dictionary.TryGetValue("defaultPort", out var value23))
				{
					DefaultPort = value23.GetInt32();
				}
				if (dictionary.TryGetValue("webUiEnabled", out var webUiEnabled)) WebUiEnabled = webUiEnabled.GetBoolean();
				if (dictionary.TryGetValue("webUiHost", out var webUiHost)) WebUiHost = webUiHost.GetString() ?? "0.0.0.0";
				if (dictionary.TryGetValue("webUiPort", out var webUiPort)) WebUiPort = webUiPort.GetInt32();
				if (dictionary.TryGetValue("webUiRequireLogin", out var webUiRequireLogin)) WebUiRequireLogin = webUiRequireLogin.GetBoolean();
				if (dictionary.TryGetValue("webUiUsername", out var webUiUsername)) WebUiUsername = webUiUsername.GetString() ?? string.Empty;
				if (dictionary.TryGetValue("webUiPassword", out var webUiPassword)) WebUiPassword = webUiPassword.GetString() ?? string.Empty;
				if (dictionary.TryGetValue("defaultGpuLayers", out var value24))
				{
					DefaultGpuLayers = ((value24.ValueKind == JsonValueKind.Number) ? value24.ToString() : (value24.GetString() ?? "all"));
				}
				if (dictionary.TryGetValue("flashAttention", out var value25))
				{
					FlashAttention = value25.GetBoolean();
				}
				if (dictionary.TryGetValue("updateChannel", out var value26))
				{
					UpdateChannel = (Enum.TryParse<UpdateChannel>(value26.GetString(), ignoreCase: true, out var result) ? result : UpdateChannel.Stable);
				}
				if (dictionary.TryGetValue("monitorOpacity", out var value27))
				{
					MonitorOpacity = value27.GetDouble();
				}
				if (dictionary.TryGetValue("monitorAlwaysOnTop", out var value28))
				{
					MonitorAlwaysOnTop = value28.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorDisplayStyle", out var valueDisplayStyle))
				{
					MonitorDisplayStyle = valueDisplayStyle.GetString() ?? "Bars";
				}
				if (dictionary.TryGetValue("monitorShowTps", out var value29))
				{
					MonitorShowTps = value29.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowVram", out var value30))
				{
					MonitorShowVram = value30.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowRam", out var value31))
				{
					MonitorShowRam = value31.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowGpuTemp", out var value32))
				{
					MonitorShowGpuTemp = value32.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowPowerDraw", out var value33))
				{
					MonitorShowPowerDraw = value33.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowGpuCore", out var value34))
				{
					MonitorShowGpuCore = value34.GetBoolean();
				}
				if (dictionary.TryGetValue("monitorShowFanSpeed", out var value35))
				{
					MonitorShowFanSpeed = value35.GetBoolean();
				}
				if (dictionary.TryGetValue("installedVersionsCache", out var value36))
				{
					InstalledVersionsCache = JsonSerializer.Deserialize<List<VersionInfo>>(value36.GetRawText()) ?? new List<VersionInfo>();
				}
			}
			if (string.IsNullOrWhiteSpace(LlamaCppBaseDirectory))
			{
				LlamaCppBaseDirectory = GetDefaultServerPath();
			}
			if (!string.IsNullOrWhiteSpace(ActiveLlamaCppVersion))
			{
				VersionInfo versionInfo = InstalledVersionsCache.FirstOrDefault((VersionInfo v) => v.VersionKey == ActiveLlamaCppVersion);
				if (versionInfo != null)
				{
					if (Directory.Exists(versionInfo.Directory) && File.Exists(Path.Combine(versionInfo.Directory, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server")))
					{
						LlamaCppDirectory = versionInfo.Directory;
					}
					return;
				}
				versionInfo = InstalledVersionsCache.FirstOrDefault((VersionInfo v) => v.Version == ActiveLlamaCppVersion);
				if (versionInfo != null)
				{
					if (Directory.Exists(versionInfo.Directory) && File.Exists(Path.Combine(versionInfo.Directory, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server")))
					{
						LlamaCppDirectory = versionInfo.Directory;
						ActiveLlamaCppVersion = versionInfo.VersionKey;
					}
					return;
				}
				string dirPath = LlamaCppDirectory?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty;
				VersionInfo versionInfo2 = InstalledVersionsCache.FirstOrDefault((VersionInfo v) => v.Directory == dirPath);
				if (versionInfo2 != null)
				{
					ActiveLlamaCppVersion = versionInfo2.VersionKey;
				}
			}
			else if (string.IsNullOrWhiteSpace(LlamaCppDirectory))
			{
				LlamaCppDirectory = LlamaCppBaseDirectory;
			}
		}
		catch
		{
		}
	}

	public async Task SaveAsync()
	{
		try
		{
			Theme = "Dark";
			string contents = SerializeToJson();
			string directoryName = Path.GetDirectoryName(_settingsPath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			await File.WriteAllTextAsync(_settingsPath, contents).ConfigureAwait(continueOnCapturedContext: false);
			this.SettingsChanged?.Invoke();
		}
		catch (Exception exception)
		{
			_log?.Error(exception, "Failed to save settings", "AppSettings");
		}
	}

	public void Save()
	{
		try
		{
			string contents = SerializeToJson();
			string directoryName = Path.GetDirectoryName(_settingsPath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(_settingsPath, contents);
		}
		catch
		{
		}
	}

	private string SerializeToJson()
	{
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		return JsonSerializer.Serialize(this, options);
	}
}
