using System.Text.Json.Serialization;
using System.Globalization;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

public class ServerProfile
{
    const int DefaultPort = 8080;
    const int DefaultTimeout = 3600;
    const int DefaultSlots = -1;
    const int DefaultContextSize = 0;
    const int DefaultThreads = -1;
    const string DefaultGpuLayers = "all";
    const int DefaultBatchSize = 2048;
    const int DefaultUbatchSize = 512;
    const int DefaultMaxTokens = -1;
    const float DefaultTemperature = 0.8f;
    const int DefaultTopK = 40;
    const float DefaultTopP = 0.95f;
    const float DefaultMinP = 0.05f;
    const float DefaultTypicalP = 1.0f;
    const float DefaultRepeatPenalty = 1.1f;
    const int DefaultRepeatLastN = -1;
    const float DefaultDryBase = 1.75f;
    const float DefaultXtcThreshold = 0.1f;

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("modelPath")]
    public string? ModelPath { get; set; }

    [JsonPropertyName("mmprojPath")]
    public string? MmprojPath { get; set; }

    [JsonPropertyName("draftModelPath")]
    public string? DraftModelPath { get; set; }

    [JsonPropertyName("llamaCppVersion")]
    public string? LlamaCppVersion { get; set; }

    // HuggingFace download
    [JsonPropertyName("hfRepo")]
    public string? HfRepo { get; set; }

    [JsonPropertyName("hfFile")]
    public string? HfFile { get; set; }

    [JsonPropertyName("hfOffline")]
    public bool HfOffline { get; set; }

    [JsonPropertyName("hfRepoDraft")]
    public string? HfRepoDraft { get; set; }

    [JsonPropertyName("hfUrl")]
    public string? HfUrl { get; set; }

    // Speculative draft parameters
    [JsonPropertyName("specDraftGpuLayers")]
    public string SpecDraftGpuLayers { get; set; } = string.Empty;

    [JsonPropertyName("specDraftNMax")]
    public int SpecDraftNMax { get; set; } = 3;

    [JsonPropertyName("specDraftNMin")]
    public int SpecDraftNMin { get; set; } = 0;

    [JsonPropertyName("specDraftPSplit")]
    public float SpecDraftPSplit { get; set; } = 0.1f;

    [JsonPropertyName("specDraftPMin")]
    public float SpecDraftPMin { get; set; } = 0.0f;

    // Extra server options from reference
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("logFilePath")]
    public string? LogFilePath { get; set; }

    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; }

    [JsonPropertyName("enableWebUI")]
    public bool EnableWebUI { get; set; } = true;

    [JsonPropertyName("enableSlots")]
    public bool EnableSlots { get; set; } = true;

    [JsonPropertyName("enableMetrics")]
    public bool EnableMetrics { get; set; }

    [JsonPropertyName("reasoning")]
    public bool Reasoning { get; set; }

    [JsonPropertyName("reasoningBudget")]
    public int ReasoningBudget { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; } = -1;

    [JsonPropertyName("presencePenalty")]
    public float PresencePenalty { get; set; }

    [JsonPropertyName("frequencyPenalty")]
    public float FrequencyPenalty { get; set; }

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = DefaultMaxTokens;

    [JsonPropertyName("cachePrompt")]
    public bool CachePrompt { get; set; } = true;

    [JsonPropertyName("contBatching")]
    public bool ContBatching { get; set; } = true;

    [JsonPropertyName("contextShift")]
    public bool ContextShift { get; set; } = true;

    [JsonPropertyName("gpuLayers")]
    public string GpuLayers { get; set; } = DefaultGpuLayers;

    [JsonPropertyName("gpuSplitMode")]
    public GpuSplitMode GpuSplitMode { get; set; }

    [JsonPropertyName("tensorSplit")]
    public int[] TensorSplit { get; set; } = Array.Empty<int>();

    [JsonPropertyName("mainGpu")]
    public int MainGpu { get; set; }

    [JsonPropertyName("flashAttention")]
    public bool FlashAttention { get; set; } = true;

    [JsonPropertyName("mmap")]
    public bool Mmap { get; set; } = true;

    [JsonPropertyName("mlock")]
    public bool Mlock { get; set; }

    [JsonPropertyName("mmqEnabled")]
    public bool MmqEnabled { get; set; } = true;

    [JsonPropertyName("kvOffloadEnabled")]
    public bool KvOffloadEnabled { get; set; } = true;

    [JsonPropertyName("contextSize")]
    public int ContextSize { get; set; } = DefaultContextSize;

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; } = 2048;

    [JsonPropertyName("ubatchSize")]
    public int UbatchSize { get; set; } = 512;

    [JsonPropertyName("cacheTypeK")]
    public CacheTypeK CacheTypeK { get; set; } = CacheTypeK.F16;

    [JsonPropertyName("cacheTypeV")]
    public CacheTypeV CacheTypeV { get; set; } = CacheTypeV.F16;

    [JsonPropertyName("cacheRam")]
    public int CacheRam { get; set; } = -1;

    [JsonPropertyName("cacheReuse")]
    public int CacheReuse { get; set; } = 32;

    [JsonPropertyName("threads")]
    public int Threads { get; set; } = DefaultThreads;

    [JsonPropertyName("threadsBatch")]
    public int ThreadsBatch { get; set; } = DefaultThreads;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.8f;

    [JsonPropertyName("topK")]
    public int TopK { get; set; } = 40;

    [JsonPropertyName("topP")]
    public float TopP { get; set; } = 0.95f;

    [JsonPropertyName("minP")]
    public float MinP { get; set; } = 0.05f;

    [JsonPropertyName("typicalP")]
    public float TypicalP { get; set; } = 1.0f;

    [JsonPropertyName("repeatPenalty")]
    public float RepeatPenalty { get; set; } = 1.1f;

    [JsonPropertyName("repeatLastN")]
    public int RepeatLastN { get; set; } = -1;

    [JsonPropertyName("mirostat")]
    public MirostatMode Mirostat { get; set; }

    [JsonPropertyName("mirostatTau")]
    public float MirostatTau { get; set; } = 5.0f;

    [JsonPropertyName("mirostatEta")]
    public float MirostatEta { get; set; } = 0.1f;

    [JsonPropertyName("dryMultiplier")]
    public float DryMultiplier { get; set; }

    [JsonPropertyName("dryBase")]
    public float DryBase { get; set; } = 1.75f;

    [JsonPropertyName("dynatempStddev")]
    public float DynatempStddev { get; set; }

    [JsonPropertyName("xtcProbability")]
    public float XtcProbability { get; set; }

    [JsonPropertyName("xtcThreshold")]
    public float XtcThreshold { get; set; } = 0.1f;

    [JsonPropertyName("host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 8080;

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = DefaultTimeout;

    [JsonPropertyName("slots")]
    public int Slots { get; set; } = DefaultSlots;

    [JsonPropertyName("predictCount")]
    public int PredictCount { get; set; } = -1;

    [JsonPropertyName("specType")]
    public string SpecType { get; set; } = string.Empty;

    [JsonPropertyName("speculativeDecoding")]
    public bool SpeculativeDecoding { get; set; }

    [JsonPropertyName("ropeScaling")]
    public string RopeScaling { get; set; } = string.Empty;

    [JsonPropertyName("ropeFreqBase")]
    public double? RopeFreqBase { get; set; }

    [JsonPropertyName("ropeFreqScale")]
    public double? RopeFreqScale { get; set; }

    [JsonPropertyName("yarnOriginalContext")]
    public int? YarnOriginalContext { get; set; }

    [JsonPropertyName("yarnExtFactor")]
    public double? YarnExtFactor { get; set; }

    [JsonPropertyName("yarnAttnFactor")]
    public double? YarnAttnFactor { get; set; }

    [JsonPropertyName("yarnBetaFast")]
    public double? YarnBetaFast { get; set; }

    [JsonPropertyName("yarnBetaSlow")]
    public double? YarnBetaSlow { get; set; }

    [JsonPropertyName("embeddingMode")]
    public bool EmbeddingMode { get; set; }

    [JsonPropertyName("poolingType")]
    public string PoolingType { get; set; } = string.Empty;

    [JsonPropertyName("processPriority")]
    public string ProcessPriority { get; set; } = "Normal";

    [JsonPropertyName("priorityHigh")]
    public bool PriorityHigh { get; set; }

    [JsonPropertyName("numa")]
    public bool Numa { get; set; }

    [JsonPropertyName("cpuAffinity")]
    public string? CpuAffinity { get; set; }

    [JsonPropertyName("customArguments")]
    public Dictionary<string, string> CustomArguments { get; set; } = new();

    [JsonPropertyName("customArgumentToggleStates")]
    public Dictionary<string, bool> CustomArgumentToggleStates { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#3B82F6";

    // --- KnownArguments: maps every known alias to the canonical argument name ---
    private static readonly Dictionary<string, ArgumentMapping> KnownArguments = new(StringComparer.OrdinalIgnoreCase)
    {
        { "host", new ArgumentMapping("--host", ArgType.String) },
        { "port", new ArgumentMapping("--port", ArgType.Int) },
        { "timeout", new ArgumentMapping("--timeout", ArgType.Int) },
        { "m", new ArgumentMapping("-m", ArgType.String) },
        { "model", new ArgumentMapping("-m", ArgType.String) },
        { "alias", new ArgumentMapping("--alias", ArgType.String) },
        { "apiKey", new ArgumentMapping("--api-key", ArgType.String) },
        { "api-key", new ArgumentMapping("--api-key", ArgType.String) },
        { "logFilePath", new ArgumentMapping("--log-file", ArgType.String) },
        { "log-file", new ArgumentMapping("--log-file", ArgType.String) },
        { "verboseLogging", new ArgumentMapping("--verbose", ArgType.Flag) },
        { "verbose", new ArgumentMapping("--verbose", ArgType.Flag) },
        { "enableWebUI", new ArgumentMapping("--ui", ArgType.Flag) },
        { "ui", new ArgumentMapping("--ui", ArgType.Flag) },
        { "enableSlots", new ArgumentMapping("--slots", ArgType.BoolOnOff) },
        { "slots", new ArgumentMapping("-np", ArgType.Int) },
        { "parallel", new ArgumentMapping("-np", ArgType.Int) },
        { "np", new ArgumentMapping("-np", ArgType.Int) },
        { "enableMetrics", new ArgumentMapping("--metrics", ArgType.Flag) },
        { "metrics", new ArgumentMapping("--metrics", ArgType.Flag) },
        { "reasoning", new ArgumentMapping("--reasoning", ArgType.Flag) },
        { "reasoningBudget", new ArgumentMapping("--reasoning-budget", ArgType.Int) },
        { "reasoning-budget", new ArgumentMapping("--reasoning-budget", ArgType.Int) },
        { "seed", new ArgumentMapping("--seed", ArgType.Int) },
        { "presencePenalty", new ArgumentMapping("--presence-penalty", ArgType.Float) },
        { "presence-penalty", new ArgumentMapping("--presence-penalty", ArgType.Float) },
        { "frequencyPenalty", new ArgumentMapping("--frequency-penalty", ArgType.Float) },
        { "frequency-penalty", new ArgumentMapping("--frequency-penalty", ArgType.Float) },
        { "maxTokens", new ArgumentMapping("--predict", ArgType.Int) },
        { "max-tokens", new ArgumentMapping("--predict", ArgType.Int) },
        { "cachePrompt", new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff) },
        { "cache-prompt", new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff) },
        { "contBatching", new ArgumentMapping("--cont-batching", ArgType.BoolOnOff) },
        { "cont-batching", new ArgumentMapping("--cont-batching", ArgType.BoolOnOff) },
        { "ngl", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpuLayers", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpu-layers", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpuSplitMode", new ArgumentMapping("--gpu-split", ArgType.Int) },
        { "gpu-split", new ArgumentMapping("--gpu-split", ArgType.Int) },
        { "tensorSplit", new ArgumentMapping("--tensor-split", ArgType.String) },
        { "tensor-split", new ArgumentMapping("--tensor-split", ArgType.String) },
        { "mainGpu", new ArgumentMapping("--main-gpu", ArgType.Int) },
        { "main-gpu", new ArgumentMapping("--main-gpu", ArgType.Int) },
        { "flashAttention", new ArgumentMapping("--flash-attn", ArgType.Flag) },
        { "flash-attn", new ArgumentMapping("--flash-attn", ArgType.Flag) },
        { "mmap", new ArgumentMapping("--mmap", ArgType.BoolOnOff) },
        { "mlock", new ArgumentMapping("--mlock", ArgType.BoolOnOff) },
         { "mmqEnabled", new ArgumentMapping("--mmq", ArgType.BoolOnOff) }, // deprecated in b9557, kept for profile compat
        { "no-mmq", new ArgumentMapping("--mmq", ArgType.BoolOnOff) },
        { "kvOffloadEnabled", new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff) },
        { "no-kv-offload", new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff) },
        { "c", new ArgumentMapping("-c", ArgType.Int) },
        { "contextSize", new ArgumentMapping("-c", ArgType.Int) },
        { "ctx-size", new ArgumentMapping("-c", ArgType.Int) },
        { "batch", new ArgumentMapping("--batch", ArgType.Int) },
        { "batchSize", new ArgumentMapping("--batch", ArgType.Int) },
        { "batch-size", new ArgumentMapping("--batch", ArgType.Int) },
        { "ubatch", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "ubatchSize", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "ubatch-size", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "cacheTypeK", new ArgumentMapping("--cache-type-k", ArgType.String) },
        { "cache-type-k", new ArgumentMapping("--cache-type-k", ArgType.String) },
        { "cacheTypeV", new ArgumentMapping("--cache-type-v", ArgType.String) },
        { "cache-type-v", new ArgumentMapping("--cache-type-v", ArgType.String) },
        { "threads", new ArgumentMapping("--threads", ArgType.Int) },
        { "threadsBatch", new ArgumentMapping("--threads-batch", ArgType.Int) },
        { "threads-batch", new ArgumentMapping("--threads-batch", ArgType.Int) },
        { "temperature", new ArgumentMapping("--temp", ArgType.Float) },
        { "temp", new ArgumentMapping("--temp", ArgType.Float) },
        { "topK", new ArgumentMapping("--top-k", ArgType.Int) },
        { "top-k", new ArgumentMapping("--top-k", ArgType.Int) },
        { "topP", new ArgumentMapping("--top-p", ArgType.Float) },
        { "top-p", new ArgumentMapping("--top-p", ArgType.Float) },
        { "minP", new ArgumentMapping("--min-p", ArgType.Float) },
        { "min-p", new ArgumentMapping("--min-p", ArgType.Float) },
        { "typicalP", new ArgumentMapping("--typical-p", ArgType.Float) },
        { "typical-p", new ArgumentMapping("--typical-p", ArgType.Float) },
        { "repeatPenalty", new ArgumentMapping("--repeat-penalty", ArgType.Float) },
        { "repeat-penalty", new ArgumentMapping("--repeat-penalty", ArgType.Float) },
        { "repeatLastN", new ArgumentMapping("--repeat-last-n", ArgType.Int) },
        { "repeat-last-n", new ArgumentMapping("--repeat-last-n", ArgType.Int) },
        { "mirostat", new ArgumentMapping("--mirostat", ArgType.Int) },
          { "mirostatLearnRate", new ArgumentMapping("--mirostat-lr", ArgType.Float) },
        { "mirostat-learn-rate", new ArgumentMapping("--mirostat-lr", ArgType.Float) },
        { "mirostatEta", new ArgumentMapping("--mirostat-lr", ArgType.Float) },
        { "mirostat-eta", new ArgumentMapping("--mirostat-lr", ArgType.Float) },
        { "mirostatTau", new ArgumentMapping("--mirostat-ent", ArgType.Float) },
        { "mirostat-tau", new ArgumentMapping("--mirostat-ent", ArgType.Float) },
        { "dryMultiplier", new ArgumentMapping("--dry-multiplier", ArgType.Float) },
        { "dry-multiplier", new ArgumentMapping("--dry-multiplier", ArgType.Float) },
        { "dryBase", new ArgumentMapping("--dry-base", ArgType.Float) },
        { "dry-base", new ArgumentMapping("--dry-base", ArgType.Float) },
        { "dynatempStddev", new ArgumentMapping("--dynatemp-range", ArgType.Float) },
        { "dynatemp-range", new ArgumentMapping("--dynatemp-range", ArgType.Float) },
        { "xtcProbability", new ArgumentMapping("--xtc-probability", ArgType.Float) },
        { "xtc-probability", new ArgumentMapping("--xtc-probability", ArgType.Float) },
        { "xtcThreshold", new ArgumentMapping("--xtc-threshold", ArgType.Float) },
        { "xtc-threshold", new ArgumentMapping("--xtc-threshold", ArgType.Float) },
        { "predictCount", new ArgumentMapping("--predict", ArgType.Int) },
        { "predict", new ArgumentMapping("--predict", ArgType.Int) },
        { "mtpBlocks", new ArgumentMapping("--mtp-blocks", ArgType.Int) },
        { "mtp-blocks", new ArgumentMapping("--mtp-blocks", ArgType.Int) },
        { "specDraftGpuLayers", new ArgumentMapping("--ngld", ArgType.String) },
        { "ngld", new ArgumentMapping("--ngld", ArgType.String) },
        { "specDraftNMax", new ArgumentMapping("--spec-draft-n-max", ArgType.Int) },
        { "spec-draft-n-max", new ArgumentMapping("--spec-draft-n-max", ArgType.Int) },
        { "specDraftNMin", new ArgumentMapping("--spec-draft-n-min", ArgType.Int) },
        { "spec-draft-n-min", new ArgumentMapping("--spec-draft-n-min", ArgType.Int) },
        { "specDraftPSplit", new ArgumentMapping("--spec-draft-p-split", ArgType.Float) },
        { "spec-draft-p-split", new ArgumentMapping("--spec-draft-p-split", ArgType.Float) },
        { "specDraftPMin", new ArgumentMapping("--spec-draft-p-min", ArgType.Float) },
        { "spec-draft-p-min", new ArgumentMapping("--spec-draft-p-min", ArgType.Float) },
         { "ropeFreqBase", new ArgumentMapping("--rope-freq-base", ArgType.Float) },
        { "rope-frequency-base", new ArgumentMapping("--rope-freq-base", ArgType.Float) },
        { "ropeFreqScale", new ArgumentMapping("--rope-freq-scale", ArgType.Float) },
        { "rope-frequency-scale", new ArgumentMapping("--rope-freq-scale", ArgType.Float) },
        { "yarnOriginalContext", new ArgumentMapping("--yarn-orig-ctx", ArgType.Int) },
        { "yarn-orig-ctx", new ArgumentMapping("--yarn-orig-ctx", ArgType.Int) },
        { "yarnExtFactor", new ArgumentMapping("--yarn-ext-factor", ArgType.Float) },
        { "yarn-ext-factor", new ArgumentMapping("--yarn-ext-factor", ArgType.Float) },
        { "yarnAttnFactor", new ArgumentMapping("--yarn-attn-factor", ArgType.Float) },
        { "yarn-attn-factor", new ArgumentMapping("--yarn-attn-factor", ArgType.Float) },
        { "yarnBetaFast", new ArgumentMapping("--yarn-beta-fast", ArgType.Float) },
        { "yarn-beta-fast", new ArgumentMapping("--yarn-beta-fast", ArgType.Float) },
        { "yarnBetaSlow", new ArgumentMapping("--yarn-beta-slow", ArgType.Float) },
        { "yarn-beta-slow", new ArgumentMapping("--yarn-beta-slow", ArgType.Float) },
        // --priority-high removed: handled by ServerManager, not a llama.cpp flag
        { "embeddingMode", new ArgumentMapping("--embedding", ArgType.Flag) },
        { "embedding", new ArgumentMapping("--embedding", ArgType.Flag) },
        { "poolingType", new ArgumentMapping("--pooling", ArgType.String) },
        { "pooling", new ArgumentMapping("--pooling", ArgType.String) },
        { "numa", new ArgumentMapping("--numa", ArgType.BoolOnOff) },
        { "speculativeDecoding", new ArgumentMapping("--speculative", ArgType.BoolOnOff) },
        { "speculative", new ArgumentMapping("--speculative", ArgType.BoolOnOff) },
        { "specType", new ArgumentMapping("--spec-type", ArgType.String) },
        { "spec-type", new ArgumentMapping("--spec-type", ArgType.String) },
        { "ropeScaling", new ArgumentMapping("--rope-scaling", ArgType.String) },
        { "rope-scaling", new ArgumentMapping("--rope-scaling", ArgType.String) },
        { "mmproj", new ArgumentMapping("--mmproj", ArgType.String) },
        { "mmprojPath", new ArgumentMapping("--mmproj", ArgType.String) },
        { "draftModel", new ArgumentMapping("-md", ArgType.String) },
        { "draft-model", new ArgumentMapping("-md", ArgType.String) },
        { "draftModelPath", new ArgumentMapping("-md", ArgType.String) },
    };

    /// <summary>
    /// Builds the command-line arguments string for llama-server, exactly matching
    /// the order and logic of LlamaManager.ServerManagerService.BuildArguments().
    /// Always args first (short flags), then conditional args, then Llama Studio extensions.
    /// </summary>
    /// <summary>
    /// Builds the full CLI arguments string — all parameters (for actual server launch).
    /// </summary>
    public string BuildFullArgsString()
    {
        return BuildReferenceArgsString();
    }

    static string F(float value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    static string D(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);

    string BuildReferenceArgsString()
    {
        var args = new List<string>();
        var (customArgs, customFlags) = CommandLineParser.ParseArguments(CustomArguments);

        static string Q(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

        bool IsOverridden(params string[] names)
        {
            foreach (var name in names)
            {
                if (customArgs.ContainsKey(name) || customFlags.Contains(name))
                    return true;
            }

            return false;
        }

        void AddValue(string flag, string value, params string[] aliases)
        {
            if (!IsOverridden(new[] { flag }.Concat(aliases).ToArray()))
                args.Add($"{flag} {value}");
        }

        void AddFlag(string flag, bool enabled, params string[] aliases)
        {
            if (enabled && !IsOverridden(new[] { flag }.Concat(aliases).ToArray()))
                args.Add(flag);
        }

        void AddBoolOnOff(string flag, bool? value, params string[] aliases)
        {
            if (value.HasValue && !IsOverridden(new[] { flag }.Concat(aliases).ToArray()))
                args.Add($"{flag} {(value.Value ? "on" : "off")}");
        }

        if (!string.IsNullOrWhiteSpace(ModelPath))
            AddValue("-m", Q(ModelPath), "--model");
        if (!string.IsNullOrWhiteSpace(HfRepo))
            AddValue("-hf", HfRepo, "--hf-repo", "-hfr");
        if (!string.IsNullOrWhiteSpace(HfFile))
            AddValue("-hff", HfFile, "--hf-file");
        if (HfOffline)
            AddFlag("--offline", true);
        if (!string.IsNullOrWhiteSpace(HfRepoDraft))
            AddValue("-hfd", HfRepoDraft, "--hf-repo-draft", "-hfrd");

        if (!string.IsNullOrWhiteSpace(Host))
            AddValue("--host", Host);
        AddValue("--port", Port.ToString(CultureInfo.InvariantCulture));

        AddValue("-c", ContextSize.ToString(CultureInfo.InvariantCulture), "--ctx-size");
        AddValue("-t", Threads.ToString(CultureInfo.InvariantCulture), "--threads");
        AddValue("-tb", ThreadsBatch.ToString(CultureInfo.InvariantCulture), "--threads-batch");
        if (!string.IsNullOrWhiteSpace(GpuLayers))
            AddValue("-ngl", GpuLayers, "--gpu-layers");
        if (GpuSplitMode != GpuSplitMode.None)
            AddValue("-sm", GpuSplitMode.ToString().ToLowerInvariant(), "--split-mode");
        if (TensorSplit.Length > 0)
            AddValue("-ts", string.Join(",", TensorSplit), "--tensor-split");
        AddValue("-mg", MainGpu.ToString(CultureInfo.InvariantCulture), "--main-gpu");

        AddBoolOnOff("-fa", FlashAttention, "--flash-attn");
        if (!Mmap)
            AddFlag("--no-mmap", true, "--mmap");
        AddFlag("--mlock", Mlock);
        // --no-mmq removed in b9557+, skip generation
        // if (!MmqEnabled) AddFlag("--no-mmq", true, "--no-mmq");
        if (!KvOffloadEnabled) AddFlag("-nkvo", true, "--no-kv-offload");

        AddValue("-b", BatchSize.ToString(CultureInfo.InvariantCulture), "--batch-size");
        AddValue("-ub", UbatchSize.ToString(CultureInfo.InvariantCulture), "--ubatch-size");
        AddValue("-ctk", CacheTypeK.ToString().ToLowerInvariant(), "--cache-type-k");
        AddValue("-ctv", CacheTypeV.ToString().ToLowerInvariant(), "--cache-type-v");
        if (CacheRam >= 0)
            AddValue("--cache-ram", CacheRam.ToString(CultureInfo.InvariantCulture));
        if (CacheReuse > 0)
            AddValue("--cache-reuse", CacheReuse.ToString(CultureInfo.InvariantCulture));

        AddValue("--temp", F(Temperature), "--temperature");
        AddValue("--top-k", TopK.ToString(CultureInfo.InvariantCulture));
        AddValue("--top-p", F(TopP));
        AddValue("--min-p", F(MinP));
        if (TypicalP > 0f)
            AddValue("--typical-p", F(TypicalP), "--typical");
        AddValue("--repeat-penalty", F(RepeatPenalty));
        AddValue("--repeat-last-n", RepeatLastN.ToString(CultureInfo.InvariantCulture));
        AddValue("--presence-penalty", F(PresencePenalty));
        AddValue("--frequency-penalty", F(FrequencyPenalty));

        if (Mirostat > MirostatMode.Disabled)
        {
            AddValue("--mirostat", ((int)Mirostat).ToString(CultureInfo.InvariantCulture));
            AddValue("--mirostat-ent", F(MirostatTau));
            AddValue("--mirostat-lr", F(MirostatEta));
        }

        if (DryMultiplier > 0f)
            AddValue("--dry-multiplier", F(DryMultiplier));
        AddValue("--dry-base", F(DryBase));
        if (DynatempStddev > 0f)
            AddValue("--dynatemp-range", F(DynatempStddev));
        if (XtcProbability > 0f)
        {
            AddValue("--xtc-probability", F(XtcProbability));
            AddValue("--xtc-threshold", F(XtcThreshold));
        }

        if (MaxTokens != 0)
            AddValue("-n", MaxTokens.ToString(CultureInfo.InvariantCulture), "--predict", "--n-predict");
        else if (PredictCount > 0)
            AddValue("-n", PredictCount.ToString(CultureInfo.InvariantCulture), "--predict", "--n-predict");

        if (!string.IsNullOrWhiteSpace(SpecType) && SpecType != "none")
            AddValue("--spec-type", SpecType);
        if (!string.IsNullOrWhiteSpace(DraftModelPath))
            AddValue("-md", Q(DraftModelPath), "--spec-draft-model", "--model-draft");
        if (!string.IsNullOrWhiteSpace(SpecDraftGpuLayers))
            AddValue("-ngld", SpecDraftGpuLayers, "--spec-draft-ngl", "--gpu-layers-draft", "--n-gpu-layers-draft");
        AddValue("--spec-draft-n-max", SpecDraftNMax.ToString(CultureInfo.InvariantCulture));
        AddValue("--spec-draft-n-min", SpecDraftNMin.ToString(CultureInfo.InvariantCulture));
        AddValue("--spec-draft-p-split", F(SpecDraftPSplit), "--draft-p-split");
        AddValue("--spec-draft-p-min", F(SpecDraftPMin), "--draft-p-min");

        if (!string.IsNullOrWhiteSpace(RopeScaling))
            AddValue("--rope-scaling", RopeScaling.ToLowerInvariant());
        if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0)
            AddValue("--rope-freq-base", D(RopeFreqBase.Value));
        if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0)
            AddValue("--rope-freq-scale", D(RopeFreqScale.Value));
        if (YarnOriginalContext.HasValue && YarnOriginalContext.Value != 0)
            AddValue("--yarn-orig-ctx", YarnOriginalContext.Value.ToString(CultureInfo.InvariantCulture));
        if (YarnExtFactor.HasValue && Math.Abs(YarnExtFactor.Value - -1.0) > 0.000001)
            AddValue("--yarn-ext-factor", D(YarnExtFactor.Value));
        if (YarnAttnFactor.HasValue && Math.Abs(YarnAttnFactor.Value - -1.0) > 0.000001)
            AddValue("--yarn-attn-factor", D(YarnAttnFactor.Value));
        if (YarnBetaFast.HasValue && Math.Abs(YarnBetaFast.Value - -1.0) > 0.000001)
            AddValue("--yarn-beta-fast", D(YarnBetaFast.Value));
        if (YarnBetaSlow.HasValue && Math.Abs(YarnBetaSlow.Value - -1.0) > 0.000001)
            AddValue("--yarn-beta-slow", D(YarnBetaSlow.Value));

        if (!CachePrompt)
            AddFlag("--no-cache-prompt", true, "--cache-prompt");
        if (ContBatching)
            AddFlag("-cb", true, "--cont-batching");
        else
            AddFlag("-nocb", true, "--no-cont-batching");
        if (ContextShift)
            AddFlag("--context-shift", true);
        else
            AddFlag("--no-context-shift", true);
        if (VerboseLogging) args.Add("-lv 4");
        if (EnableWebUI)
            AddFlag("--ui", true, "--webui");
        else
            AddFlag("--no-ui", true, "--no-webui");
        AddFlag("--metrics", EnableMetrics);
        AddBoolOnOff("-rea", Reasoning ? true : null, "--reasoning");
        if (Reasoning && ReasoningBudget > 0)
            AddValue("--reasoning-budget", ReasoningBudget.ToString(CultureInfo.InvariantCulture));
        AddFlag("--embedding", EmbeddingMode, "--embeddings");
        if (!string.IsNullOrWhiteSpace(PoolingType))
            AddValue("--pooling", PoolingType);

        // --priority-high is handled by ServerManager, not passed to llama-server
        AddFlag("--numa", Numa);
        if (!string.IsNullOrWhiteSpace(CpuAffinity))
            AddValue("-C", CpuAffinity, "--cpu-mask");

        if (Seed >= 0)
            AddValue("-s", Seed.ToString(CultureInfo.InvariantCulture), "--seed");
        AddValue("-to", Timeout.ToString(CultureInfo.InvariantCulture), "--timeout");
        if (Slots > 0)
            AddValue("-np", Slots.ToString(CultureInfo.InvariantCulture), "--parallel");
        if (!EnableSlots)
            AddFlag("--no-slots", true, "--slots");
        if (!string.IsNullOrWhiteSpace(ApiKey))
            AddValue("--api-key", Q(ApiKey));
        if (!string.IsNullOrWhiteSpace(Alias))
            AddValue("-a", Q(Alias), "--alias");
        if (!string.IsNullOrWhiteSpace(LogFilePath))
            AddValue("--log-file", Q(LogFilePath));
        if (!string.IsNullOrWhiteSpace(MmprojPath))
            AddValue("-mm", Q(MmprojPath), "--mmproj");

        AddRemainingCustomArgs(args, customArgs, customFlags);
        return string.Join(" ", args);
    }

    string BuildLegacyFullArgsString()
    {
        var args = new List<string>();

        // Parse custom arguments into canonical names
        var (customArgs, customFlags) = CommandLineParser.ParseArguments(CustomArguments);

        // Helper: add UI value if not overridden by custom args
        void AddIfNotOverridden(string canonical, string value)
        {
            if (!customArgs.ContainsKey(canonical))
                args.Add(value);
        }

        // Helper: add bool on/off if not overridden
        void AddBoolOnOff(string canonical, string flag, bool value)
        {
            if (!customArgs.ContainsKey(canonical) && !customFlags.Contains(canonical))
                args.Add(value ? flag : $"{flag}=off");
        }

        // Helper: add bool flag if not overridden
        void AddBoolFlag(string canonical, string flag, bool value)
        {
            if (!customArgs.ContainsKey(canonical) && !customFlags.Contains(canonical))
                if (value) args.Add(flag);
        }

        // === ALWAYS ARGS — exact order from LlamaManager.BuildArguments() ===

        // 1. -m (short flag as in reference)
        AddIfNotOverridden("model", $"-m \"{ModelPath}\"");

        // 2. --port
        AddIfNotOverridden("port", $"--port {Port}");

        // 3. --threads (always added, like reference)
        AddIfNotOverridden("threads", $"--threads {Threads}");

        // 4. --threads-batch (reference uses same Threads value; Llama Studio keeps separate for flexibility)
        AddIfNotOverridden("threadsBatch", $"--threads-batch {(ThreadsBatch > 0 ? ThreadsBatch : Threads)}");

        // 5. -ngl (short flag as in reference)
        if (!string.IsNullOrWhiteSpace(GpuLayers))
            AddIfNotOverridden("gpuLayers", $"-ngl {GpuLayers}");

        // 6. -c (short flag as in reference)
        AddIfNotOverridden("contextSize", $"-c {ContextSize}");

        // 7. --batch (reference uses short form)
        AddIfNotOverridden("batchSize", $"--batch {BatchSize}");

        // 8. --ubatch (reference uses BatchSize value; Llama Studio keeps separate for flexibility)
        AddIfNotOverridden("ubatchSize", $"--ubatch {(UbatchSize > 0 ? UbatchSize : BatchSize)}");

        // 9. --temp (always added, like reference)
        AddIfNotOverridden("temperature", $"--temp {F(Temperature)}");

        // 10. --top-k (always added, like reference)
        AddIfNotOverridden("topK", $"--top-k {TopK}");

        // 11. --top-p (always added, like reference)
        AddIfNotOverridden("topP", $"--top-p {F(TopP)}");

        // 12. --min-p (always added, like reference)
        AddIfNotOverridden("minP", $"--min-p {F(MinP)}");

        // 13. -fa (FlashAttention: on/off)
        if (!customArgs.ContainsKey("flashAttention") && !customFlags.Contains("flashAttention"))
        {
            args.Add("-fa");
            args.Add(FlashAttention ? "on" : "off");
        }

        // === CONDITIONAL ARGS — exact order from LlamaManager.BuildArguments() ===

        // 14. --host (conditional: not empty)
        if (!string.IsNullOrEmpty(Host))
            AddIfNotOverridden("host", $"--host {Host}");

        // 15-17. --mirostat, --mirostat-lr, --mirostat-ent (enum-based, like reference)
        if (Mirostat > MirostatMode.Disabled)
        {
            AddIfNotOverridden("mirostat", $"--mirostat {(int)Mirostat}");
            AddIfNotOverridden("mirostatLearnRate", $"--mirostat-lr {F(MirostatEta)}");
            AddIfNotOverridden("mirostatTau", $"--mirostat-ent {F(MirostatTau)}");
        }

        // 18. --spec-type (MTP: ngram, draft-mtp, draft)
        if (!string.IsNullOrWhiteSpace(SpecType) && SpecType != "none")
        {
            if (!customArgs.ContainsKey("specType") && !customFlags.Contains("specType"))
            {
                args.Add("--spec-type");
                args.Add(SpecType);
            }
        }

        // 19. --rope-scaling (conditional: not empty)
        if (!string.IsNullOrWhiteSpace(RopeScaling))
            AddIfNotOverridden("ropeScaling", $"--rope-scaling {RopeScaling.ToLowerInvariant()}");

        // 20. --rope-freq-base (conditional: > 0, like reference)
        if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0)
            AddIfNotOverridden("ropeFreqBase", $"--rope-freq-base {D(RopeFreqBase.Value)}");

        // 21. --rope-freq-scale (conditional: > 0, like reference)
        if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0)
            AddIfNotOverridden("ropeFreqScale", $"--rope-freq-scale {D(RopeFreqScale.Value)}");

        // 22. --embedding (simple flag when true — like reference)
        AddBoolFlag("embeddingMode", "--embedding", EmbeddingMode);

        // 23. --priority-high — NOT a llama.cpp flag, handled by ServerManager separately
        // (removed from CLI args)

        // === HERMASS EXTENSIONS — parameters not in LlamaManager reference ===

        // --timeout
        AddIfNotOverridden("timeout", $"--timeout {Timeout}");

        // --alias
        if (!string.IsNullOrWhiteSpace(Alias))
            AddIfNotOverridden("alias", $"--alias \"{Alias}\"");

        // --api-key
        if (!string.IsNullOrWhiteSpace(ApiKey))
            AddIfNotOverridden("apiKey", $"--api-key \"{ApiKey}\"");

        // --log-file
        if (!string.IsNullOrWhiteSpace(LogFilePath))
            AddIfNotOverridden("logFilePath", $"--log-file \"{LogFilePath}\"");

        // --verbose
        AddBoolFlag("verboseLogging", "--verbose", VerboseLogging);

        // --ui (Web UI)
        if (!customArgs.ContainsKey("enableWebUI") && !customFlags.Contains("enableWebUI"))
        {
            if (EnableWebUI) args.Add("--ui");
            else args.Add("--no-ui");
        }

        // --slots (мониторинг слотов)
        if (!customArgs.ContainsKey("enableSlots") && !customFlags.Contains("enableSlots"))
        {
            if (EnableSlots) args.Add("--slots");
            else args.Add("--no-slots");
        }

        // -np (количество параллельных слотов)
        if (Slots > 0)
            AddIfNotOverridden("slots", $"-np {Slots}");

        // --metrics
        AddBoolFlag("enableMetrics", "--metrics", EnableMetrics);

        // -rea (Reasoning: on/off)
        if (!customArgs.ContainsKey("reasoning") && !customFlags.Contains("reasoning"))
        {
            args.Add("-rea");
            args.Add(Reasoning ? "on" : "off");
        }

        // --reasoning-budget
        if (Reasoning && ReasoningBudget > 0)
            AddIfNotOverridden("reasoningBudget", $"--reasoning-budget {ReasoningBudget}");

        // --seed
        if (Seed >= 0)
            AddIfNotOverridden("seed", $"--seed {Seed}");

        // --presence-penalty
        if (PresencePenalty != 0f)
            AddIfNotOverridden("presencePenalty", $"--presence-penalty {F(PresencePenalty)}");

        // --frequency-penalty
        if (FrequencyPenalty != 0f)
            AddIfNotOverridden("frequencyPenalty", $"--frequency-penalty {F(FrequencyPenalty)}");

        // --predict (was --max-tokens, renamed in b9557+)
        if (MaxTokens > 0)
            AddIfNotOverridden("maxTokens", $"-n {MaxTokens}");

        // --cache-prompt / --no-cache-prompt
        if (!customArgs.ContainsKey("cachePrompt") && !customFlags.Contains("cachePrompt"))
        {
            if (CachePrompt) args.Add("--cache-prompt");
            else args.Add("--no-cache-prompt");
        }

        // --cont-batching / -nocb
        if (!customArgs.ContainsKey("contBatching") && !customFlags.Contains("contBatching"))
        {
            if (ContBatching) args.Add("--cont-batching");
            else args.Add("-nocb");
        }

        // --gpu-split
        if (GpuSplitMode != GpuSplitMode.None)
            AddIfNotOverridden("gpuSplitMode", $"--gpu-split {(int)GpuSplitMode}");

        // --tensor-split
        if (TensorSplit.Length > 0)
            AddIfNotOverridden("tensorSplit", $"--tensor-split {string.Join(",", TensorSplit)}");

        // --main-gpu
        if (MainGpu >= 0)
            AddIfNotOverridden("mainGpu", $"--main-gpu {MainGpu}");

        // --mmap / --no-mmap
        if (!customArgs.ContainsKey("mmap") && !customFlags.Contains("mmap"))
        {
            if (Mmap) args.Add("--mmap");
            else args.Add("--no-mmap");
        }

        // --mlock
        AddBoolFlag("mlock", "--mlock", Mlock);

        // --no-mmq removed in b9557+, skip
        // if (!MmqEnabled) args.Add("--no-mmq");

        // -nkvo (no-kv-offload)
        if (!KvOffloadEnabled)
            args.Add("-nkvo");

        // --cache-type-k
        if (CacheTypeK != 0)
            AddIfNotOverridden("cacheTypeK", $"--cache-type-k {CacheTypeK.ToString().ToLowerInvariant()}");

        // --cache-type-v
        if (CacheTypeV != 0)
            AddIfNotOverridden("cacheTypeV", $"--cache-type-v {CacheTypeV.ToString().ToLowerInvariant()}");

        // --typical-p
        if (TypicalP > 0f)
            AddIfNotOverridden("typicalP", $"--typical-p {TypicalP}");

        // --repeat-penalty
        if (RepeatPenalty > 0f)
            AddIfNotOverridden("repeatPenalty", $"--repeat-penalty {F(RepeatPenalty)}");

        // --repeat-last-n
        if (RepeatLastN > 0)
            AddIfNotOverridden("repeatLastN", $"--repeat-last-n {RepeatLastN}");

        // --dry-multiplier
        if (DryMultiplier > 0f)
            AddIfNotOverridden("dryMultiplier", $"--dry-multiplier {F(DryMultiplier)}");

        // --dry-base
        if (DryBase > 0f)
            AddIfNotOverridden("dryBase", $"--dry-base {F(DryBase)}");

        // --dynatemp-range
        if (DynatempStddev > 0f)
            AddIfNotOverridden("dynatempStddev", $"--dynatemp-range {F(DynatempStddev)}");

        // --xtc-probability
        if (XtcProbability > 0f)
            AddIfNotOverridden("xtcProbability", $"--xtc-probability {F(XtcProbability)}");

        // --xtc-threshold
        if (XtcThreshold > 0f)
            AddIfNotOverridden("xtcThreshold", $"--xtc-threshold {F(XtcThreshold)}");

        // --predict
        if (PredictCount > 0)
            AddIfNotOverridden("predictCount", $"--predict {PredictCount}");

        // Speculative draft params
        if (!string.IsNullOrWhiteSpace(SpecDraftGpuLayers))
            AddIfNotOverridden("specDraftGpuLayers", $"--ngld {SpecDraftGpuLayers}");

        if (SpecDraftNMax > 0)
            AddIfNotOverridden("specDraftNMax", $"--spec-draft-n-max {SpecDraftNMax}");

        if (SpecDraftNMin > 0)
            AddIfNotOverridden("specDraftNMin", $"--spec-draft-n-min {SpecDraftNMin}");

        if (SpecDraftPSplit > 0f)
            AddIfNotOverridden("specDraftPSplit", $"--spec-draft-p-split {F(SpecDraftPSplit)}");

        if (SpecDraftPMin > 0f)
            AddIfNotOverridden("specDraftPMin", $"--spec-draft-p-min {F(SpecDraftPMin)}");

        // YARN params
        if (YarnOriginalContext.HasValue)
            AddIfNotOverridden("yarnOriginalContext", $"--yarn-orig-ctx {YarnOriginalContext.Value}");

        if (YarnExtFactor.HasValue)
            AddIfNotOverridden("yarnExtFactor", $"--yarn-ext-factor {D(YarnExtFactor.Value)}");

        if (YarnAttnFactor.HasValue)
            AddIfNotOverridden("yarnAttnFactor", $"--yarn-attn-factor {D(YarnAttnFactor.Value)}");

        if (YarnBetaFast.HasValue)
            AddIfNotOverridden("yarnBetaFast", $"--yarn-beta-fast {D(YarnBetaFast.Value)}");

        if (YarnBetaSlow.HasValue)
            AddIfNotOverridden("yarnBetaSlow", $"--yarn-beta-slow {D(YarnBetaSlow.Value)}");

        // --pooling
        if (!string.IsNullOrWhiteSpace(PoolingType))
            AddIfNotOverridden("poolingType", $"--pooling {PoolingType}");

        // --numa
        if (Numa)
            AddBoolOnOff("numa", "--numa", Numa);

        // --speculative (draft model decoding)
        if (SpeculativeDecoding)
        {
            if (!customArgs.ContainsKey("speculativeDecoding") && !customFlags.Contains("speculativeDecoding"))
            {
                args.Add("--speculative");
                args.Add("on");
            }
        }

        // --mmproj
        if (!string.IsNullOrWhiteSpace(MmprojPath))
            AddIfNotOverridden("mmproj", $"--mmproj \"{MmprojPath}\"");

        // -md (was --draft-model, renamed in b9557+)
        if (!string.IsNullOrWhiteSpace(DraftModelPath))
            AddIfNotOverridden("draftModel", $"-md \"{DraftModelPath}\"");

        // Remaining custom arguments (not overriding any known arg)
        AddRemainingCustomArgs(args, customArgs, customFlags);

        return string.Join(" ", args);
    }

    /// <summary>
    /// Builds a compact preview of CLI arguments — includes parameters from ALL tabs.
    /// </summary>
    public string BuildArgsString()
    {
        var args = new List<string>();

        // === Model ===
        if (!string.IsNullOrWhiteSpace(ModelPath))
            args.Add($"-m \"{ModelPath}\"");

        if (!string.IsNullOrWhiteSpace(MmprojPath))
            args.Add($"--mmproj \"{MmprojPath}\"");

        if (!string.IsNullOrWhiteSpace(DraftModelPath))
            args.Add($"-md \"{DraftModelPath}\"");

        // === GPU ===
        if (!string.IsNullOrWhiteSpace(GpuLayers))
            args.Add($"-ngl {GpuLayers}");
        args.Add($"--threads {Threads}");
        if (ThreadsBatch > 0) args.Add($"--threads-batch {ThreadsBatch}");
        if (MainGpu > 0) args.Add($"--main-gpu {MainGpu}");
        args.Add($"-fa {(FlashAttention ? "on" : "off")}");
        args.Add(Mmap ? "--mmap" : "--no-mmap");
        if (Mlock) args.Add("--mlock");
        // --no-mmq removed in b9557+, skip
        // if (!MmqEnabled) args.Add("--no-mmq");
        if (!KvOffloadEnabled) args.Add("-nkvo");

        // === Context & Batch ===
        args.Add($"-c {ContextSize}");
        args.Add($"--batch {BatchSize}");
        args.Add($"--ubatch {(UbatchSize > 0 ? UbatchSize : BatchSize)}");
        if (MaxTokens > 0) args.Add($"-n {MaxTokens}"); // was --max-tokens, renamed in b9557+

        // === Cache Type ===
        if (CacheTypeK != 0) args.Add($"--cache-type-k {CacheTypeK.ToString().ToLowerInvariant()}");
        if (CacheTypeV != 0) args.Add($"--cache-type-v {CacheTypeV.ToString().ToLowerInvariant()}");

        // === Cache Management ===
        if (CacheRam >= 0) args.Add($"--cache-ram {CacheRam}");
        if (CacheReuse != 32) args.Add($"--cache-reuse {CacheReuse}");

        // === Sampling ===
        args.Add($"--temp {F(Temperature)}");
        args.Add($"--top-k {TopK}");
        args.Add($"--top-p {F(TopP)}");
        args.Add($"--min-p {F(MinP)}");
        if (TypicalP != 1.0f) args.Add($"--typical-p {F(TypicalP)}");
        if (Math.Abs(RepeatPenalty - DefaultRepeatPenalty) > 0.000001f) args.Add($"--repeat-penalty {F(RepeatPenalty)}");
        if (RepeatLastN != DefaultRepeatLastN) args.Add($"--repeat-last-n {RepeatLastN}");
        if (PresencePenalty != 0f) args.Add($"--presence-penalty {F(PresencePenalty)}");
        if (FrequencyPenalty != 0f) args.Add($"--frequency-penalty {F(FrequencyPenalty)}");
        if (Seed >= 0) args.Add($"--seed {Seed}");

        // === Mirostat ===
        if (Mirostat > MirostatMode.Disabled)
        {
            args.Add($"--mirostat {(int)Mirostat}");
            args.Add($"--mirostat-lr {F(MirostatEta)}");
            args.Add($"--mirostat-ent {F(MirostatTau)}");
        }

        // === DRY ===
        if (DryMultiplier > 0f) args.Add($"--dry-multiplier {F(DryMultiplier)}");
        if (DryBase > 0f) args.Add($"--dry-base {F(DryBase)}");

        // === Dynatemp ===
        if (DynatempStddev > 0f) args.Add($"--dynatemp-range {F(DynatempStddev)}");

        // === XTC ===
        if (XtcProbability > 0f) args.Add($"--xtc-probability {F(XtcProbability)}");
        if (XtcThreshold > 0f) args.Add($"--xtc-threshold {F(XtcThreshold)}");

        // === MTP / Speculative ===
        if (!string.IsNullOrWhiteSpace(SpecType) && SpecType != "none")
            args.Add($"--spec-type {SpecType}");
        if (PredictCount > 0) args.Add($"--predict {PredictCount}");
        if (SpeculativeDecoding) args.Add("--speculative on");
        if (!string.IsNullOrWhiteSpace(SpecDraftGpuLayers)) args.Add($"--ngld {SpecDraftGpuLayers}");
        if (SpecDraftNMax > 0) args.Add($"--spec-draft-n-max {SpecDraftNMax}");
        if (SpecDraftNMin > 0) args.Add($"--spec-draft-n-min {SpecDraftNMin}");
        if (SpecDraftPSplit > 0f) args.Add($"--spec-draft-p-split {F(SpecDraftPSplit)}");
        if (SpecDraftPMin > 0f) args.Add($"--spec-draft-p-min {F(SpecDraftPMin)}");

        // === Rope / YARN ===
        if (!string.IsNullOrWhiteSpace(RopeScaling))
            args.Add($"--rope-scaling {RopeScaling.ToLowerInvariant()}");
        if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0)
            args.Add($"--rope-freq-base {D(RopeFreqBase.Value)}"); // was --rope-frequency-base
        if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0)
            args.Add($"--rope-freq-scale {D(RopeFreqScale.Value)}"); // was --rope-frequency-scale
        if (YarnOriginalContext.HasValue)
            args.Add($"--yarn-orig-ctx {YarnOriginalContext.Value}");

        // === Advanced ===
        if (Numa) args.Add("--numa");
        if (!CachePrompt) args.Add("--no-cache-prompt");
        if (!ContBatching) args.Add("-nocb");
        if (VerboseLogging) args.Add("--verbose");
        if (EnableWebUI) args.Add("--ui");
        if (EnableSlots) args.Add("--slots");
        if (EnableMetrics) args.Add("--metrics");
        if (Reasoning) args.Add("-rea on");
        if (Reasoning && ReasoningBudget > 0) args.Add($"--reasoning-budget {ReasoningBudget}");
        if (EmbeddingMode) args.Add("--embedding");
        // --priority-high is handled by ServerManager, not passed to llama-server

        // === Connection ===
        if (!string.IsNullOrEmpty(Host) && Host != "127.0.0.1")
            args.Add($"--host {Host}");
        args.Add($"--port {Port}");
        args.Add($"--timeout {Timeout}");
        if (Slots > 0) args.Add($"-np {Slots}");

        return string.Join(" ", args);
    }

    private void AddRemainingCustomArgs(List<string> args, Dictionary<string, string> customArgs, HashSet<string> customFlags)
    {
        var knownCanonical = KnownArguments.Values.Select(m => m.Canonical).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in customArgs)
        {
            if (!knownCanonical.Contains(key))
            {
                var mapping = KnownArguments.GetValueOrDefault(key);
                if (mapping != null)
                {
                    args.Add(mapping.ArgType == ArgType.Flag ? mapping.Canonical : $"{mapping.Canonical} {value}");
                }
                else
                {
                    args.Add($"{CommandLineParser.NormalizeSpecialCharacters(key)} {value}");
                }
            }
        }

        foreach (var key in customFlags)
        {
            if (!knownCanonical.Contains(key))
            {
                var mapping = KnownArguments.GetValueOrDefault(key);
                if (mapping != null)
                {
                    args.Add(mapping.Canonical);
                }
                else
                {
                    args.Add(CommandLineParser.NormalizeSpecialCharacters(key));
                }
            }
        }
    }

    public override string ToString() => Name;

    public override bool Equals(object? obj) => obj is ServerProfile other && Id == other.Id;
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
}

