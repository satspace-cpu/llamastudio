using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LlamaStudio.Infrastructure.Llama;

/// <summary>
/// Parses llama-server --help output to detect supported CLI arguments.
/// </summary>
public class HelpParser : IHelpParser
{
    /// <summary>
    /// Regex patterns to match argument definitions in help text.
    /// Matches lines like:
    ///   -m MODEL, --model MODEL              Load model
    ///   --temp FLOAT                         Temperature (default: 0.8)
    ///   --flash-attn                        Enable Flash Attention
    /// </summary>
    static readonly Regex[] _argPatterns = new[]
    {
        // Pattern: "--flag" or "-f" at start of line, possibly with type hint
        new Regex(@"^[\s]*(--?\w[\w-]*)", RegexOptions.Multiline | RegexOptions.Compiled),
        // Pattern: "  -m MODEL, --model MODEL" style (comma-separated aliases)
        new Regex(@"[-](\w[\w-]*(?:\s+\w+)?)\s*,\s*(--\w[\w-]*(?:\s+\w+)?)", RegexOptions.Compiled),
    };

    /// <summary>
    /// Known argument name mappings for normalization.
    /// Maps common aliases to canonical form.
    /// </summary>
    static readonly Dictionary<string, string> _knownAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "m", "--model" }, { "model", "--model" },
        { "ngl", "-ngl" }, { "gpu-layers", "-ngl" },
        { "fa", "-fa" }, { "flash-attn", "-fa" }, { "flash-attention", "-fa" },
        { "c", "-c" }, { "ctx-size", "-c" },
        { "t", "-t" }, { "threads", "-t" },
        { "tb", "--threads-batch" }, { "threads-batch", "--threads-batch" },
        { "b", "--batch" }, { "batch", "--batch" },
        { "ubatch", "--ubatch" },
        { "temp", "--temp" }, { "temperature", "--temp" },
        { "top-k", "--top-k" }, { "topk", "--top-k" },
        { "top-p", "--top-p" }, { "topp", "--top-p" },
        { "min-p", "--min-p" }, { "minp", "--min-p" },
        { "typical-p", "--typical-p" },
        { "repeat-penalty", "--repeat-penalty" },
        { "repeat-last-n", "--repeat-last-n" },
        { "presence-penalty", "--presence-penalty" },
        { "frequency-penalty", "--frequency-penalty" },
        { "mirostat", "--mirostat" },
        { "mirostat-tau", "--mirostat-tau" },
        { "mirostat-eta", "--mirostat-learn-rate" }, { "mirostat-learn-rate", "--mirostat-learn-rate" },
        { "dry-multiplier", "--dry-multiplier" },
        { "dry-base", "--dry-base" },
        { "dynatemp-range", "--dynatemp-range" },
        { "xtc-probability", "--xtc-probability" },
        { "xtc-threshold", "--xtc-threshold" },
        { "speculative", "--speculative" },
        { "spec-type", "--spec-type" },
        { "ngld", "-ngld" },
        { "spec-draft-n-max", "--spec-draft-n-max" },
        { "spec-draft-n-min", "--spec-draft-n-min" },
        { "spec-draft-p-split", "--spec-draft-p-split" },
        { "spec-draft-p-min", "--spec-draft-p-min" },
        { "rope-scaling", "--rope-scaling" },
        { "rope-frequency-base", "--rope-frequency-base" },
        { "rope-frequency-scale", "--rope-frequency-scale" },
        { "yarn-orig-ctx", "--yarn-orig-ctx" },
        { "yarn-ext-factor", "--yarn-ext-factor" },
        { "yarn-attn-factor", "--yarn-attn-factor" },
        { "yarn-beta-fast", "--yarn-beta-fast" },
        { "yarn-beta-slow", "--yarn-beta-slow" },
        { "numa", "--numa" },
        { "cache-prompt", "--cache-prompt" },
        { "cont-batching", "--cont-batching" },
        { "verbose", "--verbose" },
        { "ui", "--ui" },
        { "metrics", "--metrics" },
        { "reasoning", "-rea" }, { "rea", "-rea" },
        { "embedding", "--embedding" },
        { "pooling", "--pooling" },
        { "mmproj", "--mmproj" },
        { "draft-model", "--draft-model" },
        { "host", "--host" },
        { "port", "--port" },
        { "timeout", "-to" },
        { "np", "-np" }, { "parallel", "-np" },
        { "slots", "--slots" },
        { "predict", "--predict" },
        { "max-tokens", "--max-tokens" },
        { "seed", "-s" },
        { "mmap", "--mmap" },
        { "mlock", "--mlock" },
        { "no-mmq", "--no-mmq" },
        { "no-kv-offload", "--no-kv-offload" },
        { "main-gpu", "--main-gpu" },
        { "gpu-split", "--gpu-split" },
        { "tensor-split", "--tensor-split" },
        { "cache-type-k", "--cache-type-k" },
        { "cache-type-v", "--cache-type-v" },
        { "cram", "--cache-ram" }, { "cache-ram", "--cache-ram" },
        { "cache-reuse", "--cache-reuse" },
        // --priority-high removed: not a llama.cpp flag, handled by ServerManager
        { "reasoning-budget", "--reasoning-budget" },
    };

    public async Task<LlamaHelpInfo?> ParseAsync(string serverExePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverExePath) || !File.Exists(serverExePath))
            return null;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExePath,
                Arguments = "--help",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo) ?? throw new FileNotFoundException("Failed to start process", serverExePath);

            using (cancellationToken.Register(() => process.Kill()))
            {
                var tasks = new[]
                {
                    process.StandardOutput.ReadToEndAsync(),
                    process.StandardError.ReadToEndAsync()
                };
                
                if (await WaitExitAsync(process, cancellationToken).WaitAsync(cancellationToken))
                {
                    await Task.WhenAll(tasks);
                }
                else
                {
                    process.Kill();
                    return null;
                }

                var stdout = tasks[0].Result;
                var stderr = tasks[1].Result;
                var output = stdout + stderr;

                if (string.IsNullOrWhiteSpace(output))
                    return null;

                return ParseHelpText(output);
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    static async Task<bool> WaitExitAsync(Process process, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) => tcs.TrySetResult(true);
        
        if (process.HasExited) return true;

        using var reg = ct.Register(() => tcs.TrySetCanceled());
        return await tcs.Task.WaitAsync(ct);
    }

    LlamaHelpInfo ParseHelpText(string output)
    {
        var supportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Strategy 1: Extract all --flag and -f patterns from lines
        foreach (Match match in _argPatterns[0].Matches(output))
        {
            string flag = match.Groups[1].Value.Trim();
            if (flag.Length >= 2 && !char.IsDigit(flag[1]))
            {
                supportedFlags.Add(flag);
            }
        }

        // Strategy 2: Extract comma-separated aliases like "-m MODEL, --model MODEL"
        foreach (Match match in _argPatterns[1].Matches(output))
        {
            string shortForm = "-" + match.Groups[1].Value.Split(' ')[0].Trim();
            string longForm = match.Groups[2].Value.Split(' ')[0].Trim();
            supportedFlags.Add(shortForm);
            supportedFlags.Add(longForm);
        }

        // Strategy 3: Line-by-line parsing for edge cases
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = line.TrimStart();
            
            // Lines starting with -- or - followed by word chars are argument definitions
            if (trimmed.StartsWith("--") || (trimmed.StartsWith("-") && trimmed.Length > 1 && char.IsLetter(trimmed[1])))
            {
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    supportedFlags.Add(parts[0]);
                }

                // Also check for comma-separated aliases on the same line
                var commaParts = trimmed.Split(',');
                foreach (var cp in commaParts)
                {
                    var cpTrimmed = cp.TrimStart();
                    if ((cpTrimmed.StartsWith("--") || (cpTrimmed.StartsWith("-") && cpTrimmed.Length > 1)) 
                        && char.IsLetter(cpTrimmed[1]))
                    {
                        var subParts = cpTrimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (subParts.Length > 0)
                            supportedFlags.Add(subParts[0]);
                    }
                }
            }
        }

        // Add known aliases for detected flags
        foreach (var flag in supportedFlags.ToList())
        {
            string normalized = flag.TrimStart('-');
            if (_knownAliases.TryGetValue(normalized, out var canonical))
            {
                supportedFlags.Add(canonical);
            }
        }

        var flags = ParseStructuredFlags(output);

        return new LlamaHelpInfo
        {
            SupportedFlags = supportedFlags,
            RawOutput = output,
            Flags = flags,
        };
    }

    /// <summary>
    /// Parses help text into structured CliFlagInfo objects with descriptions and default values.
    /// Handles formats like:
    ///   -m MODEL, --model MODEL              Load model
    ///   --temp FLOAT                         Temperature (default: 0.8)
    ///   --flash-attn                        Enable Flash Attention
    /// </summary>
    static Dictionary<string, CliFlagInfo> ParseStructuredFlags(string output)
    {
        var flags = new Dictionary<string, CliFlagInfo>(StringComparer.OrdinalIgnoreCase);

        // Regex to extract default value from description: "(default: 0.8)" or "(Default: 40)"
        var defaultRegex = new Regex(@"\(default:\s*([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = line.TrimStart();

            // Skip lines that don't start with a flag
            if (!trimmed.StartsWith("--") && !(trimmed.StartsWith("-") && trimmed.Length > 1 && char.IsLetter(trimmed[1])))
                continue;

            // Detect if this is an alias pair line: "-m MODEL, --model MODEL  description"
            var commaIndex = trimmed.IndexOf(',');
            string primaryFlag;
            string? shortFlag;
            string remainder;

            if (commaIndex > 0)
            {
                // Alias pair: extract both flags and the rest of the line
                var firstPart = trimmed.Substring(0, commaIndex).Trim();
                remainder = trimmed.Substring(commaIndex + 1).Trim();

                var firstTokens = firstPart.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                shortFlag = firstTokens.Length > 0 ? firstTokens[0] : null;

                var secondTokens = remainder.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                primaryFlag = secondTokens.Length > 0 ? secondTokens[0] : trimmed;

                // Description starts after the second flag and its optional type hint
                var descStart = remainder.IndexOf(primaryFlag);
                if (descStart >= 0)
                {
                    var afterFlag = remainder.Substring(descStart + primaryFlag.Length).Trim();
                    // Skip type hint like "MODEL" or "FLOAT"
                    if (afterFlag.Length > 0 && char.IsUpper(afterFlag[0]) && !afterFlag.StartsWith("-"))
                    {
                        var spaceIdx = afterFlag.IndexOf(' ');
                        if (spaceIdx > 0)
                            afterFlag = afterFlag.Substring(spaceIdx).Trim();
                    }
                    remainder = afterFlag;
                }
            }
            else
            {
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                primaryFlag = parts[0];
                shortFlag = null;

                // Description starts after the flag and optional type hint
                if (parts.Length > 1)
                {
                    remainder = string.Join(" ", parts.Skip(1));
                    // Skip type hint (uppercase word at start, not a dash)
                    if (char.IsUpper(remainder[0]) && !remainder.StartsWith("-"))
                    {
                        var spaceIdx = remainder.IndexOf(' ');
                        if (spaceIdx > 0)
                            remainder = remainder.Substring(spaceIdx).Trim();
                        else
                            remainder = string.Empty;
                    }
                }
                else
                {
                    remainder = string.Empty;
                }
            }

            // Extract default value from description
            var description = remainder;
            var defaultMatch = defaultRegex.Match(description);
            string? defaultValue = null;

            if (defaultMatch.Success)
            {
                defaultValue = defaultMatch.Groups[1].Value.Trim();
                description = defaultRegex.Replace(description, "").Trim();
            }

            // Clean up description: remove trailing whitespace and common artifacts
            description = description.Trim().TrimEnd('-').Trim();

            // Determine if flag takes a value (has type hint or default)
            bool takesValue = defaultValue != null ||
                              trimmed.Contains(" MODEL") || trimmed.Contains(" INT") ||
                              trimmed.Contains(" FLOAT") || trimmed.Contains(" STR") ||
                              trimmed.Contains(" STRING") || trimmed.Contains(" BOOL");

            var flagInfo = new CliFlagInfo
            {
                Name = primaryFlag,
                ShortName = shortFlag,
                Description = description,
                TakesValue = takesValue,
                DefaultValue = defaultValue ?? string.Empty,
            };

            flags[primaryFlag] = flagInfo;

            // Also register short name if present
            if (shortFlag != null && !flags.ContainsKey(shortFlag))
            {
                flags[shortFlag] = new CliFlagInfo
                {
                    Name = shortFlag,
                    Description = description,
                    TakesValue = takesValue,
                    DefaultValue = defaultValue ?? string.Empty,
                };
            }
        }

        return flags;
    }

    public bool IsFlagSupported(LlamaHelpInfo? helpInfo, string flag)
    {
        if (helpInfo == null || string.IsNullOrWhiteSpace(flag)) return true; // Unknown = assume supported
        return helpInfo.IsSupported(flag);
    }
}
