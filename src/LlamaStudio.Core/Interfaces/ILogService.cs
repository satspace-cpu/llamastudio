namespace LlamaStudio.Core.Interfaces;

public interface ILogService
{
    /// <summary>Raw server output lines (stdout/stderr)</summary>
    event EventHandler<string>? ServerOutputReceived;

    /// <summary>Application log entries</summary>
    IObservable<Models.LogEntry> LogStream { get; }

    void Debug(string message, string source = "App");
    void Information(string message, string source = "App");
    void Warning(string message, string source = "App");
    void Error(string message, string source = "App");
    void Error(Exception exception, string message, string source = "App");

    /// <summary>Emit a raw line from server stdout/stderr to the console</summary>
    void ServerOutput(string line);

    Task ExportLogsAsync(string path);
    Task ClearLogsAsync();
    List<Models.LogEntry> GetRecentLogs(int count = 100);

    /// <summary>Get all server console lines as a plain text block</summary>
    string GetConsoleText();

    /// <summary>Clear server console lines</summary>
    void ClearConsole();
}
