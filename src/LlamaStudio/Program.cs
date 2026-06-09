using Avalonia;
using System;
using System.IO;
using System.Threading;

namespace LlamaStudio;

class Program
{
    static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
    static readonly string _mutexName = "LlamaStudio_SingleInstance_Mutex";

    static void Log(string message)
    {
        File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
    }

    [STAThread]
    public static void Main(string[] args)
    {
        bool createdNew;
        using (var mutex = new Mutex(true, _mutexName, out createdNew))
        {
            if (!createdNew)
            {
                Log("Another instance is already running, exiting.");
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception ?? new Exception(e.ToString());
                Log($"[FATAL UNHANDLED] {ex.GetType().Name}: {ex.Message}");
                Log(ex.StackTrace ?? "no stack trace");
                if (ex.InnerException != null)
                    Log($"[INNER] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            };
            try
            {
                Log("Building Avalonia app...");
                var app = BuildAvaloniaApp();
                Log("Starting...");
                app.StartWithClassicDesktopLifetime(args);
                Log("Returned normally");
            }
            catch (Exception ex)
            {
                Log($"[FATAL] {ex.GetType().Name}: {ex.Message}");
                Log(ex.StackTrace ?? "no stack trace");
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}