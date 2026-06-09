using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Profiles;

public class ProfileManager : IProfileManager
{
    public event Action<string>? ProfileChanged;

    public void NotifyProfileChanged(string profileId) => ProfileChanged?.Invoke(profileId);

    readonly ISettings _settings;
    readonly ILogService _log;
    readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ProfileManager(ISettings settings, ILogService log)
    {
        _settings = settings;
        _log = log;
    }

    string GetProfilesDirectory()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
        var profilesDir = Path.Combine(baseDir, "profiles");

        if (!Directory.Exists(profilesDir))
            Directory.CreateDirectory(profilesDir);

        return profilesDir;
    }

    string GetProfilePath(string id) => Path.Combine(GetProfilesDirectory(), $"{id}.json");

    /// <summary>
    /// Читает JSON файл, пробуя UTF-8 сначала, затем fallback на системную кодировку.
    /// Возвращает true, если была миграция с legacy кодировки.
    /// </summary>
    static string ReadJsonFile(string path, out bool migrated)
    {
        migrated = false;
        var bytes = File.ReadAllBytes(path);

        // UTF-8 with BOM — strip BOM bytes before converting
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // Try UTF-8 without BOM
        try
        {
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            if (!text.Contains('\uFFFD'))
                return text;
        }
        catch { }

        // Fallback: system default encoding (CP1251 on Russian Windows)
        migrated = true;
        return System.Text.Encoding.Default.GetString(bytes);
    }
    List<ServerProfile> IProfileManager.GetAllProfiles()
    {
        var profiles = new List<ServerProfile>();
        var profilesDir = GetProfilesDirectory();

        if (!Directory.Exists(profilesDir))
            Directory.CreateDirectory(profilesDir);

        var files = Directory.GetFiles(profilesDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = ReadJsonFile(file, out var migrated);
                var profile = JsonSerializer.Deserialize<ServerProfile>(json, _jsonOptions);
                if (profile != null)
                {
                    profiles.Add(profile);
                    if (migrated)
                    {
                        var updatedJson = JsonSerializer.Serialize(profile, _jsonOptions);
                        File.WriteAllText(file, updatedJson, System.Text.Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to load profile: {file}", "ProfileManager");
            }
        }

        if (profiles.Count == 0)
        {
            profiles = CreateDefaultProfiles();
            foreach (var p in profiles)
                SaveProfileSync(p);
            _log.Information("Созданы профили по умолчанию", "ProfileManager");
        }

        profiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return profiles;
    }

    void SaveProfileSync(ServerProfile profile)
    {
        if (string.IsNullOrEmpty(profile.Id))
            profile.Id = Guid.NewGuid().ToString("N")[..8];

        profile.UpdatedAt = DateTime.Now;

        if (!profile.IsDefault)
        {
            // Check existing profiles on disk without full reload (avoid recursion)
            var profilesDir = GetProfilesDirectory();
            var hasDefault = false;
            if (Directory.Exists(profilesDir))
            {
                foreach (var file in Directory.GetFiles(profilesDir, "*.json"))
                {
                    try
                    {
                        var fileJson = ReadJsonFile(file, out _);
                        var p = JsonSerializer.Deserialize<ServerProfile>(fileJson, _jsonOptions);
                        if (p?.IsDefault == true)
                        {
                            hasDefault = true;
                            break;
                        }
                    }
                    catch { }
                }
            }
            if (!hasDefault)
                profile.IsDefault = true;
        }

        var path = GetProfilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        _log.Information($"Saved profile: {profile.Name} ({profile.Id})", "ProfileManager");
    }

    async Task<List<ServerProfile>> IProfileManager.GetAllProfilesAsync()
    {
        var profiles = new List<ServerProfile>();
        var profilesDir = GetProfilesDirectory();

        if (!Directory.Exists(profilesDir))
            Directory.CreateDirectory(profilesDir);

        var files = Directory.GetFiles(profilesDir, "*.json");

        foreach (var file in files)
            {
                try
                {
                    var json = ReadJsonFile(file, out var migrated);
                    var profile = JsonSerializer.Deserialize<ServerProfile>(json, _jsonOptions);
                    if (profile != null)
                    {
                        profiles.Add(profile);
                        if (migrated)
                        {
                            var updatedJson = JsonSerializer.Serialize(profile, _jsonOptions);
                            File.WriteAllText(file, updatedJson, System.Text.Encoding.UTF8);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Failed to load profile: {file}", "ProfileManager");
                }
            }

        // Если профилей нет — создать профили по умолчанию (как в референсе)
        if (profiles.Count == 0)
        {
            profiles = CreateDefaultProfiles();
            foreach (var p in profiles)
                await ((IProfileManager)this).SaveProfileAsync(p);

            _log.Information("Созданы профили по умолчанию", "ProfileManager");
        }

        profiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return profiles;
    }

   /// <summary>
    /// Создать профиль по умолчанию — один профиль с параметрами из Hermass.
    /// </summary>
    List<ServerProfile> CreateDefaultProfiles()
    {
        return new List<ServerProfile>
        {
            new ServerProfile
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Name = "Default",
                Description = "Default server profile",
                GpuLayers = "all",
                ContextSize = 0,
                BatchSize = 2048,
                UbatchSize = 512,
                Threads = -1,
                ThreadsBatch = -1,
                MainGpu = 0,
                FlashAttention = true,
                Mmap = true,
                Mlock = false,
                MmqEnabled = true,
                KvOffloadEnabled = true,
                Temperature = 0.8f,
                TopK = 40,
                TopP = 0.95f,
                MinP = 0.05f,
                TypicalP = 1.0f,
                RepeatPenalty = 1.0f,
                RepeatLastN = -1,
                PresencePenalty = 0f,
                FrequencyPenalty = 0f,
                Seed = -1,
                Mirostat = Core.Enums.MirostatMode.Disabled,
                MirostatTau = 5.0f,
                MirostatEta = 0.1f,
                DryMultiplier = 0f,
                DryBase = 1.75f,
                DynatempStddev = 0f,
                XtcProbability = 0f,
                XtcThreshold = 0.1f,
                Host = "127.0.0.1",
                Port = 8080,
                Timeout = 3600,
                Slots = -1,
                MaxTokens = -1,
                PredictCount = -1,
                CachePrompt = true,
                ContBatching = true,
                EnableWebUI = true,
                EnableSlots = true,
                EnableMetrics = false,
                VerboseLogging = false,
                Reasoning = false,
                ReasoningBudget = 0,
                EmbeddingMode = false,
                SpeculativeDecoding = false,
                SpecDraftGpuLayers = "",
                SpecDraftNMax = 3,
                SpecDraftNMin = 0,
                SpecDraftPSplit = 0.1f,
                SpecDraftPMin = 0.0f,
                ProcessPriority = "Normal",
                Numa = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDefault = true,
                Color = "#3B82F6"
            }
        };
    }

    async Task<ServerProfile?> IProfileManager.GetProfileAsync(string id)
    {
        var path = GetProfilePath(id);

        if (!File.Exists(path))
            return null;

        var json = ReadJsonFile(path, out _);
        return JsonSerializer.Deserialize<ServerProfile>(json, _jsonOptions);
    }

    ServerProfile IProfileManager.CreateProfile(string name)
    {
        var profile = new ServerProfile
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = name,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        ProfileChanged?.Invoke(profile.Id);
        return profile;
    }

    async Task<string> IProfileManager.SaveProfileAsync(ServerProfile profile)
    {
        if (string.IsNullOrEmpty(profile.Id))
            profile.Id = Guid.NewGuid().ToString("N")[..8];

        profile.UpdatedAt = DateTime.Now;

        if (!profile.IsDefault)
        {
            // Lightweight check: only scan disk for IsDefault flag, no full reload
            var profilesDir = GetProfilesDirectory();
            if (Directory.Exists(profilesDir))
            {
                bool hasDefault = false;
                foreach (var file in Directory.GetFiles(profilesDir, "*.json"))
                {
                    try
                    {
                        var fileJson = ReadJsonFile(file, out _);
                        var p = JsonSerializer.Deserialize<ServerProfile>(fileJson, _jsonOptions);
                        if (p?.IsDefault == true)
                        {
                            hasDefault = true;
                            break;
                        }
                    }
                    catch { }
                }
                if (!hasDefault)
                    profile.IsDefault = true;
            }
        }

        var path = GetProfilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(path, json, System.Text.Encoding.UTF8);

        _log.Information($"Saved profile: {profile.Name} ({profile.Id})", "ProfileManager");
        ProfileChanged?.Invoke(profile.Id);
        return profile.Id;
    }

    async Task<bool> IProfileManager.DeleteProfileAsync(string id)
    {
        var path = GetProfilePath(id);

        if (!File.Exists(path))
            return false;

        File.Delete(path);
        _log.Information($"Deleted profile: {id}", "ProfileManager");
        await Task.CompletedTask;
        return true;
    }

    async Task<ServerProfile> IProfileManager.DuplicateProfileAsync(string id)
    {
        var original = await ((IProfileManager)this).GetProfileAsync(id);

        if (original == null)
            throw new ArgumentException($"Profile not found: {id}");

        var copy = new ServerProfile
        {
            Name = original.Name + " (Copy)",
            Description = original.Description,
            ModelPath = original.ModelPath,
            MmprojPath = original.MmprojPath,
            DraftModelPath = original.DraftModelPath,
            LlamaCppVersion = original.LlamaCppVersion,
            GpuLayers = original.GpuLayers,
            GpuSplitMode = original.GpuSplitMode,
            TensorSplit = original.TensorSplit.ToArray(),
            MainGpu = original.MainGpu,
            FlashAttention = original.FlashAttention,
            Mmap = original.Mmap,
            Mlock = original.Mlock,
        MmqEnabled = original.MmqEnabled,
             KvOffloadEnabled = original.KvOffloadEnabled,
            ContextSize = original.ContextSize,
            BatchSize = original.BatchSize,
            UbatchSize = original.UbatchSize,
            CacheTypeK = original.CacheTypeK,
            CacheTypeV = original.CacheTypeV,
            Threads = original.Threads,
            ThreadsBatch = original.ThreadsBatch,
            Temperature = original.Temperature,
            TopK = original.TopK,
            TopP = original.TopP,
            MinP = original.MinP,
            TypicalP = original.TypicalP,
            RepeatPenalty = original.RepeatPenalty,
            RepeatLastN = original.RepeatLastN,
            Mirostat = original.Mirostat,
            MirostatTau = original.MirostatTau,
            MirostatEta = original.MirostatEta,
            DryMultiplier = original.DryMultiplier,
            DryBase = original.DryBase,
            DynatempStddev = original.DynatempStddev,
            XtcProbability = original.XtcProbability,
            XtcThreshold = original.XtcThreshold,
            Host = original.Host,
            Port = original.Port,
            Timeout = original.Timeout,
            Slots = original.Slots,
            PredictCount = original.PredictCount,
            SpecType = original.SpecType,
            SpecDraftGpuLayers = original.SpecDraftGpuLayers,
            SpeculativeDecoding = original.SpeculativeDecoding,
            RopeFreqBase = original.RopeFreqBase,
            RopeFreqScale = original.RopeFreqScale,
            YarnOriginalContext = original.YarnOriginalContext,
            YarnExtFactor = original.YarnExtFactor,
            YarnAttnFactor = original.YarnAttnFactor,
            YarnBetaFast = original.YarnBetaFast,
            YarnBetaSlow = original.YarnBetaSlow,
            EmbeddingMode = original.EmbeddingMode,
            PoolingType = original.PoolingType,
            ProcessPriority = original.ProcessPriority,
            Numa = original.Numa,
            CpuAffinity = original.CpuAffinity,
            CustomArguments = new Dictionary<string, string>(),
           CustomArgumentToggleStates = new Dictionary<string, bool>(),
            Color = original.Color,
            IsDefault = false
        };

        copy.Id = Guid.NewGuid().ToString("N")[..8];
        copy.CreatedAt = DateTime.Now;

        await ((IProfileManager)this).SaveProfileAsync(copy);
        return copy;
    }

    async Task<ServerProfile> IProfileManager.ImportProfileAsync(string json)
    {
        var profile = JsonSerializer.Deserialize<ServerProfile>(json, _jsonOptions);

        if (profile == null)
            throw new InvalidDataException("Invalid profile JSON");

        profile.Id = Guid.NewGuid().ToString("N")[..8];
        profile.CreatedAt = DateTime.Now;
        profile.UpdatedAt = DateTime.Now;

        await ((IProfileManager)this).SaveProfileAsync(profile);
        return profile;
    }

    string IProfileManager.ExportProfile(ServerProfile profile)
    {
        return JsonSerializer.Serialize(profile, _jsonOptions);
    }

    async Task IProfileManager.SetDefaultProfileAsync(string id)
    {
        var profiles = await ((IProfileManager)this).GetAllProfilesAsync();

        foreach (var p in profiles)
        {
            p.IsDefault = p.Id == id;
            await ((IProfileManager)this).SaveProfileAsync(p);
        }

        _log.Information($"Set default profile: {id}", "ProfileManager");
    }

    async Task<ServerProfile?> IProfileManager.GetDefaultProfileAsync()
    {
        var profiles = await ((IProfileManager)this).GetAllProfilesAsync();
        var @default = profiles.FirstOrDefault(p => p.IsDefault);
        if (@default != null) return @default;
        return profiles.FirstOrDefault();
    }
}
