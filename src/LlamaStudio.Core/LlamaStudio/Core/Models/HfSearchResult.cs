using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class HfSearchResult
{
	public string Id { get; set; } = string.Empty;

	public string Author { get; set; } = string.Empty;

	public string ModelName { get; set; } = string.Empty;

	public int Likes { get; set; }

	public int Downloads { get; set; }

	public string PipelineTag { get; set; } = string.Empty;

	public string LastModified { get; set; } = string.Empty;

	public List<string> Tags { get; set; } = new List<string>();

	public string Url => "https://huggingface.co/" + Id;
}
