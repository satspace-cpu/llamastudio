using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

/// <summary>Benchmark Center (r101): result lifecycle.</summary>
public enum BenchmarkStatus
{
	Completed,
	Partial,
	Cancelled,
	Failed
}

/// <summary>Kind of benchmark run.</summary>
public enum BenchmarkType
{
	QuickServer,
	PrefillSweep,
	ContextDepth,
	EnginePpTg,
	Full
}

/// <summary>Per-GPU metrics captured while a benchmark ran (r101).</summary>
public class BenchmarkGpuSnapshot
{
	[JsonPropertyName("index")] public int Index { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
	[JsonPropertyName("vramTotalGb")] public double VramTotalGb { get; set; }
	[JsonPropertyName("vramUsedPeakGb")] public double VramUsedPeakGb { get; set; }
	[JsonPropertyName("powerAvgW")] public double PowerAvgW { get; set; }
	[JsonPropertyName("powerPeakW")] public double PowerPeakW { get; set; }
	[JsonPropertyName("utilAvgPercent")] public double UtilAvgPercent { get; set; }
	[JsonPropertyName("utilPeakPercent")] public double UtilPeakPercent { get; set; }
	[JsonPropertyName("tempAvgC")] public double TempAvgC { get; set; }
	[JsonPropertyName("tempMaxC")] public double TempMaxC { get; set; }
	[JsonPropertyName("fanMax")] public int FanMax { get; set; }
}

/// <summary>
/// r101: immutable snapshot of the environment at benchmark time. The run is
/// bound to the profile by reference (Id) AND by this snapshot, because the
/// user may change the profile afterwards.
/// </summary>
public class BenchmarkEnvironmentSnapshot
{
	[JsonPropertyName("profileId")] public string ProfileId { get; set; } = string.Empty;
	[JsonPropertyName("profileName")] public string ProfileName { get; set; } = string.Empty;
	[JsonPropertyName("modelName")] public string ModelName { get; set; } = string.Empty;
	[JsonPropertyName("modelPath")] public string ModelPath { get; set; } = string.Empty;
	[JsonPropertyName("modelFileSizeMb")] public double ModelFileSizeMb { get; set; }
	[JsonPropertyName("modelQuantization")] public string ModelQuantization { get; set; } = string.Empty;
	[JsonPropertyName("llamaCppBuild")] public string LlamaCppBuild { get; set; } = string.Empty;
	[JsonPropertyName("llamaCppVersion")] public string LlamaCppVersion { get; set; } = string.Empty;
	[JsonPropertyName("serverPath")] public string ServerPath { get; set; } = string.Empty;
	[JsonPropertyName("benchPath")] public string BenchPath { get; set; } = string.Empty;
	[JsonPropertyName("operatingSystem")] public string OperatingSystem { get; set; } = string.Empty;
	[JsonPropertyName("osVersion")] public string OsVersion { get; set; } = string.Empty;
	[JsonPropertyName("cpuName")] public string CpuName { get; set; } = string.Empty;
	[JsonPropertyName("cpuCores")] public int CpuCores { get; set; }
	[JsonPropertyName("ramGb")] public double RamGb { get; set; }
	[JsonPropertyName("gpuCount")] public int GpuCount { get; set; }
	[JsonPropertyName("gpuModels")] public string GpuModels { get; set; } = string.Empty;
	[JsonPropertyName("vramTotalGb")] public double VramTotalGb { get; set; }
	[JsonPropertyName("powerLimitW")] public string PowerLimitW { get; set; } = string.Empty;
	[JsonPropertyName("contextSize")] public int ContextSize { get; set; }
	[JsonPropertyName("batchSize")] public int BatchSize { get; set; }
	[JsonPropertyName("ubatchSize")] public int UbatchSize { get; set; }
	[JsonPropertyName("flashAttention")] public bool FlashAttention { get; set; }
	[JsonPropertyName("kvTypeK")] public string KvTypeK { get; set; } = string.Empty;
	[JsonPropertyName("kvTypeV")] public string KvTypeV { get; set; } = string.Empty;
	[JsonPropertyName("gpuLayers")] public string GpuLayers { get; set; } = string.Empty;
	[JsonPropertyName("splitMode")] public string SplitMode { get; set; } = string.Empty;
	[JsonPropertyName("tensorSplit")] public string TensorSplit { get; set; } = string.Empty;
	[JsonPropertyName("selectedGpuIndices")] public string SelectedGpuIndices { get; set; } = string.Empty;
	[JsonPropertyName("threads")] public int Threads { get; set; }
	[JsonPropertyName("temperature")] public float Temperature { get; set; }
	[JsonPropertyName("mtpEnabled")] public bool MtpEnabled { get; set; }
	[JsonPropertyName("mtpDraftModel")] public string MtpDraftModel { get; set; } = string.Empty;
	[JsonPropertyName("mtpNMax")] public int MtpNMax { get; set; }
	[JsonPropertyName("gpus")] public List<BenchmarkGpuSnapshot> Gpus { get; set; } = new List<BenchmarkGpuSnapshot>();
}

/// <summary>
/// r101: one measured point (one context depth / one bench test).
/// All values are real measurements; null/0 = not available (never invented).
/// </summary>
public class BenchmarkPoint
{
	[JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
	/// <summary>Origin tag: "ENGINE" (llama-bench), "SERVER" (HTTP) or an error note.</summary>
	[JsonPropertyName("tag")] public string Tag { get; set; } = string.Empty;
	[JsonPropertyName("contextRequested")] public int ContextRequested { get; set; }
	[JsonPropertyName("promptTokensActual")] public int PromptTokensActual { get; set; }
	[JsonPropertyName("generatedTokens")] public int GeneratedTokens { get; set; }
	[JsonPropertyName("prefillTps")] public double PrefillTps { get; set; }
	[JsonPropertyName("generationTps")] public double GenerationTps { get; set; }
	[JsonPropertyName("e2eTps")] public double E2eTps { get; set; }
	[JsonPropertyName("ttftMs")] public double TtftMs { get; set; }
	[JsonPropertyName("wallTimeS")] public double WallTimeS { get; set; }
	[JsonPropertyName("msPerToken")] public double MsPerToken { get; set; }
	[JsonPropertyName("mtpAcceptancePercent")] public double? MtpAcceptancePercent { get; set; }
	[JsonPropertyName("mtpAccepted")] public int? MtpAccepted { get; set; }
	[JsonPropertyName("mtpGeneratedDraft")] public int? MtpGeneratedDraft { get; set; }
	[JsonPropertyName("mtpMeanLen")] public double? MtpMeanLen { get; set; }
	[JsonPropertyName("powerAvgW")] public double PowerAvgW { get; set; }
	[JsonPropertyName("powerPeakW")] public double PowerPeakW { get; set; }
	[JsonPropertyName("peakVramGb")] public double PeakVramGb { get; set; }
	[JsonPropertyName("mean")] public double? Mean { get; set; }
	[JsonPropertyName("median")] public double? Median { get; set; }
}

/// <summary>r101: benchmark settings snapshot (preset + user choices).</summary>
public class BenchmarkRunSettings
{
	[JsonPropertyName("preset")] public string Preset { get; set; } = "Standard";
	[JsonPropertyName("repetitions")] public int Repetitions { get; set; } = 3;
	[JsonPropertyName("outputTokens")] public int OutputTokens { get; set; } = 1024;
	[JsonPropertyName("warmup")] public bool Warmup { get; set; } = true;
	[JsonPropertyName("useAutoPoints")] public bool UseAutoPoints { get; set; } = true;
	[JsonPropertyName("manualPoints")] public string ManualPoints { get; set; } = string.Empty;
	[JsonPropertyName("requestedPoints")] public List<int> RequestedPoints { get; set; } = new List<int>();
}

/// <summary>
/// r101: a stored benchmark run. Bound to a profile via snapshot.
/// Imported runs are marked Source="imported".
/// </summary>
public class BenchmarkRun
{
	[JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = 1;
	[JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
	[JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
	[JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.Now;
	[JsonPropertyName("type")] public BenchmarkType Type { get; set; }
	[JsonPropertyName("status")] public BenchmarkStatus Status { get; set; } = BenchmarkStatus.Completed;
	[JsonPropertyName("durationS")] public double DurationS { get; set; }
	[JsonPropertyName("source")] public string Source { get; set; } = "local";
	[JsonPropertyName("error")] public string? Error { get; set; }
	[JsonPropertyName("environment")] public BenchmarkEnvironmentSnapshot Environment { get; set; } = new BenchmarkEnvironmentSnapshot();
	[JsonPropertyName("settings")] public BenchmarkRunSettings Settings { get; set; } = new BenchmarkRunSettings();
	[JsonPropertyName("points")] public List<BenchmarkPoint> Points { get; set; } = new List<BenchmarkPoint>();
}