/// <summary>
/// Maps a known argument alias to its canonical CLI flag and type.
/// </summary>
public class ArgumentMapping
{
    public string Canonical { get; init; } = string.Empty;
    public ArgType ArgType { get; init; }

    public ArgumentMapping(string canonical, ArgType argType)
    {
        Canonical = canonical;
        ArgType = argType;
    }
}

/// <summary>
/// Argument type for CLI generation.
/// </summary>
public enum ArgType
{
    String,
    Int,
    Float,
    Flag,
    BoolOnOff,
}

/// <summary>
/// Static utilities for parsing custom arguments into canonical names.
/// Mirrors LlamaServerLauncherAvalonia CommandLineParser.
/// </summary>
public static class CommandLineParser
{
    /// <summary>
    /// Normalizes argument name: camelCase/pascalCase → kebab-case.
    /// "gpuLayers" → "gpu-layers", "specDraftNMax" → "spec-draft-n-max"
    /// </summary>
    public static string NormalizeSpecialCharacters(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else if (c == '_')
            {
                result.Append('-');
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Parses a dictionary of custom arguments into canonical names and flags.
    /// Returns (customArgs, customFlags) where customArgs has values and customFlags are toggles.
    /// </summary>
    public static (Dictionary<string, string> CustomArgs, HashSet<string> CustomFlags) ParseArguments(Dictionary<string, string> customArguments)
    {
        var customArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var customFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (customArguments == null || customArguments.Count == 0)
            return (customArgs, customFlags);

        foreach (var (key, value) in customArguments)
        {
            var normalized = NormalizeSpecialCharacters(key);
            var canonical = GetCanonicalName(normalized);

            if (string.IsNullOrWhiteSpace(value))
            {
                customFlags.Add(canonical);
            }
            else
            {
                customArgs[canonical] = value;
            }
        }

        return (customArgs, customFlags);
    }

    /// <summary>
    /// Gets the canonical name for a normalized argument key.
    /// </summary>
    private static string GetCanonicalName(string normalized)
    {
        if (KnownArgumentsLookup.TryGetValue(normalized, out var mapping))
            return mapping.Canonical;

        return $"--{normalized}";
    }

    /// <summary>
    /// Gets argument values for a canonical name from custom arguments.
    /// </summary>
    public static List<string> GetArgumentValues(Dictionary<string, string> customArguments, string canonicalName)
    {
        var values = new List<string>();
        if (customArguments == null) return values;

        foreach (var (key, value) in customArguments)
        {
            var normalized = NormalizeSpecialCharacters(key);
            var mapping = KnownArgumentsLookup.GetValueOrDefault(normalized);
            if (mapping != null && string.Equals(mapping.Canonical, canonicalName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }
        }

        return values;
    }

    /// <summary>
    /// Checks if a canonical flag is present in custom arguments.
    /// </summary>
    public static bool GetArgumentFlags(Dictionary<string, string> customArguments, string canonicalName)
    {
        if (customArguments == null) return false;

        foreach (var key in customArguments.Keys)
        {
            var normalized = NormalizeSpecialCharacters(key);
            var mapping = KnownArgumentsLookup.GetValueOrDefault(normalized);
            if (mapping != null && string.Equals(mapping.Canonical, canonicalName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static readonly Dictionary<string, ArgumentMapping> KnownArgumentsLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "host", new ArgumentMapping("--host", ArgType.String) },
        { "port", new ArgumentMapping("--port", ArgType.Int) },
        { "timeout", new ArgumentMapping("--timeout", ArgType.Int) },
        { "m", new ArgumentMapping("-m", ArgType.String) },
        { "model", new ArgumentMapping("-m", ArgType.String) },
        { "alias", new ArgumentMapping("--alias", ArgType.String) },
        { "apikey", new ArgumentMapping("--api-key", ArgType.String) },
        { "api-key", new ArgumentMapping("--api-key", ArgType.String) },
        { "logfilepath", new ArgumentMapping("--log-file", ArgType.String) },
        { "log-file", new ArgumentMapping("--log-file", ArgType.String) },
        { "verboselogging", new ArgumentMapping("--verbose", ArgType.Flag) },
        { "verbose", new ArgumentMapping("--verbose", ArgType.Flag) },
        { "enablewebui", new ArgumentMapping("--ui", ArgType.Flag) },
        { "ui", new ArgumentMapping("--ui", ArgType.Flag) },
        { "enableslots", new ArgumentMapping("--slots", ArgType.BoolOnOff) },
        { "slots", new ArgumentMapping("-np", ArgType.Int) },
        { "parallel", new ArgumentMapping("-np", ArgType.Int) },
        { "np", new ArgumentMapping("-np", ArgType.Int) },
        { "enablemetrics", new ArgumentMapping("--metrics", ArgType.Flag) },
        { "metrics", new ArgumentMapping("--metrics", ArgType.Flag) },
        { "reasoning", new ArgumentMapping("--reasoning", ArgType.Flag) },
        { "reasoningbudget", new ArgumentMapping("--reasoning-budget", ArgType.Int) },
        { "reasoning-budget", new ArgumentMapping("--reasoning-budget", ArgType.Int) },
        { "seed", new ArgumentMapping("--seed", ArgType.Int) },
        { "presencepenalty", new ArgumentMapping("--presence-penalty", ArgType.Float) },
        { "presence-penalty", new ArgumentMapping("--presence-penalty", ArgType.Float) },
        { "frequencypenalty", new ArgumentMapping("--frequency-penalty", ArgType.Float) },
        { "frequency-penalty", new ArgumentMapping("--frequency-penalty", ArgType.Float) },
        { "maxtokens", new ArgumentMapping("--predict", ArgType.Int) },
        { "max-tokens", new ArgumentMapping("--predict", ArgType.Int) },
        { "cacheprompt", new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff) },
        { "cache-prompt", new ArgumentMapping("--cache-prompt", ArgType.BoolOnOff) },
        { "contbatching", new ArgumentMapping("--cont-batching", ArgType.BoolOnOff) },
        { "cont-batching", new ArgumentMapping("--cont-batching", ArgType.BoolOnOff) },
        { "ngld", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpulayers", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpu-layers", new ArgumentMapping("-ngl", ArgType.Int) },
        { "gpusplitmode", new ArgumentMapping("--gpu-split", ArgType.Int) },
        { "gpu-split", new ArgumentMapping("--gpu-split", ArgType.Int) },
        { "tensorsplit", new ArgumentMapping("--tensor-split", ArgType.String) },
        { "tensor-split", new ArgumentMapping("--tensor-split", ArgType.String) },
        { "maingpu", new ArgumentMapping("--main-gpu", ArgType.Int) },
        { "main-gpu", new ArgumentMapping("--main-gpu", ArgType.Int) },
        { "flashattention", new ArgumentMapping("-fa", ArgType.String) },
        { "flash-attn", new ArgumentMapping("-fa", ArgType.String) },
        { "fa", new ArgumentMapping("-fa", ArgType.String) },
        { "mmap", new ArgumentMapping("--mmap", ArgType.BoolOnOff) },
        { "mlock", new ArgumentMapping("--mlock", ArgType.BoolOnOff) },
        { "nommq", new ArgumentMapping("--mmq", ArgType.BoolOnOff) }, // deprecated
        { "no-mmq", new ArgumentMapping("--mmq", ArgType.BoolOnOff) },
        { "nokvoffload", new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff) },
        { "no-kv-offload", new ArgumentMapping("--no-kv-offload", ArgType.BoolOnOff) },
        { "c", new ArgumentMapping("-c", ArgType.Int) },
        { "contextsize", new ArgumentMapping("-c", ArgType.Int) },
        { "ctx-size", new ArgumentMapping("-c", ArgType.Int) },
        { "batch", new ArgumentMapping("--batch", ArgType.Int) },
        { "batchsize", new ArgumentMapping("--batch", ArgType.Int) },
        { "batch-size", new ArgumentMapping("--batch", ArgType.Int) },
        { "ubatch", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "ubatchsize", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "ubatch-size", new ArgumentMapping("--ubatch", ArgType.Int) },
        { "cachetypek", new ArgumentMapping("--cache-type-k", ArgType.String) },
        { "cache-type-k", new ArgumentMapping("--cache-type-k", ArgType.String) },
        { "cachetypev", new ArgumentMapping("--cache-type-v", ArgType.String) },
        { "cache-type-v", new ArgumentMapping("--cache-type-v", ArgType.String) },
        { "threads", new ArgumentMapping("--threads", ArgType.Int) },
        { "threadsbatch", new ArgumentMapping("--threads-batch", ArgType.Int) },
        { "threads-batch", new ArgumentMapping("--threads-batch", ArgType.Int) },
        { "temperature", new ArgumentMapping("--temp", ArgType.Float) },
        { "temp", new ArgumentMapping("--temp", ArgType.Float) },
        { "topk", new ArgumentMapping("--top-k", ArgType.Int) },
        { "top-k", new ArgumentMapping("--top-k", ArgType.Int) },
        { "topp", new ArgumentMapping("--top-p", ArgType.Float) },
        { "top-p", new ArgumentMapping("--top-p", ArgType.Float) },
        { "minp", new ArgumentMapping("--min-p", ArgType.Float) },
        { "min-p", new ArgumentMapping("--min-p", ArgType.Float) },
        { "typicalp", new ArgumentMapping("--typical-p", ArgType.Float) },
        { "typical-p", new ArgumentMapping("--typical-p", ArgType.Float) },
        { "repeatpenalty", new ArgumentMapping("--repeat-penalty", ArgType.Float) },
        { "repeat-penalty", new ArgumentMapping("--repeat-penalty", ArgType.Float) },
        { "repeatlastn", new ArgumentMapping("--repeat-last-n", ArgType.Int) },
        { "repeat-last-n", new ArgumentMapping("--repeat-last-n", ArgType.Int) },
        { "mirostat", new ArgumentMapping("--mirostat", ArgType.Int) },
        { "mirostatternrate", new ArgumentMapping("-mlr", ArgType.Float) },
        { "mirostat-learn-rate", new ArgumentMapping("-mlr", ArgType.Float) },
        { "mirostateeta", new ArgumentMapping("-mlr", ArgType.Float) },
        { "mirostat-eta", new ArgumentMapping("-mlr", ArgType.Float) },
        { "mirostattau", new ArgumentMapping("-mt", ArgType.Float) },
        { "mirostat-tau", new ArgumentMapping("-mt", ArgType.Float) },
        { "drymultiplier", new ArgumentMapping("--dry-multiplier", ArgType.Float) },
        { "dry-multiplier", new ArgumentMapping("--dry-multiplier", ArgType.Float) },
        { "drybase", new ArgumentMapping("--dry-base", ArgType.Float) },
        { "dry-base", new ArgumentMapping("--dry-base", ArgType.Float) },
        { "dynatempstddev", new ArgumentMapping("--dynatemp-range", ArgType.Float) },
        { "dynatemp-range", new ArgumentMapping("--dynatemp-range", ArgType.Float) },
        { "xtcprobability", new ArgumentMapping("--xtc-probability", ArgType.Float) },
        { "xtc-probability", new ArgumentMapping("--xtc-probability", ArgType.Float) },
        { "xtcthreshold", new ArgumentMapping("--xtc-threshold", ArgType.Float) },
        { "xtc-threshold", new ArgumentMapping("--xtc-threshold", ArgType.Float) },
        { "predictcount", new ArgumentMapping("--predict", ArgType.Int) },
        { "predict", new ArgumentMapping("--predict", ArgType.Int) },
        { "specdraftgpulayers", new ArgumentMapping("--ngld", ArgType.String) },
        { "ngld", new ArgumentMapping("--ngld", ArgType.String) },
        { "specdraftnmax", new ArgumentMapping("--spec-draft-n-max", ArgType.Int) },
        { "spec-draft-n-max", new ArgumentMapping("--spec-draft-n-max", ArgType.Int) },
        { "specdraftnmin", new ArgumentMapping("--spec-draft-n-min", ArgType.Int) },
        { "spec-draft-n-min", new ArgumentMapping("--spec-draft-n-min", ArgType.Int) },
        { "specdraftpsplit", new ArgumentMapping("--spec-draft-p-split", ArgType.Float) },
        { "spec-draft-p-split", new ArgumentMapping("--spec-draft-p-split", ArgType.Float) },
        { "specdraftpmin", new ArgumentMapping("--spec-draft-p-min", ArgType.Float) },
        { "spec-draft-p-min", new ArgumentMapping("--spec-draft-p-min", ArgType.Float) },
        { "ropefreqbase", new ArgumentMapping("--rope-freq-base", ArgType.Float) },
        { "rope-frequency-base", new ArgumentMapping("--rope-freq-base", ArgType.Float) },
        { "ropefreqscale", new ArgumentMapping("--rope-freq-scale", ArgType.Float) },
        { "rope-frequency-scale", new ArgumentMapping("--rope-freq-scale", ArgType.Float) },
        { "yarnoriginalcontext", new ArgumentMapping("--yarn-orig-ctx", ArgType.Int) },
        { "yarn-orig-ctx", new ArgumentMapping("--yarn-orig-ctx", ArgType.Int) },
        { "yarnextfactor", new ArgumentMapping("--yarn-ext-factor", ArgType.Float) },
        { "yarn-ext-factor", new ArgumentMapping("--yarn-ext-factor", ArgType.Float) },
        { "yarnattnfactor", new ArgumentMapping("--yarn-attn-factor", ArgType.Float) },
        { "yarn-attn-factor", new ArgumentMapping("--yarn-attn-factor", ArgType.Float) },
        { "yarnbetafast", new ArgumentMapping("--yarn-beta-fast", ArgType.Float) },
        { "yarn-beta-fast", new ArgumentMapping("--yarn-beta-fast", ArgType.Float) },
        { "yarbetaslow", new ArgumentMapping("--yarn-beta-slow", ArgType.Float) },
        { "yarn-beta-slow", new ArgumentMapping("--yarn-beta-slow", ArgType.Float) },
        // --priority-high removed: handled by ServerManager, not a llama.cpp flag
        { "embeddingmode", new ArgumentMapping("--embedding", ArgType.Flag) },
        { "embedding", new ArgumentMapping("--embedding", ArgType.Flag) },
        { "poolingtype", new ArgumentMapping("--pooling", ArgType.String) },
        { "pooling", new ArgumentMapping("--pooling", ArgType.String) },
        { "numa", new ArgumentMapping("--numa", ArgType.BoolOnOff) },
        { "speculativedecoding", new ArgumentMapping("--speculative", ArgType.BoolOnOff) },
        { "speculative", new ArgumentMapping("--speculative", ArgType.BoolOnOff) },
        { "spectype", new ArgumentMapping("--spec-type", ArgType.String) },
        { "spec-type", new ArgumentMapping("--spec-type", ArgType.String) },
        { "ropescaling", new ArgumentMapping("--rope-scaling", ArgType.String) },
        { "rope-scaling", new ArgumentMapping("--rope-scaling", ArgType.String) },
        { "mmproj", new ArgumentMapping("--mmproj", ArgType.String) },
        { "mmprojpath", new ArgumentMapping("--mmproj", ArgType.String) },
        { "draftmodel", new ArgumentMapping("-md", ArgType.String) },
        { "draft-model", new ArgumentMapping("-md", ArgType.String) },
        { "draftmodelpath", new ArgumentMapping("-md", ArgType.String) },
    };
}
