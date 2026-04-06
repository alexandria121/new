using System.IO;
using System.Text;

namespace MagicalDeckbuilder.Logging;

    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
/// Singleton error logging service
/// </summary>
public sealed class ErrorLogger
{
    private static readonly Lazy<ErrorLogger> _instance = new(() => new ErrorLogger());
    public static ErrorLogger Instance => _instance.Value;

    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly StringBuilder _sessionLog = new();

    private ErrorLogger()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appDataPath, "MagicalDeckbuilder");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "error.log");
    }

    /// <summary>
    /// Get the path to the log file
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Get all log entries from the current session
    /// </summary>
    public string SessionLog => _sessionLog.ToString();

    /// <summary>
    /// Log a message
    /// </summary>
    public void Log(LogLevel level, string source, string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var entry = new StringBuilder();
        entry.AppendLine($"[{timestamp}] [{level}] [{source}] {message}");

        if (exception != null)
        {
            entry.AppendLine($"Exception: {exception.GetType().Name}");
            entry.AppendLine($"Message: {exception.Message}");
            entry.AppendLine($"Stack Trace:");
            entry.AppendLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                entry.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
                entry.AppendLine($"Inner Message: {exception.InnerException.Message}");
                entry.AppendLine($"Inner Stack Trace:");
                entry.AppendLine(exception.InnerException.StackTrace);
            }
        }

        entry.AppendLine("---");

        WriteEntry(entry.ToString());
    }

    /// <summary>
    /// Log a message with system context (captures environment info)
    /// </summary>
    public void LogWithContext(LogLevel level, string source, string message, Exception? exception = null, string? additionalContext = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var entry = new StringBuilder();
        entry.AppendLine($"[{timestamp}] [{level}] [{source}] {message}");
        entry.AppendLine("=== SYSTEM CONTEXT ===");
        entry.AppendLine($"OS: {Environment.OSVersion}");
        entry.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        entry.AppendLine($".NET Version: {Environment.Version}");
        entry.AppendLine($"Machine: {Environment.MachineName}");
        entry.AppendLine($"Processors: {Environment.ProcessorCount}");
        entry.AppendLine($"Uptime: {Environment.TickCount64}ms");

        if (additionalContext != null)
        {
            entry.AppendLine("=== ADDITIONAL CONTEXT ===");
            entry.AppendLine(additionalContext);
        }

        if (exception != null)
        {
            entry.AppendLine("=== EXCEPTION DETAILS ===");
            entry.AppendLine($"Exception: {exception.GetType().Name}");
            entry.AppendLine($"Message: {exception.Message}");
            entry.AppendLine($"Stack Trace:");
            entry.AppendLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                entry.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
                entry.AppendLine($"Inner Message: {exception.InnerException.Message}");
                entry.AppendLine($"Inner Stack Trace:");
                entry.AppendLine(exception.InnerException.StackTrace);
            }
        }

        entry.AppendLine("---");

        WriteEntry(entry.ToString());
    }

    private void WriteEntry(string entryString)
    {
        lock (_lock)
        {
            _sessionLog.Append(entryString);
            try
            {
                File.AppendAllText(_logFilePath, entryString);
            }
            catch
            {
                // Silently fail if we can't write to log
            }
        }
    }

    /// <summary>
    /// Log debug message
    /// </summary>
    public void Debug(string source, string message)
        => Log(LogLevel.Debug, source, message);

    /// <summary>
    /// Log info message
    /// </summary>
    public void Info(string source, string message)
        => Log(LogLevel.Info, source, message);

    /// <summary>
    /// Log warning message
    /// </summary>
    public void Warning(string source, string message)
        => Log(LogLevel.Warning, source, message);

    /// <summary>
    /// Log error message
    /// </summary>
    public void Error(string source, string message, Exception? exception = null)
        => Log(LogLevel.Error, source, message, exception);

    /// <summary>
    /// Log fatal error
    /// </summary>
    public void Fatal(string source, string message, Exception? exception = null)
        => Log(LogLevel.Fatal, source, message, exception);

    /// <summary>
    /// Log a message with operation context
    /// </summary>
    public void LogOperation(string operation, LogLevel level, string source, string message, Exception? exception = null)
    {
        var contextMessage = $"[Operation: {operation}] {message}";
        Log(level, source, contextMessage, exception);
    }

    /// <summary>
    /// Clear the log file
    /// </summary>
    public void ClearLog()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                    File.Delete(_logFilePath);
            }
            catch
            {
                // Silently fail
            }
        }
    }

    /// <summary>
    /// Read the current log file contents
    /// </summary>
    public string ReadLogFile()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                    return File.ReadAllText(_logFilePath);
            }
            catch
            {
                // Silently fail
            }
            return string.Empty;
        }
    }
}
