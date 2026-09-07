using System;
using System.ComponentModel;
using LlamaStudio;
using LlamaStudio.Controls;
using LlamaStudio.Views;
using LlamaStudio.Views.Pages;

namespace CompiledAvaloniaXaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public class _0021XamlLoader
{
	public static object TryLoad(IServiceProvider P_0, string P_1)
	{
		if (string.Equals(P_1, "avares://LlamaStudio/App.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new App();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Controls/TpsGaugeControl.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new TpsGaugeControl();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/ArgPickerWindow.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ArgPickerWindow();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/MainWindow.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new MainWindow();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/ApiTestPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ApiTestPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/ChatPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ChatPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/DashboardPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new DashboardPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/LlamaReleasesPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new LlamaReleasesPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/LogsPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new LogsPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/ModelsPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ModelsPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/ProfilesPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ProfilesPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/ServerPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new ServerPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/SettingsPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new SettingsPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/Pages/SupportPage.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new SupportPage();
		}
		if (string.Equals(P_1, "avares://LlamaStudio/Views/TipWindow.axaml", StringComparison.OrdinalIgnoreCase))
		{
			return new TipWindow();
		}
		return null;
	}

	public static object TryLoad(string P_0)
	{
		return TryLoad(null, P_0);
	}
}
