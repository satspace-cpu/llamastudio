using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Logging;

namespace LlamaStudio;

internal class Program
{
	private static readonly string _mutexName = "LlamaStudio_SingleInstance_Mutex";

	private static void Log(string message)
	{
		try
		{
			System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} {message}{Environment.NewLine}");
		}
		catch
		{
		}
	}

	[STAThread]
	public static void Main(string[] args)
	{
		if (OperatingSystem.IsLinux() && IsAnotherLlamaStudioProcessRunning())
		{
			Log("Another LlamaStudio process is already running, exiting.");
			return;
		}

		bool createdNew;
		using (new Mutex(initiallyOwned: true, _mutexName, out createdNew))
		{
			if (!createdNew)
			{
				Log("Another instance is already running, exiting.");
				return;
			}
			AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
			{
				Exception ex2 = (e.ExceptionObject as Exception) ?? new Exception(e.ToString());
				Log("[FATAL UNHANDLED] " + ex2.GetType().Name + ": " + ex2.Message);
				Log(ex2.StackTrace ?? "no stack trace");
				if (ex2.InnerException != null)
				{
					Log("[INNER] " + ex2.InnerException.GetType().Name + ": " + ex2.InnerException.Message);
				}
			};
			try
			{
				Log("Building Avalonia app...");
				AppBuilder builder = BuildAvaloniaApp();
				Log("Starting...");
				builder.StartWithClassicDesktopLifetime(args);
				Log("Returned normally");
			}
			catch (Exception ex)
			{
				Log("[FATAL] " + ex.GetType().Name + ": " + ex.Message);
				Log(ex.StackTrace ?? "no stack trace");
			}
		}
	}

	private static bool IsAnotherLlamaStudioProcessRunning()
	{
		int currentProcessId = Environment.ProcessId;
		try
		{
			return Process.GetProcesses()
				.Where(process => process.Id != currentProcessId)
				.Any(process =>
				{
					try
					{
						return process.ProcessName.Equals("LlamaStudio", StringComparison.OrdinalIgnoreCase);
					}
					catch
					{
						return false;
					}
				});
		}
		catch (Exception ex)
		{
			Log("Unable to inspect running processes: " + ex.Message);
			return false;
		}
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont()
			.LogToTrace(LogEventLevel.Warning);
	}
}
