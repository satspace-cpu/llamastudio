namespace LlamaStudio.Core.Models;

/// <summary>
/// A flag that our application generates but is no longer present in the current llama.cpp binary.
/// </summary>
public class RemovedFlag
{
    public string OurFlag { get; set; } = string.Empty;
    public string? SuggestedReplacement { get; set; }
}
