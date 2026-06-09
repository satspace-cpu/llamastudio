namespace LlamaStudio.Core.Interfaces;

public interface IDialogService
{
    Task<string?> SelectFolderAsync(string title, string? initialPath = null);
    Task<string?> SelectFileAsync(string title, string? initialPath = null, string? filter = null);
    Task<string?> SaveFileAsync(string title, string? initialPath = null, string? filter = null);
    Task ShowMessageAsync(string message, string? title = null);
    Task<bool> ShowConfirmationAsync(string message, string? title = null);
    Task ShowErrorAsync(string message, string? title = null);
    Task ShowSuccessAsync(string message, string? title = null);
    Task ShowInfoAsync(string message, string? title = null, bool isLongText = false);
    Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
    Task<string?> ShowProfileSelectAsync(string title, string message, IEnumerable<string> profiles);
    IDisposable? ShowLoading(string message, string? title = null);
}
