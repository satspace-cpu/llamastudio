using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LlamaStudio.Core.Models;

public static class ContentParser
{
	private static readonly Regex CodeBlockRegex = new Regex("```(\\w*)\\n([\\s\\S]*?)```", RegexOptions.Compiled);

	public static List<ContentPart> Parse(string markdown)
	{
		List<ContentPart> list = new List<ContentPart>();
		if (string.IsNullOrEmpty(markdown))
		{
			return list;
		}
		int num = 0;
		foreach (Match item in CodeBlockRegex.Matches(markdown))
		{
			if (item.Index > num)
			{
				string text = markdown.Substring(num, item.Index - num).Trim();
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(new ContentPart
					{
						Type = ContentPart.PartType.Text,
						Content = text
					});
				}
			}
			list.Add(new ContentPart
			{
				Type = ContentPart.PartType.Code,
				Content = item.Groups[2].Value.Trim(),
				Language = item.Groups[1].Value.ToLower()
			});
			num = item.Index + item.Length;
		}
		if (num < markdown.Length)
		{
			string trailingText = markdown.Substring(num).Trim();
			if (!string.IsNullOrEmpty(trailingText))
			{
				list.Add(new ContentPart
				{
					Type = ContentPart.PartType.Text,
					Content = trailingText
				});
			}
		}
		if (list.Count == 0)
		{
			list.Add(new ContentPart
			{
				Type = ContentPart.PartType.Text,
				Content = markdown
			});
		}
		return list;
	}
}
