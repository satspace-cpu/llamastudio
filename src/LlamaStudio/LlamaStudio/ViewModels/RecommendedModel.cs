namespace LlamaStudio.ViewModels;

public sealed class RecommendedModel
{
	public string Repo { get; }

	public string Name { get; }

	public string Quantization { get; }

	public string Size { get; }

	public string Params { get; }

	public string Description { get; }

	public RecommendedModel(string repo, string name, string quantization, string size, string description)
	{
		Repo = repo;
		Name = name;
		Quantization = quantization;
		Size = size;
		Params = description;
		Description = name + " | " + quantization;
	}
}
