using LlamaStudio.Core.Interfaces;

namespace LlamaStudio;

internal static class NativeLanguage
{
	public static bool IsEnglish
	{
		get
		{
			try
			{
				var localization = App.Services?.GetService(typeof(ILocalizationService)) as ILocalizationService;
				return localization == null || localization.Language == "en";
			}
			catch
			{
				return true;
			}
		}
	}

	public static string Text(string russian, string english) => IsEnglish ? english : russian;
}
