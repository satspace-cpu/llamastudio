namespace LlamaStudio.Core.Models;

public class ArgumentMapping
{
	public string Canonical { get; init; } = string.Empty;

	public ArgType ArgType { get; init; }

	public ArgumentMapping(string canonical, ArgType argType)
	{
		Canonical = canonical;
		ArgType = argType;
	}
}
