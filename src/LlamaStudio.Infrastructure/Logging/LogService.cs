using System.Reactive.Linq;
using System.Reactive.Subjects;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Logging;

public class LogService : ILogService, IDisposable
{
    readonly Subject<LogEntry> _subject = new();
    readonly List<LogEntry> _buffer = new();
    readonly object _lock = new();

    // Server console output — plain text lines
    const int MaxConsoleLines = 3000;
    readonly Queue<string> _consoleLines = new();
    readonly object _consoleLock = new();

    // File logging for server output
    static readonly string s_logFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs", "server_output.log");
    static readonly string s_appLogFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
    static readonly string s_chatLogFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs", "chat.log");
    static readonly object s_fileLock = new();

    public IObservable<LogEntry> LogStream => _subject.AsObservable();
    public event EventHandler<string>? ServerOutputReceived;

    public LogService()
    {
        // Ensure logs directory exists
        var logDir = Path.GetDirectoryName(s_logFilePath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
    }

    static void WriteToFile(string path, string line)
    {
        lock (s_fileLock)
        {
            try
            {
                File.AppendAllText(path, line + "\n", System.Text.Encoding.UTF8);
            }
            catch { }
        }
    }

    // --- Application logging (in-memory only, no disk) ---

    public void Debug(string message, string source = "App") => Emit(LogLevel.Debug, message, source);
    public void Information(string message, string source = "App") => Emit(LogLevel.Information, message, source);
    public void Warning(string message, string source = "App") => Emit(LogLevel.Warning, message, source);
    public void Error(string message, string source = "App") => Emit(LogLevel.Error, message, source);
    public void Error(Exception exception, string message, string source = "App") => Emit(LogLevel.Error, $"{message}: {exception.Message}", source);

    void Emit(LogLevel level, string message, string source)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Source = source
        };

        lock (_lock)
        {
            _buffer.Add(entry);
            if (_buffer.Count > 500)
                _buffer.RemoveAt(0);
        }

        _subject.OnNext(entry);

        // Write to app log file
        var logLine = $"[{entry.Timestamp:HH:mm:ss.fff}] [{level,-12}] [{source,-15}] {message}";
        WriteToFile(s_appLogFilePath, logLine);

        // Write chat-related logs to separate file
        if (source == "Chat" || source == "ChatService")
            WriteToFile(s_chatLogFilePath, logLine);
    }

    // --- Server console output ---

    public void ServerOutput(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // Filter out llama-server internal debug spam (e.g. "0.xx D srv  stop: all tasks already finished")
        if (IsServerDebugLine(line))
            return;

        lock (_consoleLock)
        {
            _consoleLines.Enqueue(line);
            while (_consoleLines.Count > MaxConsoleLines)
                _consoleLines.Dequeue();
        }

        // Write to file
        lock (s_fileLock)
        {
            try
            {
                File.AppendAllText(s_logFilePath, line + "\n");
            }
            catch { }
        }

        ServerOutputReceived?.Invoke(this, line);
    }

    static bool IsServerDebugLine(string line)
    {
        // llama-server debug lines format: "0.09.492.063 D srv  message"
        // Only filter out 'D' (Debug) level lines — keep I, W, E visible
        if (line.Length < 6)
            return false;

        // Must start with a digit
        if (line[0] < '0' || line[0] > '9')
            return false;

        // Find the first space after the timestamp
        int spacePos = -1;
        for (int i = 1; i < Math.Min(line.Length, 20); i++)
        {
            char c = line[i];
            if (c == ' ')
            {
                spacePos = i;
                break;
            }
            // Timestamp consists of digits and dots
            if (!((c >= '0' && c <= '9') || c == '.'))
                return false;
        }

        if (spacePos < 2)
            return false;

        // After space, expect a single letter log level
        if (spacePos + 1 >= line.Length)
            return false;

        char level = line[spacePos + 1];
        // Only filter Debug lines
        return level == 'D';
    }

    public string GetConsoleText()
    {
        lock (_consoleLock)
        {
            return string.Join("\n", _consoleLines);
        }
    }

    public void ClearConsole()
    {
        lock (_consoleLock)
        {
            _consoleLines.Clear();
        }
        // Also clear file log
        lock (s_fileLock)
        {
            try
            {
                File.WriteAllText(s_logFilePath, string.Empty);
            }
            catch { }
        }
    }

    // --- Legacy methods ---

    public List<LogEntry> GetRecentLogs(int count = 100)
    {
        lock (_lock)
        {
            return _buffer.TakeLast(count).ToList();
        }
    }

    public async Task ExportLogsAsync(string path)
    {
        var lines = new List<string>();
        lock (_consoleLock)
        {
            lines = new List<string>(_consoleLines);
        }
        await File.WriteAllLinesAsync(path, lines);
    }

    public Task ClearLogsAsync()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
        lock (_consoleLock)
        {
            _consoleLines.Clear();
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}
