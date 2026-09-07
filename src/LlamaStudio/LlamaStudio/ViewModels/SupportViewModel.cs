using CommunityToolkit.Mvvm.ComponentModel;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.ViewModels;

public class SupportViewModel : ObservableObject
{
	private readonly ILocalizationService _loc;

	public string Title => _loc.T("support.title");

	public string TelegramTitle => _loc.T("support.telegram_title");

	public string TelegramDesc => _loc.T("support.telegram_desc");

	public string OpenChannelBtn => _loc.T("support.open_channel");

	public string SupportChatBtn => _loc.T("support.support_chat_btn");

	public string HelperBotBtn => _loc.T("support.helper_bot_btn");

	public string GithubTitle => _loc.T("support.github_title");

	public string GithubDesc => _loc.T("support.github_desc");

	public string OpenRepoBtn => _loc.T("support.open_repo");

	public string DownloadLatestBtn => _loc.T("support.download_latest");

	public string WhereToFind => _loc.T("support.where_to_find");

	public string NewsChannel => _loc.T("support.news_channel");

	public string QuestionsChat => _loc.T("support.questions_chat");

	public string QuickAnswersBot => _loc.T("support.quick_answers_bot");

	public string ReportBug => _loc.T("support.report_bug");

	public SupportViewModel(ILocalizationService loc)
	{
		_loc = loc;
		_loc.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] array = new string[15]
		{
			"Title", "TelegramTitle", "TelegramDesc", "OpenChannelBtn", "SupportChatBtn", "HelperBotBtn", "GithubTitle", "GithubDesc",
			"OpenRepoBtn", "DownloadLatestBtn", "WhereToFind", "NewsChannel", "QuestionsChat", "QuickAnswersBot", "ReportBug"
		};
		foreach (string propertyName in array)
		{
			OnPropertyChanged(propertyName);
		}
	}
}
