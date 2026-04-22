using System;
using System.IO;

namespace ClipyWin.Utilities;

public static class Log
{
    private static readonly object _lock = new();
    public static string LogFile => Path.Combine(AppPaths.AppDataFolder, "clipy.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", ex == null ? message : $"{message}\n{ex}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
