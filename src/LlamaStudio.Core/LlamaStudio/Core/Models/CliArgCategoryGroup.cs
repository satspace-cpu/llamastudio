using System.Collections.Generic;

namespace LlamaStudio.Core.Models;

public class CliArgCategoryGroup
{
	public string CategoryName { get; set; } = "";

	public List<CliArgDefinition> Items { get; set; } = new List<CliArgDefinition>();
}
