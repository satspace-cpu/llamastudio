using System.Collections.Generic;
using System.Linq;

namespace LlamaStudio.Core.Models;

/// <summary>
/// Represents parsed help output from llama-server --help.
/// Contains the set of supported CLI arguments for the current build.
/// </summary>
public class LlamaHelpInfo
{
    /// <summary>
    /// All recognized argument flags (e.g., "--temp", "-ngl", "--flash-attn").
    /// </summary>
    public HashSet<string> SupportedFlags { get; set; } = new();

    /// <summary>
    /// Raw help output text.
    /// </summary>
    public string RawOutput { get; set; } = string.Empty;

    /// <summary>
    /// Structured flag info extracted from --help (name, description, default value).
    /// Populated by enhanced parser.
    /// </summary>
    public Dictionary<string, CliFlagInfo> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
        /// Whether the flag is supported by current llama.cpp build.
        /// Checks against known aliases (e.g., "flash-attn" matches "--flash-attn", "-fa").
        /// </summary>
        public bool IsSupported(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return true;

            // Normalize: strip leading dashes
            string normalized = flag.TrimStart('-');

            foreach (var supported in SupportedFlags)
            {
                string sNormalized = supported.TrimStart('-');
                if (sNormalized.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Also check known alias mappings: try matching by common names
            var aliasMap = new Dictionary<string, string[]>
            {
                { "cache-ram", new[] { "cram", "cache-ram" } },
                { "cache-reuse", new[] { "cache-reuse" } },
                { "cache-type-k", new[] { "ctk", "cache-type-k" } },
                { "cache-type-v", new[] { "ctv", "cache-type-v" } },
                { "flash-attn", new[] { "fa", "flash-attn", "flash-attention" } },
                { "gpu-layers", new[] { "ngl", "gpu-layers" } },
                { "ctx-size", new[] { "c", "ctx-size" } },
            };

            foreach (var (_, aliases) in aliasMap)
            {
                if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if any of these aliases are in SupportedFlags
                    foreach (var supported in SupportedFlags)
                    {
                        string sNormalized2 = supported.TrimStart('-');
                        if (aliases.Any(a => sNormalized2.Equals(a, StringComparison.OrdinalIgnoreCase)))
                            return true;
                    }
                }
            }

            return false;
        }

   /// <summary>
        /// Check multiple flags. Returns list of unsupported ones.
        /// </summary>
        public List<string> GetUnsupported(IEnumerable<string> flags)
        {
            var unsupported = new List<string>();
            foreach (var flag in flags)
            {
                // Extract just the flag name, strip value (e.g. "--cache-ram 24000" → "--cache-ram")
                string flagName = flag.Split(' ')[0];
                if (!IsSupported(flagName))
                    unsupported.Add(flag);
            }
            return unsupported;
        }
}
