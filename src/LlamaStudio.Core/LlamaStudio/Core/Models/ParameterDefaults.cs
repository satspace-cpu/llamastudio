using System;

namespace LlamaStudio.Core.Models;

public static class ParameterDefaults
{
	public const string Host = "127.0.0.1";

	public const int Port = 8080;

	public const int Timeout = 3600;

	public const int Slots = 1;

	public const string GpuLayers = "all";

	public const int Threads = 0;

	public const int ThreadsBatch = 0;

	public const int MainGpu = 0;

	public const bool FlashAttention = true;

	public const bool Mmap = true;

	public const bool Mlock = false;

	public const bool KvOffloadEnabled = true;

	public const string CacheTypeK = "f16";

	public const string CacheTypeV = "f16";

	public const int CacheRam = -1;

	public const int CacheReuse = 32;

	public const int ContextSize = 0;

	public const int BatchSize = 2048;

	public const int UbatchSize = 512;

	public const int MaxTokens = -1;

	public const bool CachePrompt = true;

	public const bool ContBatching = true;

	public const bool ContextShift = true;

	public const float Temperature = 0.8f;

	public const int TopK = 40;

	public const float TopP = 0.95f;

	public const float MinP = 0.05f;

	public const float TypicalP = 1f;

	public const float RepeatPenalty = 1.1f;

	public const int RepeatLastN = -1;

	public const float PresencePenalty = 0f;

	public const float FrequencyPenalty = 0f;

	public const int Seed = -1;

	public const int Mirostat = 0;

	public const float MirostatTau = 5f;

	public const float MirostatEta = 0.1f;

	public const float DryMultiplier = 0f;

	public const float DryBase = 1.75f;

	public const float DynatempStddev = 0f;

	public const float XtcProbability = 0f;

	public const float XtcThreshold = 0.1f;

	public const string SpecType = "";

	public const int PredictCount = -1;

	public const string SpecDraftGpuLayers = "auto";

	public const int SpecDraftNMax = 3;

	public const int SpecDraftNMin = 0;

	public const float SpecDraftPSplit = 0.1f;

	public const float SpecDraftPMin = 0f;

	public const string RopeScaling = "";

	public const double RopeFreqBase = 0.0;

	public const double RopeFreqScale = 0.0;

	public const int YarnOriginalContext = 0;

	public const double YarnExtFactor = -1.0;

	public const double YarnAttnFactor = -1.0;

	public const double YarnBetaFast = -1.0;

	public const double YarnBetaSlow = -1.0;

	public const bool EnableWebUI = true;

	public const bool EnableSlots = true;

	public const bool EnableMetrics = false;

	public const bool Reasoning = false;

	public const int ReasoningBudget = 0;

	public const bool EmbeddingMode = false;

	public const bool Numa = false;

	public const bool VerboseLogging = false;

	public const bool SpeculativeDecoding = false;

	public const bool PriorityHigh = false;

	public static bool IsDefault(int value, int @default)
	{
		return value == @default;
	}

	public static bool IsDefault(float value, float @default)
	{
		return Math.Abs(value - @default) < 1E-06f;
	}

	public static bool IsDefault(double value, double @default)
	{
		return Math.Abs(value - @default) < 1E-06;
	}

	public static bool IsDefault(string? value, string @default)
	{
		return value == @default;
	}

	public static bool IsDefault(bool value, bool @default)
	{
		return value == @default;
	}
}
