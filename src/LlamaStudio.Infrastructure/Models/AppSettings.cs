using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Models;

public class AppSettings : ISettings
{
    const string DefaultJson = """
    {
      "LlamaCppDirectory": "",
      "ModelsDirectory": "",
      "AdditionalModelsDirectories": "",
      "ActiveLlamaCppVersion": "",
      "Theme": "Dark",
      "Language": "en",
      "AutoCheckUpdates": true,
      "AutoStartWithOS": false,
    "ShowTrayIcon": true,
       "StartMonitoringWindow": true,
      "KeepServerOnExit": false,
      "LastSelectedProfileId": ""
      "MaxLogEntries": 5000,
      "DefaultHost": "127.0.0.1",
      "DefaultPort": 8080,
      "DefaultGpuLayers": "all",
      "FlashAttention": true,
      "UpdateChannel": "Stable",
      "MonitorOpacity": 0.92,
      "MonitorAlwaysOnTop": true,
      "MonitorShowTps": true,
      "MonitorShowVram": true,
      "MonitorShowRam": true,
      "MonitorShowGpuTemp": true,
      "MonitorShowPowerDraw": true,
      "MonitorShowGpuCore": true,
      "MonitorShowFanSpeed": true
    }
    """;

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
    public bool StartMinimized { get; set; } = false;
    public bool StartMonitoringWindow { get; set; } = true;
    public bool KeepServerOnExit { get; set; } = false;
    public string LastSelectedProfileId { get; set; } = string.Empty;
    public int MaxLogEntries { get; set; } = 5000;
    public string DefaultHost { get; set; } = "127.0.0.1";
    public int DefaultPort { get; set; } = 8080;
    public string DefaultGpuLayers { get; set; } = "all";
    public bool FlashAttention { get; set; } = true;
    public UpdateChannel UpdateChannel { get; set; } = UpdateChannel.Stable;

    // Monitoring settings
    public double MonitorOpacity { get; set; } = 0.92;
    public bool MonitorAlwaysOnTop { get; set; } = true;
    public bool MonitorShowTps { get; set; } = true;
    public bool MonitorShowVram { get; set; } = true;
    public bool MonitorShowRam { get; set; } = true;
    public bool MonitorShowGpuTemp { get; set; } = true;
    public bool MonitorShowPowerDraw { get; set; } = true;
    public bool MonitorShowGpuCore { get; set; } = true;
    public bool MonitorShowFanSpeed { get; set; } = true;

    public string GetSettingsPath() => _settingsPath;
    string _settingsPath;

    public AppSettings()
    {
        _settingsPath = GetDefaultSettingsPath();
    }

    static string GetDefaultServerPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LlamaStudio");
    }

    static string GetDefaultSettingsPath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LlamaStudio");

        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);

        return Path.Combine(appData, "settings.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, opts);
                if (data == null) return;

                if (data.TryGetValue("llamaCppBaseDirectory", out var d0)) LlamaCppBaseDirectory = d0.GetString() ?? string.Empty;
                if (data.TryGetValue("llamaCppDirectory", out var d1))
                {
                    var v = d1.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) LlamaCppDirectory = v;
                }
                if (data.TryGetValue("modelsDirectory", out var d2)) ModelsDirectory = d2.GetString() ?? string.Empty;
                if (data.TryGetValue("additionalModelsDirectories", out var d3)) AdditionalModelsDirectories = d3.GetString() ?? string.Empty;
                if (data.TryGetValue("activeLlamaCppVersion", out var d4)) ActiveLlamaCppVersion = d4.GetString() ?? string.Empty;
                if (data.TryGetValue("theme", out var d5)) Theme = d5.GetString() ?? "Dark";
                if (data.TryGetValue("language", out var d6)) Language = d6.GetString() ?? "en";
                if (data.TryGetValue("autoCheckUpdates", out var d7)) AutoCheckUpdates = d7.GetBoolean();
                if (data.TryGetValue("autoStartWithOS", out var d8b)) AutoStartWithOS = d8b.GetBoolean();
                if (data.TryGetValue("showTrayIcon", out var d9)) ShowTrayIcon = d9.GetBoolean();
                if (data.TryGetValue("minimizeToTray", out var d9b)) MinimizeToTray = d9b.GetBoolean();
                if (data.TryGetValue("startMinimized", out var d9f)) StartMinimized = d9f.GetBoolean();
                if (data.TryGetValue("startMonitoringWindow", out var d9e)) StartMonitoringWindow = d9e.GetBoolean();
                if (data.TryGetValue("keepServerOnExit", out var d9d)) KeepServerOnExit = d9d.GetBoolean();
                if (data.TryGetValue("lastSelectedProfileId", out var d9c)) LastSelectedProfileId = d9c.GetString() ?? string.Empty;
                if (data.TryGetValue("maxLogEntries", out var d11)) MaxLogEntries = d11.GetInt32();
                if (data.TryGetValue("defaultHost", out var d12)) DefaultHost = d12.GetString() ?? "127.0.0.1";
                if (data.TryGetValue("defaultPort", out var d13)) DefaultPort = d13.GetInt32();
                if (data.TryGetValue("defaultGpuLayers", out var d14)) DefaultGpuLayers = d14.ValueKind == JsonValueKind.Number ? d14.ToString() : (d14.GetString() ?? "all");
                if (data.TryGetValue("flashAttention", out var d15)) FlashAttention = d15.GetBoolean();
                if (data.TryGetValue("updateChannel", out var d16))
                    UpdateChannel = Enum.TryParse<UpdateChannel>(d16.GetString(), true, out var ch) ? ch : UpdateChannel.Stable;
                if (data.TryGetValue("monitorOpacity", out var m1)) MonitorOpacity = m1.GetDouble();
                if (data.TryGetValue("monitorAlwaysOnTop", out var m2)) MonitorAlwaysOnTop = m2.GetBoolean();
                if (data.TryGetValue("monitorShowTps", out var m3)) MonitorShowTps = m3.GetBoolean();
                if (data.TryGetValue("monitorShowVram", out var m4)) MonitorShowVram = m4.GetBoolean();
                if (data.TryGetValue("monitorShowRam", out var m5)) MonitorShowRam = m5.GetBoolean();
                if (data.TryGetValue("monitorShowGpuTemp", out var m6)) MonitorShowGpuTemp = m6.GetBoolean();
                if (data.TryGetValue("monitorShowPowerDraw", out var m7)) MonitorShowPowerDraw = m7.GetBoolean();
                if (data.TryGetValue("monitorShowGpuCore", out var m8)) MonitorShowGpuCore = m8.GetBoolean();
                if (data.TryGetValue("monitorShowFanSpeed", out var m9)) MonitorShowFanSpeed = m9.GetBoolean();
            }

            // Apply defaults for empty values
            if (string.IsNullOrWhiteSpace(LlamaCppDirectory))
                LlamaCppDirectory = GetDefaultServerPath();

            // If ActiveLlamaCppVersion is set, ensure LlamaCppDirectory points to the actual server folder
            if (!string.IsNullOrWhiteSpace(ActiveLlamaCppVersion))
            {
                var versionedPath = Path.Combine(LlamaCppDirectory, ActiveLlamaCppVersion);
                if (Directory.Exists(versionedPath) && File.Exists(Path.Combine(versionedPath, "llama-server.exe")))
                {
                    LlamaCppDirectory = versionedPath;
                }
            }
        }
        catch (Exception ex)
        {
         }
    }

    public async Task SaveAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(this, opts);
            await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
