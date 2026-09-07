using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface ILogService
{
	IObservable<LogEntry> LogStream { get; }

	event EventHandler<string>? ServerOutputReceived;

	void Debug(string message, string source = "App");

	void Information(string message, string source = "App");

	void Warning(string message, string source = "App");

	void Error(string message, string source = "App");

	void Error(Exception exception, string message, string source = "App");

	void ServerOutput(string line);

	Task ExportLogsAsync(string path);

	Task ClearLogsAsync();

	List<LogEntry> GetRecentLogs(int count = 100);

	string GetConsoleText();

	void ClearConsole();
}
