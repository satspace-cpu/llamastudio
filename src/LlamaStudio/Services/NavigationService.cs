using LlamaStudio.Core.Interfaces;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Services;

public class NavigationService : INavigationService
{
    readonly Func<MainViewModel> _mainFactory;

    public NavigationService(Func<MainViewModel> mainFactory)
    {
        _mainFactory = mainFactory;
    }

    public void Navigate(string page)
    {
        _mainFactory().NavigateTo(page);
    }
}