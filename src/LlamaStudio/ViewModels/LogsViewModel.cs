using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using LlamaStudio.Core.Interfaces;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;


namespace LlamaStudio.ViewModels;

public partial class LogsViewModel : ObservableObject, IDisposable
{
    readonly ILogService _logService;
    readonly ILocalizationService _loc;
    readonly ConcurrentQueue<string> _pendingLogs = new();
    bool _flushScheduled;
    readonly object _lock = new();

    const int MaxLines = 2000;
    string _lastLine = string.Empty;
    int _duplicateCount = 0;

    [ObservableProperty] ObservableCollection<string> _logLines = new();
    [ObservableProperty] bool _autoScroll = true;
    [ObservableProperty] bool _isListening = false;

    public string Title => _loc.T("logs.title");
    public string ClearBtn => _loc.T("logs.clear_btn");
    public string CopyBtn => _loc.T("logs.copy_btn");
    public string SaveBtn => _loc.T("logs.save_btn");
    public string AutoScrollLabel => _loc.T("logs.autoscroll_label");
    public string ListeningLabel => _loc.T("logs.listening_label");

    public LogsViewModel(ILogService logService, ILocalizationService loc)
    {
        _logService = logService;
        _loc = loc;

        _loc.OnLanguageChanged += (_, _) =>
        {
            foreach (var prop in new[]
            {
                nameof(Title), nameof(ClearBtn), nameof(CopyBtn),
                nameof(SaveBtn), nameof(AutoScrollLabel), nameof(ListeningLabel)
            })
                OnPropertyChanged(prop);
        };

        var initial = _logService.GetConsoleText();
        if (!string.IsNullOrEmpty(initial))
        {
            foreach (var line in initial.Split('\n'))
                LogLines.Add(line);
        }

        _logService.ServerOutputReceived += OnServerOutputReceived;
        IsListening = true;
    }

    void OnServerOutputReceived(object? sender, string line)
    {
        _pendingLogs.Enqueue(line);

        lock (_lock)
        {
            if (_flushScheduled) return;
            _flushScheduled = true;
        }

        Dispatcher.UIThread.Post(FlushLogs, DispatcherPriority.Background);
    }

    void FlushLogs()
    {
        var toFlush = new List<string>();
        while (_pendingLogs.TryDequeue(out var line))
            toFlush.Add(line);

        lock (_lock)
        {
            _flushScheduled = false;
        }

        if (toFlush.Count == 0) return;

        foreach (var line in toFlush)
        {
            if (line == _lastLine)
            {
                _duplicateCount++;
            }
            else
            {
                if (_duplicateCount > 0)
                {
                    LogLines.Add($"  ({_duplicateCount} identical lines above)");
                    _duplicateCount = 0;
                }
                LogLines.Add(line);
                _lastLine = line;
            }
        }

        // Flush remaining duplicates
        if (_duplicateCount > 0)
        {
            LogLines.Add($"  ({_duplicateCount} identical lines above)");
            _duplicateCount = 0;
        }

        while (LogLines.Count > MaxLines)
            LogLines.RemoveAt(0);
    }

    public string GetFullText()
    {
        return string.Join("\n", LogLines);
    }

    [RelayCommand]
    void ClearLog()
    {
        LogLines.Clear();
        _lastLine = string.Empty;
        _duplicateCount = 0;
        _logService.ClearConsole();
    }

    [RelayCommand]
    async Task SaveLog()
    {
        var text = GetFullText();
        if (string.IsNullOrEmpty(text)) return;
        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            $"llama_console_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        await File.WriteAllTextAsync(path, text);
    }

    public void Dispose()
    {
        _logService.ServerOutputReceived -= OnServerOutputReceived;
        IsListening = false;
    }
}
