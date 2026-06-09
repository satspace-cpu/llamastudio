using LlamaStudio.Core.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace LlamaStudio.Services;

public class FilePickerService : IFilePickerService
{
    readonly Func<Window?> _getWindow;

    public FilePickerService(Func<Window?> getWindow)
    {
        _getWindow = getWindow;
    }

    public async Task<string?> SaveFileAsync(string suggestedName, string extension)
    {
        var window = _getWindow();
        if (window == null)
            return null;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save chat session",
            DefaultExtension = extension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Markdown")
                {
                    Patterns = new[] { $"*{extension}" }
                },
                new FilePickerFileType("Text")
                {
                    Patterns = new[] { "*.txt" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file == null)
            return null;

        return file.Path.LocalPath;
    }
}
