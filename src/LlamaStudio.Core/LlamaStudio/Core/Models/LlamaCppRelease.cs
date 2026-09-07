using System;
using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class LlamaCppRelease
{
	public string TagName { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string HtmlUrl { get; set; } = string.Empty;

	public DateTime PublishedAt { get; set; }

	public bool IsPrerelease { get; set; }

	public string Body { get; set; } = string.Empty;

	public List<ReleaseAsset> Assets { get; set; } = new List<ReleaseAsset>();
}
