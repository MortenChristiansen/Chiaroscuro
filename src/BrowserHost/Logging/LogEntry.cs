using System;

namespace BrowserHost.Logging;

public record LogEntry(DateTime Timestamp, LogType Type, string Message);