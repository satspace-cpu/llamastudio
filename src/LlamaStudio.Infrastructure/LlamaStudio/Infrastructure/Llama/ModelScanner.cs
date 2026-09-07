using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Llama;

public class ModelScanner : IModelScanner
{
	private enum GgufType : uint
	{
		UINT8 = 0u,
		INT8 = 1u,
		UINT16 = 2u,
		INT16 = 3u,
		UINT32 = 4u,
		INT32 = 5u,
		FLOAT32 = 6u,
		BOOL = 7u,
		STRING = 8u,
		ARRAY = 9u,
		UINT64 = 10u,
		INT64 = 11u,
		FLOAT64 = 12u,
		BLOB = 255u
	}

	private readonly ILogService _log;

	public ModelScanner(ILogService log)
	{
		_log = log;
	}

	async Task<List<GgufModelInfo>> IModelScanner.ScanDirectoryAsync(string directory, CancellationToken ct)
	{
		if (!Directory.Exists(directory))
		{
			_log.Error("Directory not found: " + directory, "ModelScanner");
			return new List<GgufModelInfo>();
		}
		List<GgufModelInfo> models = new List<GgufModelInfo>();
		string[] files = Directory.GetFiles(directory, "*.gguf", SearchOption.AllDirectories);
		_log.Information($"Scanning {files.Length} GGUF files in {directory}", "ModelScanner");
		models.AddRange((await Task.WhenAll(files.Select(async delegate(string f)
		{
			if (ct.IsCancellationRequested)
			{
				return (GgufModelInfo)null;
			}
			try
			{
				return await ((IModelScanner)this).AnalyzeModelAsync(f, ct);
			}
			catch (Exception exception)
			{
				_log.Error(exception, "Failed to analyze: " + f, "ModelScanner");
				return (GgufModelInfo)null;
			}
		}))).Where((GgufModelInfo m) => m != null).Cast<GgufModelInfo>());
		_log.Information($"Found {models.Count} valid models", "ModelScanner");
		return models;
	}

	async Task<GgufModelInfo> IModelScanner.AnalyzeModelAsync(string modelPath, CancellationToken ct)
	{
		return await Task.Run(() => AnalyzeModelSync(modelPath), ct);
	}

