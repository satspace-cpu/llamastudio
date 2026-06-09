namespace LlamaStudio.Core.Enums;

public enum ModelType
{
    Chat,
    Completion,
    Embedding,
    Reranker,
    Vision,
    Mtp,
    Unknown
}

public enum QuantizationType
{
    Unknown,
    Q4_0,
    Q4_1,
    Q5_0,
    Q5_1,
    Q5_K_S,
    Q5_K_M,
    Q5_K_L,
    Q6_K,
    Q8_0,
    IQ2_XXS,
    IQ2_XS,
    IQ2_S,
    IQ2_M,
    IQ3_XXS,
    IQ3_XS,
    IQ3_S,
    IQ3_M,
    IQ4_XS,
    IQ4_SSL,
    IQ4_NL,
    FP16,
    FP32
}

public enum GpuSplitMode
{
    None,
    Layer,
    Row
}

public enum CacheTypeK
{
    F32,
    F16,
    Q8_0,
    Q4_0
}

public enum CacheTypeV
{
    F32,
    F16,
    Q8_0,
    Q4_0
}

public enum ServerState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Fatal
}

public enum SamplerStrategy
{
    Default,
    Grammar,
    Dry,
    Mirostat,
    Dynatemp,
    Xtc
}

public enum UpdateChannel
{
    Stable,
    PreRelease,
    Nightly
}

public enum MirostatMode
{
    Disabled = 0,
    Mode1 = 1,
    Mode2 = 2
}

public enum McpTransportType
{
    Stdio,
    Sse
}
