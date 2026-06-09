namespace LlamaStudio.Core.Interfaces;

public interface IFilePickerService
{
    Task<string?> SaveFileAsync(string suggestedName, string extension);
}
