using System;

namespace LlamaStudio.Core.Interfaces;

public interface ILocalizationService
{
	string Language { get; set; }

	event EventHandler<string> OnLanguageChanged;

	string T(string key);

	void ChangeLanguage(string language);
}
