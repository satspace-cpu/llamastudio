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
    bool KeepServerOnExit { get; set; }
    string LastSelectedProfileId { get; set; }
    int MaxLogEntries { get; set; }
    string DefaultHost { get; set; }
    int DefaultPort { get; set; }
    string DefaultGpuLayers { get; set; }
    bool FlashAttention { get; set; }
    Enums.UpdateChannel UpdateChannel { get; set; }

    // Monitoring window settings
    double MonitorOpacity { get; set; }
    bool MonitorAlwaysOnTop { get; set; }
    bool MonitorShowTps { get; set; }
    bool MonitorShowVram { get; set; }
    bool MonitorShowRam { get; set; }
    bool MonitorShowGpuTemp { get; set; }
    bool MonitorShowPowerDraw { get; set; }
    bool MonitorShowGpuCore { get; set; }
    bool MonitorShowFanSpeed { get; set; }

    Task SaveAsync();
    void Save();
    Task LoadAsync();
    string GetSettingsPath();
}
