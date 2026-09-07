using System;
using LlamaStudio.Core.Enums;

namespace LlamaStudio.Core.Models;

public class LogEntry
{
	public DateTime Timestamp { get; set; }

	public LogLevel Level { get; set; }

	public string Source { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;

	public bool IsStderr { get; set; }

	public string? Exception { get; set; }
}
