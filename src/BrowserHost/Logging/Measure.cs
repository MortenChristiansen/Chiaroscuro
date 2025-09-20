using System;
using System.Diagnostics;

namespace BrowserHost.Logging;

public static class Measure
{
    private static readonly DateTime _startupTime = DateTime.Now;

    public static IDisposable Operation(string operationName)
    {
        return new OperationMeasurement(operationName);
    }

    public static void Event(string eventName)
    {
        var elapsed = DateTime.Now - _startupTime;
        var message = $"{eventName} [at +{elapsed:mm\\:ss\\:fff}]";
        LoggingService.Instance.Log(LogType.Performance, message);
    }

    private sealed class OperationMeasurement : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationMeasurement(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed;
            var message = $"{_operationName} [duration {elapsed:mm\\:ss\\:fff}]";
            LoggingService.Instance.Log(LogType.Performance, message);
        }
    }
}