using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.ViewModels;

public class LogsViewModel : ObservableObject, IDisposable
{
	private readonly ILogService _logService;

	private readonly ILocalizationService _loc;

	private readonly ConcurrentQueue<string> _pendingLogs = new ConcurrentQueue<string>();

	private bool _flushScheduled;

	private readonly object _lock = new object();

	private const int MaxLines = 2000;

	private string _lastLine = string.Empty;

	private int _duplicateCount;

	[ObservableProperty]
	private ObservableCollection<string> _logLines = new ObservableCollection<string>();

	[ObservableProperty]
	private bool _autoScroll = true;

	[ObservableProperty]
	private bool _isListening;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? clearLogCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveLogCommand;

	public string Title => _loc.T("logs.title");

	public string ClearBtn => _loc.T("logs.clear_btn");

	public string CopyBtn => _loc.T("logs.copy_btn");

	public string SaveBtn => _loc.T("logs.save_btn");

	public string AutoScrollLabel => _loc.T("logs.autoscroll_label");

	public string ListeningLabel => _loc.T("logs.listening_label");

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<string> LogLines
	{
		get
		{
			return _logLines;
		}
		[MemberNotNull("_logLines")]
		set
		{
			if (!EqualityComparer<ObservableCollection<string>>.Default.Equals(_logLines, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LogLines);
				_logLines = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LogLines);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoScroll
	{
		get
		{
			return _autoScroll;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoScroll, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AutoScroll);
				_autoScroll = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AutoScroll);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsListening
	{
		get
		{
			return _isListening;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isListening, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsListening);
				_isListening = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsListening);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearLogCommand => clearLogCommand ?? (clearLogCommand = new RelayCommand(ClearLog));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveLogCommand => saveLogCommand ?? (saveLogCommand = new AsyncRelayCommand(SaveLog));

	public LogsViewModel(ILogService logService, ILocalizationService loc)
	{
		_logService = logService;
		_loc = loc;
		_loc.OnLanguageChanged += delegate
		{
			string[] array2 = new string[6] { "Title", "ClearBtn", "CopyBtn", "SaveBtn", "AutoScrollLabel", "ListeningLabel" };
			foreach (string propertyName in array2)
			{
				OnPropertyChanged(propertyName);
			}
		};
		string consoleText = _logService.GetConsoleText();
		if (!string.IsNullOrEmpty(consoleText))
		{
			string[] array = consoleText.Split('\n');
			foreach (string item in array)
			{
				LogLines.Add(item);
			}
		}
		_logService.ServerOutputReceived += OnServerOutputReceived;
		IsListening = true;
	}

	private void OnServerOutputReceived(object? sender, string line)
	{
		_pendingLogs.Enqueue(line);
		lock (_lock)
		{
			if (_flushScheduled)
			{
				return;
			}
			_flushScheduled = true;
		}
		Dispatcher.UIThread.Post(FlushLogs, DispatcherPriority.Background);
	}

	private void FlushLogs()
	{
		List<string> list = new List<string>();
		string result;
		while (_pendingLogs.TryDequeue(out result))
		{
			list.Add(result);
		}
		lock (_lock)
		{
			_flushScheduled = false;
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (string item in list)
		{
			if (item == _lastLine)
			{
				_duplicateCount++;
				continue;
			}
			if (_duplicateCount > 0)
			{
				LogLines.Add($"  ({_duplicateCount} identical lines above)");
				_duplicateCount = 0;
			}
			LogLines.Add(item);
			_lastLine = item;
		}
		if (_duplicateCount > 0)
		{
			LogLines.Add($"  ({_duplicateCount} identical lines above)");
			_duplicateCount = 0;
		}
		while (LogLines.Count > 2000)
		{
			LogLines.RemoveAt(0);
		}
	}

	public string GetFullText()
	{
		return string.Join("\n", LogLines);
	}

	[RelayCommand]
	private void ClearLog()
	{
		LogLines.Clear();
		_lastLine = string.Empty;
		_duplicateCount = 0;
		_logService.ClearConsole();
	}

	[RelayCommand]
	private async Task SaveLog()
	{
		string fullText = GetFullText();
		if (!string.IsNullOrEmpty(fullText))
		{
			await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"llama_console_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), fullText);
		}
	}

	public void Dispose()
	{
		_logService.ServerOutputReceived -= OnServerOutputReceived;
		IsListening = false;
	}
}
