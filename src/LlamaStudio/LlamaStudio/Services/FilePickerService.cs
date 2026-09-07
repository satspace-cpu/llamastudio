using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Services;

public class FilePickerService : IFilePickerService
{
	private readonly Func<Window?> _getWindow;

	public FilePickerService(Func<Window?> getWindow)
	{
		_getWindow = getWindow;
	}

	public async Task<string?> SaveFileAsync(string suggestedName, string extension)
	{
		Window window = _getWindow();
		if (window == null)
		{
			return null;
		}
		return (await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Save chat session",
			DefaultExtension = extension,
			FileTypeChoices = new FilePickerFileType[3]
			{
				new FilePickerFileType("Markdown")
				{
					Patterns = new string[1] { "*" + extension }
				},
				new FilePickerFileType("Text")
				{
					Patterns = new string[1] { "*.txt" }
				},
				new FilePickerFileType("All Files")
				{
					Patterns = new string[1] { "*.*" }
				}
			}
		}))?.Path.LocalPath;
	}
}
