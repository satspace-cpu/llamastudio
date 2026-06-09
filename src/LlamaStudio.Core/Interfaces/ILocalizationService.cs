namespace LlamaStudio.Core.Interfaces;

using System;

public interface ILocalizationService
{
    string Language { get; set; }
    string T(string key);
    void ChangeLanguage(string language);
    event EventHandler<string> OnLanguageChanged;
}
