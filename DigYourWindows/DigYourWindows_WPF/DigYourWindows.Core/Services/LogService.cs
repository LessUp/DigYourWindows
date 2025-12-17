using System.Diagnostics;

namespace DigYourWindows.Core.Services;

public interface ILogService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

public sealed class FileLogService : ILogService
{
    private readonly object _lock = new();
    private readonly string _logFilePath;

    public FileLogService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "DigYourWindows", "logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "digyourwindows.log");
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warn(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            var line = $"{timestamp} [{level}] {message}";

            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }

            Debug.WriteLine(line);
        }
        catch
        {
        }
    }
}
