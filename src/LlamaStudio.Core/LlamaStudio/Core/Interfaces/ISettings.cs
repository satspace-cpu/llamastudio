using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface ISettings
{
	string LlamaCppBaseDirectory { get; set; }

	string LlamaCppDirectory { get; set; }

	string ModelsDirectory { get; set; }

	string AdditionalModelsDirectories { get; set; }

	string ActiveLlamaCppVersion { get; set; }

	string Theme { get; set; }

	string Language { get; set; }

	bool AutoCheckUpdates { get; set; }

	bool AutoStartWithOS { get; set; }

	bool ShowTrayIcon { get; set; }

	bool MinimizeToTray { get; set; }

	bool StartMinimized { get; set; }

	bool StartMonitoringWindow { get; set; }

	bool ShowDesktopWindow { get; set; }

	bool KeepServerOnExit { get; set; }

	bool AutoStartServer { get; set; }

	bool AutoAttachExternalServer { get; set; }

	bool EncryptSessions { get; set; }

	bool ClearClipboardOnExit { get; set; }

	string HfDownloadDirectory { get; set; }

	string LastSelectedProfileId { get; set; }

	int MaxLogEntries { get; set; }

	string DefaultHost { get; set; }

	int DefaultPort { get; set; }

	bool WebUiEnabled { get; set; }

	string WebUiHost { get; set; }

	int WebUiPort { get; set; }

	bool WebUiRequireLogin { get; set; }

	string WebUiUsername { get; set; }

	string WebUiPassword { get; set; }

	string DefaultGpuLayers { get; set; }

	bool FlashAttention { get; set; }

	UpdateChannel UpdateChannel { get; set; }

	double MonitorOpacity { get; set; }

	bool MonitorAlwaysOnTop { get; set; }

	string MonitorDisplayStyle { get; set; }

	bool MonitorShowTps { get; set; }

	bool MonitorShowVram { get; set; }

	bool MonitorShowRam { get; set; }

	bool MonitorShowGpuTemp { get; set; }

	bool MonitorShowPowerDraw { get; set; }

	bool MonitorShowGpuCore { get; set; }

	bool MonitorShowFanSpeed { get; set; }

	List<VersionInfo> InstalledVersionsCache { get; set; }

	event Action? SettingsChanged;

	Task SaveAsync();

	void Save();

	Task LoadAsync();

	string GetSettingsPath();
}
