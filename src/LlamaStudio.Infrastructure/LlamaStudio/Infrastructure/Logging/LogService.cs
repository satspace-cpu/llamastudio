using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Logging;

public class LogService : ILogService, IDisposable
{
	private readonly Subject<LogEntry> _subject = new Subject<LogEntry>();

	private readonly List<LogEntry> _buffer = new List<LogEntry>();

	private readonly object _lock = new object();

	private const int MaxConsoleLines = 3000;

	private readonly Queue<string> _consoleLines = new Queue<string>();

	private readonly object _consoleLock = new object();

	private static readonly object s_fileLock = new object();

	public IObservable<LogEntry> LogStream => _subject.AsObservable();

	public event EventHandler<string>? ServerOutputReceived;

	private static void WriteToFile(string path, string line)
	{
	}

	public void Debug(string message, string source = "App")
	{
		Emit(LogLevel.Debug, message, source);
	}

	public void Information(string message, string source = "App")
	{
		Emit(LogLevel.Information, message, source);
	}

	public void Warning(string message, string source = "App")
	{
		Emit(LogLevel.Warning, message, source);
	}

	public void Error(string message, string source = "App")
	{
		Emit(LogLevel.Error, message, source);
	}

	public void Error(Exception exception, string message, string source = "App")
	{
		Emit(LogLevel.Error, message + ": " + exception.Message, source);
	}

	private void Emit(LogLevel level, string message, string source)
	{
		LogEntry logEntry = new LogEntry
		{
			Timestamp = DateTime.Now,
			Level = level,
			Message = message,
			Source = source
		};
		lock (_lock)
		{
			_buffer.Add(logEntry);
			if (_buffer.Count > 500)
			{
				_buffer.RemoveAt(0);
			}
		}
		_subject.OnNext(logEntry);
	}

	public void ServerOutput(string line)
	{
		if (string.IsNullOrWhiteSpace(line) || IsServerDebugLine(line))
		{
			return;
		}
		lock (_consoleLock)
		{
			_consoleLines.Enqueue(line);
			while (_consoleLines.Count > 3000)
			{
				_consoleLines.Dequeue();
			}
		}
		this.ServerOutputReceived?.Invoke(this, line);
	}

	private static bool IsServerDebugLine(string line)
	{
		if (line.Length < 6)
		{
			return false;
		}
		if (line[0] < '0' || line[0] > '9')
		{
			return false;
		}
		int num = -1;
		for (int i = 1; i < Math.Min(line.Length, 20); i++)
		{
			char c = line[i];
			switch (c)
			{
			case ' ':
				break;
			default:
				if (c == '.')
				{
					continue;
				}
				return false;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				continue;
			}
			num = i;
			break;
		}
		if (num < 2)
		{
			return false;
		}
		if (num + 1 >= line.Length)
		{
			return false;
		}
		return line[num + 1] == 'D';
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
	}

	public List<LogEntry> GetRecentLogs(int count = 100)
	{
		lock (_lock)
		{
			return _buffer.TakeLast(count).ToList();
		}
	}

	public async Task ExportLogsAsync(string path)
	{
		List<string> contents = new List<string>();
		lock (_consoleLock)
		{
			contents = new List<string>(_consoleLines);
		}
		await File.WriteAllLinesAsync(path, contents);
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
