using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

public class ServerProfile : INotifyPropertyChanged
{
	private const int DefaultPort = 8080;

	private const int DefaultTimeout = 3600;

	private const int DefaultSlots = 1;

	private const int DefaultContextSize = 0;

	private const int DefaultThreads = -1;

	private const string DefaultGpuLayers = "all";

	private const int DefaultBatchSize = 2048;

	private const int DefaultUbatchSize = 512;

	private const int DefaultMaxTokens = -1;

	private const float DefaultTemperature = 0.8f;

	private const int DefaultTopK = 40;

	private const float DefaultTopP = 0.95f;

	private const float DefaultMinP = 0.05f;

	private const float DefaultTypicalP = 1f;

	private const float DefaultRepeatPenalty = 1.1f;

	private const int DefaultRepeatLastN = 64;

	private const float DefaultDryBase = 1.75f;

	private const float DefaultXtcThreshold = 0.1f;

	private bool _isDefault;

	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

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

	[JsonPropertyName("llamaCppBaseDirectory")]
	public string? LlamaCppBaseDirectory { get; set; }

	[JsonPropertyName("selectedServerVersion")]
	public string? SelectedServerVersion { get; set; }

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

	[JsonPropertyName("specDraftGpuLayers")]
	public string SpecDraftGpuLayers { get; set; } = string.Empty;

	[JsonPropertyName("draftModelLocalOnly")]
	public bool DraftModelLocalOnly { get; set; }

	[JsonPropertyName("specDraftNMax")]
	public int SpecDraftNMax { get; set; } = 3;

	[JsonPropertyName("specDraftNMin")]
	public int SpecDraftNMin { get; set; }

	[JsonPropertyName("specDraftPSplit")]
	public float SpecDraftPSplit { get; set; } = 0.1f;

	[JsonPropertyName("specDraftPMin")]
	public float SpecDraftPMin { get; set; }

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
	public int MaxTokens { get; set; } = -1;

	[JsonPropertyName("cachePrompt")]
	public bool CachePrompt { get; set; } = true;

	[JsonPropertyName("contBatching")]
	public bool ContBatching { get; set; } = true;

	[JsonPropertyName("contextShift")]
	public bool ContextShift { get; set; } = true;

	[JsonPropertyName("gpuLayers")]
	public string GpuLayers { get; set; } = string.Empty;

	[JsonPropertyName("gpuSplitMode")]
	public GpuSplitMode GpuSplitMode { get; set; }

	/// <summary>
	/// Migrates the removed legacy <c>row</c> split mode to <c>layer</c>.
	/// The enum member remains solely so JSON profiles written by older releases
	/// can be read without a reset.
	/// </summary>
	public bool NormalizeLegacyGpuSplitMode()
	{
		if (GpuSplitMode != GpuSplitMode.Row)
		{
			return false;
		}

		GpuSplitMode = GpuSplitMode.Layer;
		return true;
	}

	// r68: tensor split mode requires an explicit GPU layer count. An empty
	// (Auto) value makes llama.cpp fail with "llama_params_fit is not
	// implemented for SPLIT_MODE_TENSOR", so it is promoted to "all".
	public string GetEffectiveGpuLayers()
	{
		if (GpuSplitMode == GpuSplitMode.Tensor && (string.IsNullOrWhiteSpace(GpuLayers) || GpuLayers == "-1"))
		{
			return "all";
		}
		return GpuLayers;
	}

	[JsonPropertyName("tensorSplit")]
	public int[] TensorSplit { get; set; } = Array.Empty<int>();

	[JsonPropertyName("mainGpu")]
	public int MainGpu { get; set; }

	[JsonPropertyName("selectedGpuIndices")]
	public int[] SelectedGpuIndices { get; set; } = Array.Empty<int>();

