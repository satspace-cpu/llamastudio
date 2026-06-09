using System.Text.RegularExpressions;

namespace LlamaStudio.Core.Models;

/// <summary>Represents a part of a message content (text or code block)</summary>
public class ContentPart
{
    public enum PartType
    {
        Text,
        Code
    }

    public PartType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

/// <summary>Parses markdown content into parts (text and code blocks)</summary>
public static class ContentParser
{
    private static readonly Regex CodeBlockRegex = new(@"```(\w*)\n([\s\S]*?)```", RegexOptions.Compiled);

    public static List<ContentPart> Parse(string markdown)
    {
        var parts = new List<ContentPart>();
        if (string.IsNullOrEmpty(markdown))
            return parts;

        int lastIndex = 0;
        foreach (Match match in CodeBlockRegex.Matches(markdown))
        {
            // Add text before code block
            if (match.Index > lastIndex)
            {
                var text = markdown.Substring(lastIndex, match.Index - lastIndex).Trim();
                if (!string.IsNullOrEmpty(text))
                    parts.Add(new ContentPart { Type = ContentPart.PartType.Text, Content = text });
            }

            // Add code block
            parts.Add(new ContentPart
            {
                Type = ContentPart.PartType.Code,
                Content = match.Groups[2].Value.Trim(),
                Language = match.Groups[1].Value.ToLower()
            });

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < markdown.Length)
        {
            var text = markdown.Substring(lastIndex).Trim();
            if (!string.IsNullOrEmpty(text))
                parts.Add(new ContentPart { Type = ContentPart.PartType.Text, Content = text });
        }

        // If no code blocks found, return single text part
        if (parts.Count == 0)
            parts.Add(new ContentPart { Type = ContentPart.PartType.Text, Content = markdown });

        return parts;
    }
}
