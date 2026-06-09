using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Llama;

public class ModelScanner : IModelScanner
{
    readonly ILogService _log;

    public ModelScanner(ILogService log)
    {
        _log = log;
    }

    async Task<List<GgufModelInfo>> IModelScanner.ScanDirectoryAsync(string directory, CancellationToken ct)
    {
        if (!Directory.Exists(directory))
        {
            _log.Error($"Directory not found: {directory}", "ModelScanner");
            return new();
        }

        var models = new List<GgufModelInfo>();

        var files = Directory.GetFiles(directory, "*.gguf", SearchOption.AllDirectories);

        _log.Information($"Scanning {files.Length} GGUF files in {directory}", "ModelScanner");

        var tasks = files.Select(async f =>
        {
            if (ct.IsCancellationRequested) return null;
            try
            {
                return await ((IModelScanner)this).AnalyzeModelAsync(f, ct);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to analyze: {f}", "ModelScanner");
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        models.AddRange(results.Where(m => m != null).Cast<GgufModelInfo>());

        _log.Information($"Found {models.Count} valid models", "ModelScanner");
        return models;
    }

    async Task<GgufModelInfo> IModelScanner.AnalyzeModelAsync(string modelPath, CancellationToken ct)
    {
        return await Task.Run(() => AnalyzeModelSync(modelPath), ct);
    }

    GgufModelInfo AnalyzeModelSync(string modelPath)
    {
        var info = new GgufModelInfo
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
            _log.Warning($"Header parse failed for {info.FileName}: {ex.Message}", "ModelScanner");
            info.Architecture = "unknown";
            info.QuantizationTag = ExtractFileNameQuant(modelPath);
            info.Quantization = ParseQuantization(info.QuantizationTag);
            info.ModelType = ModelType.Chat;
            info.EstimatedVramGb = info.SizeBytes / (1024.0 * 1024.0 * 1024.0);
            return info;
        }

        info.Metadata = metadata;
        info.Architecture = ExtractMetadata(metadata, "general.architecture") ?? "unknown";

        info.QuantizationTag = ExtractFileNameQuant(modelPath);
        info.Quantization = ParseQuantization(info.QuantizationTag);

        if (int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.context_length"), out var ctx))
            info.ContextSize = ctx;

        if (int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.embedding_length"), out var emb))
            info.EmbeddingSize = emb;

        if (int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.block_count"), out var blocks))
        {
            info.BlockCount = blocks;
            info.LayerCount = blocks;
        }

        if (int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.attention.head_count"), out var heads))
            info.HeadCount = heads;

        if (int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.attention.head_count_kv"), out var kvHeads))
            info.HeadCountKv = kvHeads;

        if (int.TryParse(ExtractMetadata(metadata, "general.vocab_size"), out var vocab))
            info.VocabSize = vocab;

        info.RopeFreqBase = ParseDouble(ExtractMetadata(metadata, $"{info.Architecture}.rope.freq_base"));
        info.RopeFreqScale = ParseDouble(ExtractMetadata(metadata, $"{info.Architecture}.rope.freq_scale"));
        info.YarnOriginalContext = ParseInt(ExtractMetadata(metadata, $"{info.Architecture}.rope.yarn.original_context_length"));

        info.HasMtp = metadata.ContainsKey($"{info.Architecture}.attention.head_count_gate") ||
                      info.Architecture.Contains("mtp") ||
                      info.Architecture.Contains("MTP");

        if (info.HasMtp && int.TryParse(ExtractMetadata(metadata, $"{info.Architecture}.mtp_depth"), out var mtpDepth))
            info.MtpDepth = mtpDepth;

        info.IsVision = info.Architecture is "mllama" or "llava" or "moondream" or "pixtral" or "granite-vision" ||
                         metadata.ContainsKey("clip.vision.block_count");
        info.VisionImageSize = ParseInt(ExtractMetadata(metadata, "clip.vision.image_size")) ?? 0;
        info.RequiresMmproj = info.IsVision && info.Architecture != "mllama";

        info.ModelType = DetermineModelType(info, metadata);

        info.EstimatedVramGb = EstimateVramForModel(info);

        var authorTag = ExtractMetadata(metadata, "general.author");
        if (!string.IsNullOrEmpty(authorTag))
            info.Author = authorTag;

        return info;
    }

    Dictionary<string, object> ParseGgufHeader(string path)
    {
        var metadata = new Dictionary<string, object>();

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        using var br = new BinaryReader(fs);

        var magic = br.ReadUInt32();
        if (magic != 0x46554747u)
            throw new InvalidDataException($"Invalid GGUF file: magic number is 0x{magic:X8}, expected 0x46554747");

        var version = br.ReadUInt32();
        var fieldCount = version >= 2 ? br.ReadUInt64() : 0;
        var tensorCount = version >= 2 ? br.ReadUInt64() : 0;

        for (ulong i = 0; i < fieldCount; i++)
        {
            var key = ReadUtf8String(br);
            var type = (GgufType)br.ReadUInt32();
            var value = ReadGgufValue(br, type, path);

            if (value != null)
                metadata[key] = value;
        }

        return metadata;
    }

    string ReadUtf8String(BinaryReader br)
    {
        var len = br.ReadUInt64();
        if (len > 100_000_000) throw new InvalidDataException("String too long");
        var bytes = br.ReadBytes((int)len);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    object? ReadGgufValue(BinaryReader br, GgufType type, string path)
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
            _ => null
        };
    }

    object ReadGgufArray(BinaryReader br)
    {
        var type = (GgufType)br.ReadUInt32();
        var n = br.ReadUInt64();
        var items = new List<object?>();
        for (ulong i = 0; i < n; i++)
        {
            items.Add(ReadGgufValue(br, type, string.Empty));
        }
        return items;
    }

    byte[] ReadGgufBlob(BinaryReader br)
    {
        var len = br.ReadUInt64();
        if (len > 10_000_000) throw new InvalidDataException("Blob too large");
        return br.ReadBytes((int)len);
    }

    enum GgufType : uint
    {
        UINT8 = 0, INT8 = 1, UINT16 = 2, INT16 = 3,
        UINT32 = 4, INT32 = 5, FLOAT32 = 6,
        BOOL = 7, STRING = 8, ARRAY = 9,
        UINT64 = 10, INT64 = 11, FLOAT64 = 12,
        BLOB = 0xff
    }

    ModelType DetermineModelType(GgufModelInfo info, Dictionary<string, object> metadata)
    {
        if (info.IsVision) return ModelType.Vision;
        if (info.HasMtp) return ModelType.Mtp;

        var archStr = metadata.GetValueOrDefault("general.architecture")?.ToString() ?? "";
        if (info.Architecture == "bert" ||
            archStr.Contains("bert") ||
            metadata.ContainsKey("bert.pooling_type"))
            return ModelType.Embedding;

        if (metadata.ContainsKey("reranker"))
            return ModelType.Reranker;

        if (info.EmbeddingSize > 0 && info.ContextSize < 2048)
            return ModelType.Embedding;

        var description = (metadata.GetValueOrDefault("general.description")?.ToString() ?? "").ToLower();

        if (description.Contains("embedding") || description.Contains("embed"))
            return ModelType.Embedding;

        if (description.Contains("rerank"))
            return ModelType.Reranker;

        return ModelType.Chat;
    }

    static string? ExtractMetadata(Dictionary<string, object> metadata, string key)
    {
        return metadata.TryGetValue(key, out var val) ? val?.ToString() : null;
    }

    static double EstimateVramForModel(GgufModelInfo info)
    {
        if (info.SizeBytes <= 0) return 0;

        double baseGib = info.SizeBytes / (1024.0 * 1024.0 * 1024.0);

        double kvOverhead = 0;
        if (info.ContextSize > 0 && info.BlockCount > 0)
        {
            kvOverhead = (info.ContextSize * info.EmbeddingSize * 2 * info.BlockCount) /
                         (1024.0 * 1024.0 * 1024.0) * 0.001;
        }

        return Math.Round(baseGib + kvOverhead, 2);
    }

    string ExtractFileNameQuant(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).ToLower();

        foreach (var tag in new[] { "q8_0", "q6_k", "q5_k_m", "q5_k_s", "q5_1", "q5_0",
                                     "q4_k_m", "q4_1", "q4_0", "iq3_xxs", "iq2_xxs",
                                     "fp16", "fp32", "q2_k", "q3_k_m", "q3_k_s",
                                     "iq4_xs", "iq4_nl", "iq4_nn", "iq1_s" })
        {
            if (name.Contains(tag))
                return tag.ToUpper();
        }

        return "Q4_K_M";
    }

    QuantizationType ParseQuantization(string tag)
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
            _ => QuantizationType.Unknown
        };
    }

    static double? ParseDouble(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return double.TryParse(value, out var d) ? d : null;
    }

    static int? ParseInt(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return int.TryParse(value, out var i) ? i : null;
    }

    string IModelScanner.FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824L => $"{bytes / 1_073_741_824.0:F2} GiB",
            >= 1_048_576L => $"{bytes / 1_048_576.0:F2} MiB",
            >= 1_024L => $"{bytes / 1_024.0:F2} KiB",
            _ => $"{bytes} B"
        };
    }

    double IModelScanner.EstimateVramUsage(GgufModelInfo model, int gpuLayers)
    {
        if (gpuLayers < 0 || gpuLayers >= model.BlockCount)
            return model.EstimatedVramGb;

        double totalGib = model.SizeBytes / (1024.0 * 1024.0 * 1024.0);
        double perLayer = totalGib / (model.BlockCount > 0 ? model.BlockCount : 1);
        double gpuVram = perLayer * gpuLayers;

        double kvCache = (model.ContextSize * model.EmbeddingSize * 2) /
                         (1024.0 * 1024.0 * 1024.0) * 0.001;

        return Math.Round(gpuVram + kvCache, 2);
    }
}