	// r94: per-GPU power limits last applied through this profile (index -> watts).
	// Different profiles may keep different limits for the same cards.
	[JsonPropertyName("gpuPowerLimits")]
	public Dictionary<string, string> GpuPowerLimits { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
	public int ContextSize { get; set; }

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
	public int CacheReuse { get; set; }

	[JsonPropertyName("threads")]
	public int Threads { get; set; } = -1;

	[JsonPropertyName("threadsBatch")]
	public int ThreadsBatch { get; set; } = -1;

	[JsonPropertyName("temperature")]
	public float Temperature { get; set; } = 0.8f;

	[JsonPropertyName("topK")]
	public int TopK { get; set; } = 40;

	[JsonPropertyName("topP")]
	public float TopP { get; set; } = 0.95f;

	[JsonPropertyName("minP")]
	public float MinP { get; set; } = 0.05f;

	[JsonPropertyName("typicalP")]
	public float TypicalP { get; set; } = 1f;

	[JsonPropertyName("repeatPenalty")]
	public float RepeatPenalty { get; set; } = 1.1f;

	[JsonPropertyName("repeatLastN")]
	public int RepeatLastN { get; set; } = 64;

	[JsonPropertyName("mirostat")]
	public MirostatMode Mirostat { get; set; }

	[JsonPropertyName("mirostatTau")]
	public float MirostatTau { get; set; } = 5f;

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
	public float XtcThreshold { get; set; }

	[JsonPropertyName("host")]
	public string Host { get; set; } = "127.0.0.1";

	[JsonPropertyName("port")]
	public int Port { get; set; } = 8080;

	[JsonPropertyName("rpcEnabled")]
	public bool RpcEnabled { get; set; }

	[JsonPropertyName("rpcEndpoint")]
	public string RpcEndpoint { get; set; } = string.Empty;

	[JsonPropertyName("rpcSshUsername")]
	public string RpcSshUsername { get; set; } = string.Empty;

	[JsonPropertyName("rpcSshPassword")]
	public string RpcSshPassword { get; set; } = string.Empty;

	[JsonPropertyName("rpcSavePassword")]
	public bool RpcSavePassword { get; set; }

	[JsonPropertyName("rpcUseCache")]
	public bool RpcUseCache { get; set; }

	[JsonPropertyName("rpcRemoteTelemetry")]
	public bool RpcRemoteTelemetry { get; set; } = true;

	[JsonPropertyName("rpcServerPath")]
	public string RpcServerPath { get; set; } = string.Empty;

	[JsonPropertyName("timeout")]
	public int Timeout { get; set; } = 3600;

	[JsonPropertyName("slots")]
	public int Slots { get; set; } = DefaultSlots;

	[JsonPropertyName("predictCount")]
	public int PredictCount { get; set; } = -1;

	[JsonPropertyName("specType")]
	public string SpecType { get; set; } = "none";

	[JsonPropertyName("speculativeDecoding")]
	public bool SpeculativeDecoding { get; set; }

	[JsonPropertyName("jinja")]
	public bool Jinja { get; set; } = true;

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
	public Dictionary<string, string> CustomArguments { get; set; } = new Dictionary<string, string>();

	[JsonPropertyName("customArgumentToggleStates")]
	public Dictionary<string, bool> CustomArgumentToggleStates { get; set; } = new Dictionary<string, bool>();

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[JsonPropertyName("updatedAt")]
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	[JsonPropertyName("isDefault")]
	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			_isDefault = value;
			OnPropertyChanged("IsDefault");
		}
	}

	[JsonPropertyName("color")]
	public string Color { get; set; } = "#3B82F6";

	[JsonIgnore]
	public string DeviceBackendPrefix { get; set; } = "CUDA";

	private const int RpcGpuSelectionIndex = 100000;

	private string EffectiveDeviceBackendPrefix()
	{
		string? buildType = SelectedServerVersion?.Split('|').LastOrDefault();
		if (!string.IsNullOrWhiteSpace(buildType))
		{
			string normalized = buildType.ToLowerInvariant();
			if (normalized.Contains("vulkan"))
			{
				return "Vulkan";
			}
			if (normalized.Contains("sycl"))
			{
				return "SYCL";
			}
			if (normalized.Contains("metal"))
			{
				return "METAL";
			}
			if (normalized.Contains("rpc"))
			{
				return "RPC";
			}
			if (normalized.Contains("cpu"))
			{
				return string.Empty;
			}
			if (normalized.Contains("openvino"))
			{
				return string.Empty;
			}
		}
		return DeviceBackendPrefix;
	}

	private string BuildSelectedDeviceList(string deviceBackendPrefix)
	{
		List<string> devices = SelectedGpuIndices
			.Where(index => index != RpcGpuSelectionIndex && !string.IsNullOrWhiteSpace(deviceBackendPrefix))
			.Select(index => deviceBackendPrefix + index)
			.ToList();
		if (RpcEnabled && SelectedGpuIndices.Contains(RpcGpuSelectionIndex))
		{
			devices.Add("RPC0");
		}
		return string.Join(",", devices);
	}

	private string GetDraftLocalDevice()
	{
		string prefix = EffectiveDeviceBackendPrefix();
		if (string.IsNullOrWhiteSpace(prefix) || prefix.Equals("RPC", StringComparison.OrdinalIgnoreCase))
			return string.Empty;
		int localIndex = SelectedGpuIndices.FirstOrDefault(index => index != RpcGpuSelectionIndex);
		return prefix + localIndex.ToString(CultureInfo.InvariantCulture);
	}

	private string BuildTensorSplitForCommand()
	{
		// Tensor-split values follow the exact order emitted by --device:
		// local CUDA devices first, then RPC0. Do not reverse the profile values;
		// the web/desktop slider already presents them in this same order.
		return string.Join(",", TensorSplit);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(string name)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	public string BuildFullArgsString()
	{
		return BuildReferenceArgsString();
	}

	private string EffectiveSpecType()
	{
		if (string.IsNullOrWhiteSpace(SpecType) || SpecType.Equals("none", StringComparison.OrdinalIgnoreCase))
			return string.Empty;
		if (SpeculativeDecoding)
			return SpecType;
		return string.Join(",", SpecType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(type => type.Equals("draft-mtp", StringComparison.OrdinalIgnoreCase) || type.StartsWith("ngram-", StringComparison.OrdinalIgnoreCase)));
	}

	private bool IsSpeculativeModeActive() => !string.IsNullOrWhiteSpace(EffectiveSpecType());

	private bool IsDraftModelActive() => SpeculativeDecoding && !string.IsNullOrWhiteSpace(SpecType) && !SpecType.Equals("none", StringComparison.OrdinalIgnoreCase);

	// DFlash, DSpark and EAGLE consume hidden states from the target model. With a
	// CUDA/RPC tensor split they must use the target model's device layout.
	private bool CanPinDraftModelToLocal()
	{
		if (!IsDraftModelActive())
			return false;
		return !SpecType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Any(type => type.Equals("draft-dflash", StringComparison.OrdinalIgnoreCase)
				|| type.Equals("draft-dspark", StringComparison.OrdinalIgnoreCase)
				|| type.Equals("draft-eagle3", StringComparison.OrdinalIgnoreCase));
	}

	private static string F(float floatValue)
	{
		return floatValue.ToString("0.########", CultureInfo.InvariantCulture);
	}

	private static string D(double doubleValue)
	{
		return doubleValue.ToString("0.########", CultureInfo.InvariantCulture);
	}

	public string BuildReferenceArgsString()
	{
		List<string> args = new List<string>();
		string deviceBackendPrefix = EffectiveDeviceBackendPrefix();
		Dictionary<string, string> customArgs;
		HashSet<string> customFlags;
		(customArgs, customFlags) = CommandLineParser.ParseArguments(CustomArguments);
		if (!string.IsNullOrWhiteSpace(ModelPath))
		{
			AddValue("-m", Q(ModelPath), new string[1] { "--model" });
		}
		if (!string.IsNullOrWhiteSpace(MmprojPath))
		{
			AddValue("--mmproj", Q(MmprojPath), Array.Empty<string>());
		}
		if (!string.IsNullOrWhiteSpace(HfRepo))
		{
			AddValue("-hf", HfRepo, new string[2] { "--hf-repo", "-hfr" });
		}
		if (!string.IsNullOrWhiteSpace(HfFile))
		{
			AddValue("-hff", HfFile, new string[1] { "--hf-file" });
		}
		if (HfOffline)
		{
			AddFlag("--offline", enabled: true, Array.Empty<string>());
		}
		if (!string.IsNullOrWhiteSpace(HfRepoDraft))
		{
			AddValue("-hfd", HfRepoDraft, new string[2] { "--hf-repo-draft", "-hfrd" });
		}
		if (!string.IsNullOrWhiteSpace(Host) && Host != "127.0.0.1")
		{
			AddValue("--host", Host, Array.Empty<string>());
		}
		int argValue;
		if (Port != 8080)
		{
			argValue = Port;
			AddValue("--port", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (RpcEnabled && !string.IsNullOrWhiteSpace(RpcEndpoint))
		{
			AddValue("--rpc", RpcEndpoint.Trim(), Array.Empty<string>());
		}
		if (ContextSize != 0)
		{
			argValue = ContextSize;
			AddValue("-c", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--ctx-size" });
		}
		if (Threads > 0)
		{
			argValue = Threads;
			AddValue("-t", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--threads" });
		}
		if (ThreadsBatch > 0)
		{
			argValue = ThreadsBatch;
			AddValue("-tb", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--threads-batch" });
		}
		string effectiveNgl = GetEffectiveGpuLayers();
		if (effectiveNgl != "-1" && !string.IsNullOrWhiteSpace(effectiveNgl))
		{
			AddValue("-ngl", effectiveNgl, new string[1] { "--gpu-layers" });
		}
		if (GpuSplitMode != GpuSplitMode.None)
		{
			AddValue("-sm", GpuSplitMode.ToString().ToLowerInvariant(), new string[1] { "--split-mode" });
		}
		// With RPC enabled, do not let llama.cpp choose the device order. Its
		// automatic enumeration can put RPC0 before the local CUDA device, which
		// makes a UI split such as 89,11 allocate 89% to the remote card.
		// Keep the same order as the UI: local CUDA devices first, RPC0 last.
		if (SelectedGpuIndices.Length != 0 && !string.IsNullOrWhiteSpace(deviceBackendPrefix))
		{
			string deviceList = BuildSelectedDeviceList(deviceBackendPrefix);
			if (!string.IsNullOrWhiteSpace(deviceList))
			{
				AddValue("--device", deviceList, Array.Empty<string>());
			}
		}
		if (TensorSplit.Length != 0 && (SelectedGpuIndices.Length > 1 || (RpcEnabled && SelectedGpuIndices.Contains(RpcGpuSelectionIndex))))
		{
			AddValue("-ts", BuildTensorSplitForCommand(), new string[1] { "--tensor-split" });
		}
		if (MainGpu >= 0 && ((!RpcEnabled && SelectedGpuIndices.Length <= 1) || (RpcEnabled && TensorSplit.Length == 0)))
		{
			argValue = MainGpu;
			AddValue("-mg", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--main-gpu" });
		}
		if (!FlashAttention)
		{
			AddBoolOnOff("-fa", false, new string[1] { "--flash-attn" });
		}
		if (!Mmap)
		{
			AddFlag("--no-mmap", enabled: true, new string[1] { "--mmap" });
		}
		if (Mlock)
		{
                // The legacy --mlock flag was used together with the default mmap mode.
                // Preserve that behavior with the explicit modern equivalent; --load-mode mlock
                // alone is a different load strategy and does not retain the previous mmap semantics.
                AddValue("--load-mode", "mmap+mlock", new string[1] { "--mlock" });
		}
		if (!KvOffloadEnabled)
		{
			AddFlag("-nkvo", enabled: true, new string[1] { "--no-kv-offload" });
		}
		if (BatchSize != 2048)
		{
			argValue = BatchSize;
			AddValue("-b", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--batch-size" });
		}
		if (UbatchSize != 512)
		{
			argValue = UbatchSize;
			AddValue("-ub", argValue.ToString(CultureInfo.InvariantCulture), new string[1] { "--ubatch-size" });
		}
		if (CacheTypeK != CacheTypeK.F16)
		{
			AddValue("-ctk", CacheTypeK.ToString().ToLowerInvariant(), new string[1] { "--cache-type-k" });
		}
		if (CacheTypeV != CacheTypeV.F16)
		{
			AddValue("-ctv", CacheTypeV.ToString().ToLowerInvariant(), new string[1] { "--cache-type-v" });
		}
		if (CacheRam > 0)
		{
			argValue = CacheRam;
			AddValue("--cache-ram", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (CacheReuse > 0)
		{
			argValue = CacheReuse;
			AddValue("--cache-reuse", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (Math.Abs(Temperature - 0.8f) > 1E-06f)
		{
			AddValue("--temp", F(Temperature), new string[1] { "--temperature" });
		}
		if (TopK != 40)
		{
			argValue = TopK;
			AddValue("--top-k", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (Math.Abs(TopP - 0.95f) > 1E-06f)
		{
			AddValue("--top-p", F(TopP), Array.Empty<string>());
		}
		if (Math.Abs(MinP - 0.05f) > 1E-06f)
		{
			AddValue("--min-p", F(MinP), Array.Empty<string>());
		}
		if (Math.Abs(TypicalP - 1f) > 1E-06f)
		{
			AddValue("--typical-p", F(TypicalP), new string[1] { "--typical" });
		}
		if (Math.Abs(RepeatPenalty - 1.1f) > 1E-06f)
		{
			AddValue("--repeat-penalty", F(RepeatPenalty), Array.Empty<string>());
		}
		if (RepeatLastN != 64)
		{
			argValue = RepeatLastN;
			AddValue("--repeat-last-n", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (PresencePenalty != 0f)
		{
			AddValue("--presence-penalty", F(PresencePenalty), Array.Empty<string>());
		}
		if (FrequencyPenalty != 0f)
		{
			AddValue("--frequency-penalty", F(FrequencyPenalty), Array.Empty<string>());
		}
		if (Mirostat > MirostatMode.Disabled)
		{
			argValue = (int)Mirostat;
			AddValue("--mirostat", argValue.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
			AddValue("--mirostat-ent", F(MirostatTau), Array.Empty<string>());
			AddValue("--mirostat-lr", F(MirostatEta), Array.Empty<string>());
		}
		if (DryMultiplier > 0f)
		{
			AddValue("--dry-multiplier", F(DryMultiplier), Array.Empty<string>());
		}
		if (Math.Abs(DryBase - 1.75f) > 1E-06f)
		{
			AddValue("--dry-base", F(DryBase), Array.Empty<string>());
		}
		if (DynatempStddev > 0f)
		{
			AddValue("--dynatemp-range", F(DynatempStddev), Array.Empty<string>());
		}
		if (XtcProbability > 0f)
		{
			AddValue("--xtc-probability", F(XtcProbability), Array.Empty<string>());
		}
		if (Math.Abs(XtcThreshold - 0.1f) > 1E-06f && XtcThreshold > 0f)
		{
			AddValue("--xtc-threshold", F(XtcThreshold), Array.Empty<string>());
		}
		if (MaxTokens > 0)
		{
			argValue = MaxTokens;
			AddValue("-n", argValue.ToString(CultureInfo.InvariantCulture), new string[2] { "--predict", "--n-predict" });
		}
		else if (PredictCount > 0)
		{
			argValue = PredictCount;
			AddValue("-n", argValue.ToString(CultureInfo.InvariantCulture), new string[2] { "--predict", "--n-predict" });
		}
		string effectiveSpecType = EffectiveSpecType();
		bool speculativeActive = !string.IsNullOrWhiteSpace(effectiveSpecType);
		bool draftModelActive = IsDraftModelActive();
		if (speculativeActive)
		{
			AddValue("--spec-type", effectiveSpecType, Array.Empty<string>());
			if (draftModelActive && !string.IsNullOrWhiteSpace(DraftModelPath))
				AddValue("-md", Q(DraftModelPath), new string[2] { "--spec-draft-model", "--model-draft" });
			if (draftModelActive && DraftModelLocalOnly && CanPinDraftModelToLocal() && !string.IsNullOrWhiteSpace(GetDraftLocalDevice()))
				AddValue("--spec-draft-device", GetDraftLocalDevice(), new string[1] { "-devd" });
			if (draftModelActive && !string.IsNullOrWhiteSpace(SpecDraftGpuLayers) && int.TryParse(SpecDraftGpuLayers, out argValue))
				AddValue("-ngld", SpecDraftGpuLayers, new string[3] { "--spec-draft-ngl", "--gpu-layers-draft", "--n-gpu-layers-draft" });
			// llama.cpp supplies 3 for built-in MTP, but an external draft model must
			// receive the value explicitly, including the value 3.
			if (SpecDraftNMax != 3 || (draftModelActive && !string.IsNullOrWhiteSpace(DraftModelPath)))
				AddValue("--spec-draft-n-max", SpecDraftNMax.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
			if (SpecDraftNMin != 0)
				AddValue("--spec-draft-n-min", SpecDraftNMin.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
			if (Math.Abs(SpecDraftPSplit - 0.1f) > 1E-06f)
				AddValue("--spec-draft-p-split", F(SpecDraftPSplit), new string[1] { "--draft-p-split" });
			if (SpecDraftPMin != 0f)
				AddValue("--spec-draft-p-min", F(SpecDraftPMin), new string[1] { "--draft-p-min" });
		}
		if (!string.IsNullOrWhiteSpace(RopeScaling))
		{
			AddValue("--rope-scaling", RopeScaling.ToLowerInvariant(), Array.Empty<string>());
		}
		if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0.0)
		{
			AddValue("--rope-freq-base", D(RopeFreqBase.Value), Array.Empty<string>());
		}
		if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0.0)
		{
			AddValue("--rope-freq-scale", D(RopeFreqScale.Value), Array.Empty<string>());
		}
		if (YarnOriginalContext.HasValue && YarnOriginalContext.Value != 0)
		{
			AddValue("--yarn-orig-ctx", YarnOriginalContext.Value.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (YarnExtFactor.HasValue && Math.Abs(YarnExtFactor.Value - -1.0) > 1E-06)
		{
			AddValue("--yarn-ext-factor", D(YarnExtFactor.Value), Array.Empty<string>());
		}
		if (YarnAttnFactor.HasValue && Math.Abs(YarnAttnFactor.Value - -1.0) > 1E-06)
		{
			AddValue("--yarn-attn-factor", D(YarnAttnFactor.Value), Array.Empty<string>());
		}
		if (YarnBetaFast.HasValue && Math.Abs(YarnBetaFast.Value - -1.0) > 1E-06)
		{
			AddValue("--yarn-beta-fast", D(YarnBetaFast.Value), Array.Empty<string>());
		}
		if (YarnBetaSlow.HasValue && Math.Abs(YarnBetaSlow.Value - -1.0) > 1E-06)
		{
			AddValue("--yarn-beta-slow", D(YarnBetaSlow.Value), Array.Empty<string>());
		}
		if (!CachePrompt)
		{
			AddFlag("--no-cache-prompt", enabled: true, new string[1] { "--cache-prompt" });
		}
		if (!ContBatching)
		{
			AddFlag("-nocb", enabled: true, new string[1] { "--no-cont-batching" });
		}
		if (!ContextShift)
		{
			AddFlag("--no-context-shift", enabled: true, Array.Empty<string>());
		}
		if (VerboseLogging)
		{
			AddFlag("--verbose", enabled: true, new string[1] { "-v" });
		}
		if (!EnableWebUI)
		{
			AddFlag("--no-ui", enabled: true, new string[1] { "--no-webui" });
		}
		AddFlag("--metrics", EnableMetrics, Array.Empty<string>());
		if (Reasoning)
		{
			AddBoolOnOff("-rea", true, new string[1] { "--reasoning" });
		}
		if (Reasoning && ReasoningBudget > 0)
		{
			AddValue("--reasoning-budget", ReasoningBudget.ToString(CultureInfo.InvariantCulture), Array.Empty<string>());
		}
		if (Jinja)
			AddFlag("--jinja", enabled: true, Array.Empty<string>());
		AddFlag("--embedding", EmbeddingMode, new string[1] { "--embeddings" });
		if (!string.IsNullOrWhiteSpace(PoolingType))
		{
			AddValue("--pooling", PoolingType, Array.Empty<string>());
		}
		if (Numa)
		{
			AddValue("--numa", "distribute", Array.Empty<string>());
		}
		if (!string.IsNullOrWhiteSpace(CpuAffinity))
		{
			AddValue("-C", CpuAffinity, new string[1] { "--cpu-mask" });
		}
		if (Seed >= 0)
		{
			AddValue("-s", Seed.ToString(CultureInfo.InvariantCulture), new string[1] { "--seed" });
		}
		if (Timeout != 3600)
		{
			AddValue("-to", Timeout.ToString(CultureInfo.InvariantCulture), new string[1] { "--timeout" });
		}
		if (Slots > 0)
		{
			AddValue("-np", Slots.ToString(CultureInfo.InvariantCulture), new string[1] { "--parallel" });
		}
		if (!EnableSlots)
		{
			AddFlag("--no-slots", enabled: true, new string[1] { "--slots" });
		}
		if (!string.IsNullOrWhiteSpace(ApiKey))
		{
			AddValue("--api-key", Q(ApiKey), Array.Empty<string>());
		}
		if (!string.IsNullOrWhiteSpace(Alias))
		{
			AddValue("-a", Q(Alias), new string[1] { "--alias" });
		}
		if (!string.IsNullOrWhiteSpace(LogFilePath))
		{
			AddValue("--log-file", Q(LogFilePath), Array.Empty<string>());
		}
		AddRemainingCustomArgs(args, customArgs, customFlags);
		return string.Join(" ", args);
		void AddBoolOnOff(string flag, bool? onOffValue, string[] aliases)
		{
			if (onOffValue.HasValue && !IsOverridden(new string[1] { flag }.Concat(aliases).ToArray()))
			{
				args.Add(flag + " " + (onOffValue.Value ? "on" : "off"));
			}
		}
		void AddFlag(string flag, bool enabled, string[] aliases)
		{
			if (enabled && !IsOverridden(new string[1] { flag }.Concat(aliases).ToArray()))
			{
				args.Add(flag);
			}
		}
		void AddValue(string flag, string flagValue, string[] aliases)
		{
			if (!IsOverridden(new string[1] { flag }.Concat(aliases).ToArray()))
			{
				args.Add(flag + " " + flagValue);
			}
		}
		bool IsOverridden(string[] names)
		{
			foreach (string name in names)
			{
				if (customArgs.ContainsKey(name) || customFlags.Contains(name))
				{
					return true;
				}
			}
			return false;
		}
		static string Q(string rawValue)
		{
			return "\"" + rawValue.Replace("\"", "\\\"") + "\"";
		}
	}

	private string BuildLegacyFullArgsString()
	{
		string deviceBackendPrefix = EffectiveDeviceBackendPrefix();
		List<string> args = new List<string>();
		Dictionary<string, string> customArgs;
		HashSet<string> customFlags;
		(customArgs, customFlags) = CommandLineParser.ParseArguments(CustomArguments);
		string effectiveSpecType = EffectiveSpecType();
		bool speculativeActive = !string.IsNullOrWhiteSpace(effectiveSpecType);
		bool draftModelActive = IsDraftModelActive();
		AddIfNotOverridden("model", "-m \"" + ModelPath + "\"");
		if (Port != 8080)
		{
			AddIfNotOverridden("port", $"--port {Port}");
		}
		if (Threads != -1)
		{
			AddIfNotOverridden("threads", $"--threads {Threads}");
		}
		if (ThreadsBatch != -1)
		{
			AddIfNotOverridden("threadsBatch", $"--threads-batch {ThreadsBatch}");
		}
		string effectiveNgl = GetEffectiveGpuLayers();
		if (!string.IsNullOrWhiteSpace(effectiveNgl))
		{
			AddIfNotOverridden("gpuLayers", "-ngl " + effectiveNgl);
		}
		if (ContextSize != 0)
		{
			AddIfNotOverridden("contextSize", $"-c {ContextSize}");
		}
		if (BatchSize != 2048)
		{
			AddIfNotOverridden("batchSize", $"--batch-size {BatchSize}");
		}
		if (UbatchSize != 512)
		{
			AddIfNotOverridden("ubatchSize", $"--ubatch-size {UbatchSize}");
		}
		if (Math.Abs(Temperature - 0.8f) > 1E-06f)
		{
			AddIfNotOverridden("temperature", "--temp " + F(Temperature));
		}
		if (TopK != 40)
		{
			AddIfNotOverridden("topK", $"--top-k {TopK}");
		}
		if (Math.Abs(TopP - 0.95f) > 1E-06f)
		{
			AddIfNotOverridden("topP", "--top-p " + F(TopP));
		}
		if (Math.Abs(MinP - 0.05f) > 1E-06f)
		{
			AddIfNotOverridden("minP", "--min-p " + F(MinP));
		}
		if (!customArgs.ContainsKey("flashAttention") && !customFlags.Contains("flashAttention") && !FlashAttention)
		{
			args.Add("-fa");
			args.Add("off");
		}
		if (!string.IsNullOrEmpty(Host))
		{
			AddIfNotOverridden("host", "--host " + Host);
		}
		if (Mirostat > MirostatMode.Disabled)
		{
			AddIfNotOverridden("mirostat", $"--mirostat {Mirostat}");
			AddIfNotOverridden("mirostatLearnRate", "--mirostat-lr " + F(MirostatEta));
			AddIfNotOverridden("mirostatTau", "--mirostat-ent " + F(MirostatTau));
		}
		if (speculativeActive)
		{
			if (!customArgs.ContainsKey("specType") && !customFlags.Contains("specType"))
			{
				args.Add("--spec-type");
				args.Add(effectiveSpecType);
			}
		}
		if (!string.IsNullOrWhiteSpace(RopeScaling))
		{
			AddIfNotOverridden("ropeScaling", "--rope-scaling " + RopeScaling.ToLowerInvariant());
		}
		if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0.0)
		{
			AddIfNotOverridden("ropeFreqBase", "--rope-freq-base " + D(RopeFreqBase.Value));
		}
		if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0.0)
		{
			AddIfNotOverridden("ropeFreqScale", "--rope-freq-scale " + D(RopeFreqScale.Value));
		}
		AddBoolFlag("embeddingMode", "--embedding", EmbeddingMode);
		AddIfNotOverridden("timeout", $"--timeout {Timeout}");
		if (!string.IsNullOrWhiteSpace(Alias))
		{
			AddIfNotOverridden("alias", "--alias \"" + Alias + "\"");
		}
		if (!string.IsNullOrWhiteSpace(ApiKey))
		{
			AddIfNotOverridden("apiKey", "--api-key \"" + ApiKey + "\"");
		}
		if (!string.IsNullOrWhiteSpace(LogFilePath))
		{
			AddIfNotOverridden("logFilePath", "--log-file \"" + LogFilePath + "\"");
		}
		AddBoolFlag("verboseLogging", "--verbose", VerboseLogging);
		if (!customArgs.ContainsKey("enableWebUI") && !customFlags.Contains("enableWebUI") && EnableWebUI)
		{
			args.Add("--ui");
		}
		if (!customArgs.ContainsKey("--slots") && !customArgs.ContainsKey("--no-slots") && !customFlags.Contains("--slots") && !customFlags.Contains("--no-slots") && !EnableSlots)
		{
			args.Add("--no-slots");
		}
		if (Slots > 0)
		{
			AddIfNotOverridden("slots", $"-np {Slots}");
		}
		AddBoolFlag("enableMetrics", "--metrics", EnableMetrics);
		if (!customArgs.ContainsKey("reasoning") && !customFlags.Contains("reasoning") && Reasoning)
		{
			args.Add("-rea");
			args.Add("on");
		}
		if (Reasoning && ReasoningBudget > 0)
		{
			AddIfNotOverridden("reasoningBudget", $"--reasoning-budget {ReasoningBudget}");
		}
		if (Seed >= 0)
		{
			AddIfNotOverridden("seed", $"--seed {Seed}");
		}
		if (PresencePenalty != 0f)
		{
			AddIfNotOverridden("presencePenalty", "--presence-penalty " + F(PresencePenalty));
		}
		if (FrequencyPenalty != 0f)
		{
			AddIfNotOverridden("frequencyPenalty", "--frequency-penalty " + F(FrequencyPenalty));
		}
		if (MaxTokens > 0)
		{
			AddIfNotOverridden("maxTokens", $"-n {MaxTokens}");
		}
		if (!customArgs.ContainsKey("cachePrompt") && !customFlags.Contains("cachePrompt") && !CachePrompt)
		{
			args.Add("--no-cache-prompt");
		}
		if (!customArgs.ContainsKey("contBatching") && !customFlags.Contains("contBatching") && !ContBatching)
		{
			args.Add("-nocb");
		}
		if (!customArgs.ContainsKey("contextShift") && !customFlags.Contains("contextShift") && !ContextShift)
		{
			args.Add("--no-context-shift");
		}
		if (GpuSplitMode != GpuSplitMode.None)
		{
			AddIfNotOverridden("gpuSplitMode", $"--split-mode {GpuSplitMode.ToString().ToLowerInvariant()}");
		}
		if (SelectedGpuIndices.Length != 0 && !string.IsNullOrWhiteSpace(deviceBackendPrefix))
		{
			string deviceList = BuildSelectedDeviceList(deviceBackendPrefix);
			if (!string.IsNullOrWhiteSpace(deviceList))
			{
				AddIfNotOverridden("selectedGpuIndices", "--device " + deviceList);
			}
		}
		if (TensorSplit.Length != 0 && SelectedGpuIndices.Length > 1)
		{
			AddIfNotOverridden("tensorSplit", "--tensor-split " + BuildTensorSplitForCommand());
		}
		if (MainGpu >= 0 && SelectedGpuIndices.Length <= 1)
		{
			AddIfNotOverridden("mainGpu", $"--main-gpu {MainGpu}");
		}
		if (!customArgs.ContainsKey("mmap") && !customFlags.Contains("mmap") && !Mmap)
		{
			args.Add("--no-mmap");
		}
		if (Mlock)
		{
                AddIfNotOverridden("mlock", "--load-mode mmap+mlock");
		}
		if (!KvOffloadEnabled)
		{
			AddBoolFlag("kvOffloadDisabled", "-nkvo", !KvOffloadEnabled);
		}
		if (CacheTypeK != CacheTypeK.F16)
		{
			AddIfNotOverridden("cacheTypeK", "--cache-type-k " + CacheTypeK.ToString().ToLowerInvariant());
		}
		if (CacheTypeV != CacheTypeV.F16)
		{
			AddIfNotOverridden("cacheTypeV", "--cache-type-v " + CacheTypeV.ToString().ToLowerInvariant());
		}
		if (CacheRam >= 0)
		{
			AddIfNotOverridden("cacheRam", $"--cache-ram {CacheRam}");
		}
		if (CacheReuse > 0)
		{
			AddIfNotOverridden("cacheReuse", $"--cache-reuse {CacheReuse}");
		}
		if (Math.Abs(TypicalP - 1f) > 1E-06f)
		{
			AddIfNotOverridden("typicalP", $"--typical-p {TypicalP}");
		}
		if (Math.Abs(RepeatPenalty - 1.1f) > 1E-06f)
		{
			AddIfNotOverridden("repeatPenalty", "--repeat-penalty " + F(RepeatPenalty));
		}
		if (RepeatLastN != 64)
		{
			AddIfNotOverridden("repeatLastN", $"--repeat-last-n {RepeatLastN}");
		}
		if (DryMultiplier > 0f)
		{
			AddIfNotOverridden("dryMultiplier", "--dry-multiplier " + F(DryMultiplier));
		}
		if (Math.Abs(DryBase - 1.75f) > 1E-06f)
		{
			AddIfNotOverridden("dryBase", "--dry-base " + F(DryBase));
		}
		if (DynatempStddev > 0f)
		{
			AddIfNotOverridden("dynatempStddev", "--dynatemp-range " + F(DynatempStddev));
		}
		if (XtcProbability > 0f)
		{
			AddIfNotOverridden("xtcProbability", "--xtc-probability " + F(XtcProbability));
		}
		if (Math.Abs(XtcThreshold - 0.1f) > 1E-06f)
		{
			AddIfNotOverridden("xtcThreshold", "--xtc-threshold " + F(XtcThreshold));
		}
		if (PredictCount > 0)
		{
			AddIfNotOverridden("predictCount", $"--predict {PredictCount}");
		}
		if (draftModelActive && !string.IsNullOrWhiteSpace(SpecDraftGpuLayers))
		{
			AddIfNotOverridden("specDraftGpuLayers", "--ngld " + SpecDraftGpuLayers);
		}
		if (speculativeActive && SpecDraftNMax > 0)
		{
			AddIfNotOverridden("specDraftNMax", $"--spec-draft-n-max {SpecDraftNMax}");
		}
		if (speculativeActive && SpecDraftNMin > 0)
		{
			AddIfNotOverridden("specDraftNMin", $"--spec-draft-n-min {SpecDraftNMin}");
		}
		if (speculativeActive && SpecDraftPSplit > 0f)
		{
			AddIfNotOverridden("specDraftPSplit", "--spec-draft-p-split " + F(SpecDraftPSplit));
		}
		if (speculativeActive && SpecDraftPMin > 0f)
		{
			AddIfNotOverridden("specDraftPMin", "--spec-draft-p-min " + F(SpecDraftPMin));
		}
		if (YarnOriginalContext.HasValue)
		{
			AddIfNotOverridden("yarnOriginalContext", $"--yarn-orig-ctx {YarnOriginalContext.Value}");
		}
		if (YarnExtFactor.HasValue)
		{
			AddIfNotOverridden("yarnExtFactor", "--yarn-ext-factor " + D(YarnExtFactor.Value));
		}
		if (YarnAttnFactor.HasValue)
		{
			AddIfNotOverridden("yarnAttnFactor", "--yarn-attn-factor " + D(YarnAttnFactor.Value));
		}
		if (YarnBetaFast.HasValue)
		{
			AddIfNotOverridden("yarnBetaFast", "--yarn-beta-fast " + D(YarnBetaFast.Value));
		}
		if (YarnBetaSlow.HasValue)
		{
			AddIfNotOverridden("yarnBetaSlow", "--yarn-beta-slow " + D(YarnBetaSlow.Value));
		}
		if (!string.IsNullOrWhiteSpace(PoolingType))
		{
			AddIfNotOverridden("poolingType", "--pooling " + PoolingType);
		}
		if (Numa)
		{
			AddIfNotOverridden("numa", "--numa distribute");
		}
		if (Jinja && !customArgs.ContainsKey("jinja") && !customFlags.Contains("jinja"))
			args.Add("--jinja");
		if (!string.IsNullOrWhiteSpace(MmprojPath))
		{
			AddIfNotOverridden("mmproj", "--mmproj \"" + MmprojPath + "\"");
		}
		if (draftModelActive && !string.IsNullOrWhiteSpace(DraftModelPath))
		{
			AddIfNotOverridden("draftModel", "-md \"" + DraftModelPath + "\"");
		}
		if (draftModelActive && DraftModelLocalOnly && CanPinDraftModelToLocal() && !string.IsNullOrWhiteSpace(GetDraftLocalDevice()))
		{
			AddIfNotOverridden("draftModelLocalOnly", "--spec-draft-device " + GetDraftLocalDevice());
		}
		AddRemainingCustomArgs(args, customArgs, customFlags);
		return string.Join(" ", args);
		void AddBoolFlag(string canonical, string flag, bool enabled)
		{
			if (!customArgs.ContainsKey(canonical) && !customFlags.Contains(canonical) && enabled)
			{
				args.Add(flag);
			}
		}
		void AddBoolOnOff(string canonical, string flag, bool enabled)
		{
			if (!customArgs.ContainsKey(canonical) && !customFlags.Contains(canonical))
			{
				args.Add(enabled ? flag : (flag + "=off"));
			}
		}
		void AddIfNotOverridden(string canonical, string argValue)
		{
			if (!customArgs.ContainsKey(canonical))
			{
				args.Add(argValue);
			}
		}
	}

	public string BuildArgsString()
	{
		List<string> args = new List<string>();
		string deviceBackendPrefix = EffectiveDeviceBackendPrefix();
		string effectiveSpecType = EffectiveSpecType();
		bool speculativeActive = !string.IsNullOrWhiteSpace(effectiveSpecType);
		bool draftModelActive = IsDraftModelActive();
		if (!string.IsNullOrWhiteSpace(ModelPath))
		{
			args.Add("-m \"" + ModelPath + "\"");
		}
		if (!string.IsNullOrWhiteSpace(MmprojPath))
		{
			args.Add("--mmproj \"" + MmprojPath + "\"");
		}
		if (draftModelActive && !string.IsNullOrWhiteSpace(DraftModelPath))
		{
			args.Add("-md \"" + DraftModelPath + "\"");
		}
		if (draftModelActive && DraftModelLocalOnly && CanPinDraftModelToLocal() && !string.IsNullOrWhiteSpace(GetDraftLocalDevice()))
		{
			args.Add("--spec-draft-device " + GetDraftLocalDevice());
		}
		string effectiveNgl = GetEffectiveGpuLayers();
		if (!string.IsNullOrWhiteSpace(effectiveNgl))
		{
			args.Add("-ngl " + effectiveNgl);
		}
		if (Threads > 0)
		{
			args.Add($"--threads {Threads}");
		}
		if (ThreadsBatch > 0)
		{
			args.Add($"--threads-batch {ThreadsBatch}");
		}
		if (MainGpu > 0 && !(SelectedGpuIndices.Length > 1 && TensorSplit.Length != 0))
		{
			args.Add($"--main-gpu {MainGpu}");
		}
		if (SelectedGpuIndices.Length != 0 && !string.IsNullOrWhiteSpace(deviceBackendPrefix))
		{
			string deviceList = BuildSelectedDeviceList(deviceBackendPrefix);
			if (!string.IsNullOrWhiteSpace(deviceList))
			{
				args.Add("--device " + deviceList);
			}
		}
		if (!FlashAttention)
		{
			args.Add("-fa off");
		}
		if (!Mmap)
		{
			args.Add("--no-mmap");
		}
		if (Mlock)
		{
                args.Add("--load-mode mmap+mlock");
		}
		if (!KvOffloadEnabled)
		{
			args.Add("-nkvo");
		}
		if (ContextSize != 0)
		{
			args.Add($"-c {ContextSize}");
		}
		if (BatchSize != 2048)
		{
			args.Add($"--batch {BatchSize}");
		}
		if (UbatchSize != 512)
		{
			args.Add($"--ubatch {UbatchSize}");
		}
		if (MaxTokens > 0)
		{
			args.Add($"-n {MaxTokens}");
		}
		if (CacheTypeK != CacheTypeK.F16)
		{
			args.Add("--cache-type-k " + CacheTypeK.ToString().ToLowerInvariant());
		}
		if (CacheTypeV != CacheTypeV.F16)
		{
			args.Add("--cache-type-v " + CacheTypeV.ToString().ToLowerInvariant());
		}
		if (CacheRam >= 0)
		{
			args.Add($"--cache-ram {CacheRam}");
		}
		if (CacheReuse != 0)
		{
			args.Add($"--cache-reuse {CacheReuse}");
		}
		if (Math.Abs(Temperature - 0.8f) > 1E-06f)
		{
			args.Add("--temp " + F(Temperature));
		}
		if (TopK != 40)
		{
			args.Add($"--top-k {TopK}");
		}
		if (Math.Abs(TopP - 0.95f) > 1E-06f)
		{
			args.Add("--top-p " + F(TopP));
		}
		if (Math.Abs(MinP - 0.05f) > 1E-06f)
		{
			args.Add("--min-p " + F(MinP));
		}
		if (Math.Abs(TypicalP - 1f) > 1E-06f)
		{
			args.Add("--typical-p " + F(TypicalP));
		}
		if (Math.Abs(RepeatPenalty - 1.1f) > 1E-06f)
		{
			args.Add("--repeat-penalty " + F(RepeatPenalty));
		}
		if (RepeatLastN != 64)
		{
			args.Add($"--repeat-last-n {RepeatLastN}");
		}
		if (PresencePenalty != 0f)
		{
			args.Add("--presence-penalty " + F(PresencePenalty));
		}
		if (FrequencyPenalty != 0f)
		{
			args.Add("--frequency-penalty " + F(FrequencyPenalty));
		}
		if (Seed >= 0)
		{
			args.Add($"--seed {Seed}");
		}
		if (Mirostat > MirostatMode.Disabled)
		{
			args.Add($"--mirostat {Mirostat}");
			args.Add("--mirostat-lr " + F(MirostatEta));
			args.Add("--mirostat-ent " + F(MirostatTau));
		}
		if (DryMultiplier > 0f)
		{
			args.Add("--dry-multiplier " + F(DryMultiplier));
		}
		if (Math.Abs(DryBase - 1.75f) > 1E-06f)
		{
			args.Add("--dry-base " + F(DryBase));
		}
		if (DynatempStddev > 0f)
		{
			args.Add("--dynatemp-range " + F(DynatempStddev));
		}
		if (XtcProbability > 0f)
		{
			args.Add("--xtc-probability " + F(XtcProbability));
		}
		if (XtcThreshold > 0f)
		{
			args.Add("--xtc-threshold " + F(XtcThreshold));
		}
		if (speculativeActive)
		{
			args.Add("--spec-type " + effectiveSpecType);
		}
		if (PredictCount > 0)
		{
			args.Add($"--predict {PredictCount}");
		}
		if (Jinja)
			args.Add("--jinja");
		if (draftModelActive && !string.IsNullOrWhiteSpace(SpecDraftGpuLayers) && int.TryParse(SpecDraftGpuLayers, out var _))
		{
			args.Add("--ngld " + SpecDraftGpuLayers);
		}
		if (speculativeActive && (SpecDraftNMax != 3 || (draftModelActive && !string.IsNullOrWhiteSpace(DraftModelPath))))
		{
			args.Add($"--spec-draft-n-max {SpecDraftNMax}");
		}
		if (speculativeActive && SpecDraftNMin != 0)
		{
			args.Add($"--spec-draft-n-min {SpecDraftNMin}");
		}
		if (speculativeActive && Math.Abs(SpecDraftPSplit - 0.1f) > 1E-06f)
		{
			args.Add("--spec-draft-p-split " + F(SpecDraftPSplit));
		}
		if (speculativeActive && SpecDraftPMin != 0f)
		{
			args.Add("--spec-draft-p-min " + F(SpecDraftPMin));
		}
		if (!string.IsNullOrWhiteSpace(RopeScaling))
		{
			args.Add("--rope-scaling " + RopeScaling.ToLowerInvariant());
		}
		if (RopeFreqBase.HasValue && RopeFreqBase.Value > 0.0)
		{
			args.Add("--rope-freq-base " + D(RopeFreqBase.Value));
		}
		if (RopeFreqScale.HasValue && RopeFreqScale.Value > 0.0)
		{
			args.Add("--rope-freq-scale " + D(RopeFreqScale.Value));
		}
		if (YarnOriginalContext.HasValue)
		{
			args.Add($"--yarn-orig-ctx {YarnOriginalContext.Value}");
		}
		if (Numa)
		{
			args.Add("--numa distribute");
		}
		if (!CachePrompt)
		{
			args.Add("--no-cache-prompt");
		}
		if (!ContBatching)
		{
			args.Add("-nocb");
		}
		if (!ContextShift)
		{
			args.Add("--no-context-shift");
		}
		if (VerboseLogging)
		{
			args.Add("--verbose");
		}
		if (!EnableWebUI)
		{
			args.Add("--no-ui");
		}
		if (EnableMetrics)
		{
			args.Add("--metrics");
		}
		if (Reasoning)
		{
			args.Add("-rea on");
		}
		if (Reasoning && ReasoningBudget > 0)
		{
			args.Add($"--reasoning-budget {ReasoningBudget}");
		}
		if (EmbeddingMode)
		{
			args.Add("--embedding");
		}
		if (!string.IsNullOrEmpty(Host) && Host != "127.0.0.1")
		{
			args.Add("--host " + Host);
		}
		if (Port != 8080)
		{
			args.Add($"--port {Port}");
		}
		if (Timeout != 3600)
		{
			args.Add($"--timeout {Timeout}");
		}
		if (Slots > 0)
		{
			args.Add($"-np {Slots}");
		}
		if (!EnableSlots)
		{
			args.Add("--no-slots");
		}
		foreach (var (argName, argValue) in CustomArguments)
		{
			if (string.IsNullOrEmpty(argValue))
			{
				args.Add(argName);
			}
			else
			{
				args.Add(argName + " " + argValue);
			}
		}
		return string.Join(" ", args);
	}

	private void AddRemainingCustomArgs(List<string> args, Dictionary<string, string> customArgs, HashSet<string> customFlags)
	{
		HashSet<string> knownArgNames = CommandLineParser.KnownArgumentsLookup.Values.Select((ArgumentMapping m) => m.Canonical).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> customArg in customArgs)
		{
			customArg.Deconstruct(out var key, out var argValue);
			string argName = key;
			string argValue2 = argValue;
			ArgumentMapping argumentMapping = CommandLineParser.KnownArgumentsLookup.GetValueOrDefault(argName);
			if (argumentMapping != null)
			{
				args.Add(argumentMapping.Canonical + " " + QuoteValue(argValue2));
			}
			else if (knownArgNames.Contains(argName))
			{
				args.Add(argName + " " + QuoteValue(argValue2));
			}
			else
			{
				args.Add(CommandLineParser.NormalizeSpecialCharacters(argName) + " " + QuoteValue(argValue2));
			}
		}
		foreach (string customFlag in customFlags)
		{
			ArgumentMapping argumentMapping2 = CommandLineParser.KnownArgumentsLookup.GetValueOrDefault(customFlag);
			if (argumentMapping2 != null)
			{
				args.Add(argumentMapping2.Canonical);
			}
			else if (knownArgNames.Contains(customFlag))
			{
				args.Add(customFlag);
			}
			else
			{
				args.Add(CommandLineParser.NormalizeSpecialCharacters(customFlag));
			}
		}
	}

	public override string ToString()
	{
		return Name;
	}

	public override bool Equals(object? other)
	{
		if (other is ServerProfile serverProfile)
		{
			return Id == serverProfile.Id;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Id?.GetHashCode() ?? 0;
	}

	private static string QuoteValue(string rawValue)
	{
		return "\"" + rawValue.Replace("\"", "\\\"") + "\"";
	}
}
