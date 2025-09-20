using BrowserHost.Utilities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserHost.Logging;

public sealed class LoggingService : IDisposable
{
    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
    public static LoggingService Instance => _instance.Value;

    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly Timer _flushTimer;
    private readonly string _logsFolder;
    private bool _disposed;

    private LoggingService()
    {
        _logsFolder = Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "logs");
        Directory.CreateDirectory(_logsFolder);
        
        // Flush every 5 seconds
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void Log(LogType type, string message)
    {
        if (_disposed) return;
        
        var entry = new LogEntry(DateTime.Now, type, message);
        _logQueue.Enqueue(entry);
    }

    private void FlushLogs(object? state)
    {
        if (_disposed) return;

        try
        {
            var today = DateTime.Today;
            var logFile = Path.Combine(_logsFolder, $"{today:yyyy-MM-dd}.log");
            
            var entriesToWrite = new StringBuilder();
            while (_logQueue.TryDequeue(out var entry))
            {
                var formattedEntry = FormatLogEntry(entry);
                entriesToWrite.AppendLine(formattedEntry);
            }

            if (entriesToWrite.Length > 0)
            {
                File.AppendAllText(logFile, entriesToWrite.ToString());
            }
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            // Don't let logging errors crash the application
            Debug.WriteLine($"Failed to flush logs: {ex.Message}");
        }
    }

    private static string FormatLogEntry(LogEntry entry)
    {
        var typeString = entry.Type switch
        {
            LogType.Performance => "Performance",
            LogType.Crashes => "Crashes", 
            LogType.ConsoleErrors => "ConsoleErrors",
            _ => entry.Type.ToString()
        };
        
        return $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}: [{typeString}] {entry.Message}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _flushTimer?.Dispose();
        
        // Flush any remaining logs
        FlushLogs(null);
    }
}