	private GgufModelInfo AnalyzeModelSync(string modelPath)
	{
		GgufModelInfo modelInfo = new GgufModelInfo
		{
			Path = modelPath,
			FileName = Path.GetFileName(modelPath),
			FullName = modelPath,
			SizeBytes = new FileInfo(modelPath).Length
		};
		Dictionary<string, object> metadata;
		try
		{
			metadata = ParseGgufHeader(modelPath);
		}
		catch (Exception ex)
		{
			_log.Warning("Header parse failed for " + modelInfo.FileName + ": " + ex.Message, "ModelScanner");
			modelInfo.Architecture = "unknown";
			modelInfo.QuantizationTag = ExtractFileNameQuant(modelPath);
			modelInfo.Quantization = ParseQuantization(modelInfo.QuantizationTag);
			modelInfo.ModelType = ModelType.Chat;
			modelInfo.EstimatedVramGb = (double)modelInfo.SizeBytes / 1073741824.0;
			return modelInfo;
		}
		modelInfo.Metadata = metadata;
		modelInfo.Architecture = ExtractMetadata(metadata, "general.architecture") ?? "unknown";
		modelInfo.QuantizationTag = ExtractFileNameQuant(modelPath);
		modelInfo.Quantization = ParseQuantization(modelInfo.QuantizationTag);
		if (int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".context_length"), out var contextSize))
		{
			modelInfo.ContextSize = contextSize;
		}
		if (int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".embedding_length"), out var embeddingSize))
		{
			modelInfo.EmbeddingSize = embeddingSize;
		}
		if (int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".block_count"), out var blockCount))
		{
			modelInfo.BlockCount = blockCount;
			modelInfo.LayerCount = blockCount;
		}
		if (int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".attention.head_count"), out var headCount))
		{
			modelInfo.HeadCount = headCount;
		}
		if (int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".attention.head_count_kv"), out var headCountKv))
		{
			modelInfo.HeadCountKv = headCountKv;
		}
		if (int.TryParse(ExtractMetadata(metadata, "general.vocab_size"), out var vocabSize))
		{
			modelInfo.VocabSize = vocabSize;
		}
		modelInfo.RopeFreqBase = ParseDouble(ExtractMetadata(metadata, modelInfo.Architecture + ".rope.freq_base"));
		modelInfo.RopeFreqScale = ParseDouble(ExtractMetadata(metadata, modelInfo.Architecture + ".rope.freq_scale"));
		modelInfo.YarnOriginalContext = ParseInt(ExtractMetadata(metadata, modelInfo.Architecture + ".rope.yarn.original_context_length"));
		modelInfo.HasMtp = metadata.ContainsKey(modelInfo.Architecture + ".attention.head_count_gate") || modelInfo.Architecture.Contains("mtp") || modelInfo.Architecture.Contains("MTP");
		if (modelInfo.HasMtp && int.TryParse(ExtractMetadata(metadata, modelInfo.Architecture + ".mtp_depth"), out var mtpDepth))
		{
			modelInfo.MtpDepth = mtpDepth;
		}
		GgufModelInfo modelInfo2 = modelInfo;
		bool isVision;
		switch (modelInfo.Architecture)
		{
		case "mllama":
		case "llava":
		case "moondream":
		case "pixtral":
		case "granite-vision":
			isVision = true;
			break;
		default:
			isVision = false;
			break;
		}
		modelInfo2.IsVision = isVision || metadata.ContainsKey("clip.vision.block_count");
		modelInfo.VisionImageSize = ParseInt(ExtractMetadata(metadata, "clip.vision.image_size")).GetValueOrDefault();
		modelInfo.RequiresMmproj = modelInfo.IsVision && modelInfo.Architecture != "mllama";
		modelInfo.ModelType = DetermineModelType(modelInfo, metadata);
		modelInfo.EstimatedVramGb = EstimateVramForModel(modelInfo);
		string author = ExtractMetadata(metadata, "general.author");
		if (!string.IsNullOrEmpty(author))
		{
			modelInfo.Author = author;
		}
		return modelInfo;
	}

	private Dictionary<string, object> ParseGgufHeader(string path)
	{
		Dictionary<string, object> metadata = new Dictionary<string, object>();
		using FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
		using BinaryReader binaryReader = new BinaryReader(input);
		uint magicNumber = binaryReader.ReadUInt32();
		if (magicNumber != 1179993927)
		{
			throw new InvalidDataException($"Invalid GGUF file: magic number is 0x{magicNumber:X8}, expected 0x46554747");
		}
		uint version = binaryReader.ReadUInt32();
		ulong metadataCount = ((version >= 2) ? binaryReader.ReadUInt64() : 0);
		if (version >= 2)
		{
			binaryReader.ReadUInt64();
		}
		for (ulong i = 0uL; i < metadataCount; i++)
		{
			string key = ReadUtf8String(binaryReader);
			GgufType type = (GgufType)binaryReader.ReadUInt32();
			object metadataValue = ReadGgufValue(binaryReader, type, path);
			if (metadataValue != null)
			{
				metadata[key] = metadataValue;
			}
		}
		return metadata;
	}

	private string ReadUtf8String(BinaryReader br)
	{
		ulong length = br.ReadUInt64();
		if (length > 100000000)
		{
			throw new InvalidDataException("String too long");
		}
		byte[] bytes = br.ReadBytes((int)length);
		return Encoding.UTF8.GetString(bytes);
	}

	private object? ReadGgufValue(BinaryReader br, GgufType type, string path)
	{
		return type switch
		{
			GgufType.UINT8 => br.ReadByte(), 
			GgufType.INT8 => br.ReadSByte(), 
			GgufType.UINT16 => br.ReadUInt16(), 
			GgufType.INT16 => br.ReadInt16(), 
			GgufType.UINT32 => br.ReadUInt32(), 
			GgufType.INT32 => br.ReadInt32(), 
			GgufType.FLOAT32 => br.ReadSingle(), 
			GgufType.UINT64 => br.ReadUInt64(), 
			GgufType.INT64 => br.ReadInt64(), 
			GgufType.FLOAT64 => br.ReadDouble(), 
			GgufType.BOOL => br.ReadBoolean(), 
			GgufType.STRING => ReadUtf8String(br), 
			GgufType.ARRAY => ReadGgufArray(br), 
			GgufType.BLOB => ReadGgufBlob(br), 
			_ => null, 
		};
	}

	private object ReadGgufArray(BinaryReader br)
	{
		GgufType type = (GgufType)br.ReadUInt32();
		ulong elementCount = br.ReadUInt64();
		List<object> values = new List<object>();
		for (ulong i = 0uL; i < elementCount; i++)
		{
			values.Add(ReadGgufValue(br, type, string.Empty));
		}
		return values;
	}

	private byte[] ReadGgufBlob(BinaryReader br)
	{
		ulong size = br.ReadUInt64();
		if (size > 10000000)
		{
			throw new InvalidDataException("Blob too large");
		}
		return br.ReadBytes((int)size);
	}

	private ModelType DetermineModelType(GgufModelInfo info, Dictionary<string, object> metadata)
	{
		if (info.IsVision)
		{
			return ModelType.Vision;
		}
		if (info.HasMtp)
		{
			return ModelType.Mtp;
		}
		string architecture = metadata.GetValueOrDefault("general.architecture")?.ToString() ?? "";
		if (info.Architecture == "bert" || architecture.Contains("bert") || metadata.ContainsKey("bert.pooling_type"))
		{
			return ModelType.Embedding;
		}
		if (metadata.ContainsKey("reranker"))
		{
			return ModelType.Reranker;
		}
		if (info.EmbeddingSize > 0 && info.ContextSize < 2048)
		{
			return ModelType.Embedding;
		}
		string description = (metadata.GetValueOrDefault("general.description")?.ToString() ?? "").ToLower();
		if (description.Contains("embedding") || description.Contains("embed"))
		{
			return ModelType.Embedding;
		}
		if (description.Contains("rerank"))
		{
			return ModelType.Reranker;
		}
		return ModelType.Chat;
	}

	private static string? ExtractMetadata(Dictionary<string, object> metadata, string key)
	{
		if (!metadata.TryGetValue(key, out object value))
		{
			return null;
		}
		return value?.ToString();
	}

	private static double EstimateVramForModel(GgufModelInfo info)
	{
		if (info.SizeBytes <= 0)
		{
			return 0.0;
		}
		double weightsGb = (double)info.SizeBytes / 1073741824.0;
		double kvCacheGb = 0.0;
		if (info.ContextSize > 0 && info.BlockCount > 0)
		{
			kvCacheGb = (double)(info.ContextSize * info.EmbeddingSize * 2 * info.BlockCount) / 1073741824.0 * 0.001;
		}
		return Math.Round(weightsGb + kvCacheGb, 2);
	}

	private string ExtractFileNameQuant(string path)
	{
		string fileNameLower = Path.GetFileNameWithoutExtension(path).ToLower();
		string[] quantTags = new string[20]
		{
			"q8_0", "q6_k", "q5_k_m", "q5_k_s", "q5_1", "q5_0", "q4_k_m", "q4_1", "q4_0", "iq3_xxs",
			"iq2_xxs", "fp16", "fp32", "q2_k", "q3_k_m", "q3_k_s", "iq4_xs", "iq4_nl", "iq4_nn", "iq1_s"
		};
		foreach (string tag in quantTags)
		{
			if (fileNameLower.Contains(tag))
			{
				return tag.ToUpper();
			}
		}
		return "Q4_K_M";
	}

	private QuantizationType ParseQuantization(string tag)
	{
		return tag.ToUpper() switch
		{
			"Q4_0" => QuantizationType.Q4_0, 
			"Q4_1" => QuantizationType.Q4_1, 
			"Q5_0" => QuantizationType.Q5_0, 
			"Q5_1" => QuantizationType.Q5_1, 
			"Q5_K_S" => QuantizationType.Q5_K_S, 
			"Q5_K_M" => QuantizationType.Q5_K_M, 
			"Q5_K_L" => QuantizationType.Q5_K_L, 
			"Q6_K" => QuantizationType.Q6_K, 
			"Q8_0" => QuantizationType.Q8_0, 
			"FP16" => QuantizationType.FP16, 
			"FP32" => QuantizationType.FP32, 
			"IQ2_XXS" => QuantizationType.IQ2_XXS, 
			"IQ2_XS" => QuantizationType.IQ2_XS, 
			"IQ3_XXS" => QuantizationType.IQ3_XXS, 
			_ => QuantizationType.Unknown, 
		};
	}

	private static double? ParseDouble(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		if (!double.TryParse(value, out var parsed))
		{
			return null;
		}
		return parsed;
	}

	private static int? ParseInt(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		if (!int.TryParse(value, out var parsed))
		{
			return null;
		}
		return parsed;
	}

	string IModelScanner.FormatSize(long bytes)
	{
		if (bytes >= 1048576)
		{
			if (bytes >= 1073741824)
			{
				return $"{(double)bytes / 1073741824.0:F2} GiB";
			}
			return $"{(double)bytes / 1048576.0:F2} MiB";
		}
		if (bytes >= 1024)
		{
			return $"{(double)bytes / 1024.0:F2} KiB";
		}
		return $"{bytes} B";
	}

	double IModelScanner.EstimateVramUsage(GgufModelInfo model, int gpuLayers)
	{
		if (gpuLayers < 0 || gpuLayers >= model.BlockCount)
		{
			return model.EstimatedVramGb;
		}
		double weightsGb = (double)model.SizeBytes / 1073741824.0 / (double)((model.BlockCount <= 0) ? 1 : model.BlockCount) * (double)gpuLayers;
		double kvCacheGb = (double)(model.ContextSize * model.EmbeddingSize * 2) / 1073741824.0 * 0.001;
		return Math.Round(weightsGb + kvCacheGb, 2);
	}
}
