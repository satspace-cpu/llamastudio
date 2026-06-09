using System.Text.Json.Serialization;

namespace LlamaStudio.Core.Models;

public class GgufModelInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("modelType")]
    public Core.Enums.ModelType ModelType { get; set; }

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("quantization")]
    public Core.Enums.QuantizationType Quantization { get; set; }

    [JsonPropertyName("quantizationTag")]
    public string QuantizationTag { get; set; } = string.Empty;

    [JsonPropertyName("contextSize")]
    public int ContextSize { get; set; }

    [JsonPropertyName("embeddingSize")]
    public int EmbeddingSize { get; set; }

    [JsonPropertyName("blockCount")]
    public int BlockCount { get; set; }

    [JsonPropertyName("headCount")]
    public int HeadCount { get; set; }

    [JsonPropertyName("headCount Kv")]
    public int HeadCountKv { get; set; }

    [JsonPropertyName("layerCount")]
    public int LayerCount { get; set; }

    [JsonPropertyName("vocabSize")]
    public int VocabSize { get; set; }

    [JsonPropertyName("estimatedVramGb")]
    public double EstimatedVramGb { get; set; }

    [JsonPropertyName("ropeFreqBase")]
    public double? RopeFreqBase { get; set; }

    [JsonPropertyName("ropeFreqScale")]
    public double? RopeFreqScale { get; set; }

    [JsonPropertyName("yarnOriginalContext")]
    public int? YarnOriginalContext { get; set; }

    [JsonPropertyName("hasMtp")]
    public bool HasMtp { get; set; }

    [JsonPropertyName("mtpDepth")]
    public int MtpDepth { get; set; }

    [JsonPropertyName("isVision")]
    public bool IsVision { get; set; }

    [JsonPropertyName("visionImageSize")]
    public int VisionImageSize { get; set; }

    [JsonPropertyName("requiresMmproj")]
    public bool RequiresMmproj { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("tags")]
    public HashSet<string> Tags { get; set; } = new();

    // UI state (not serialized)
    public bool IsMmprojFile => FileName.Contains("mmproj", StringComparison.OrdinalIgnoreCase);
    public bool IsServerModelConnected { get; set; }
    public bool IsServerMmprojConnected { get; set; }
}
