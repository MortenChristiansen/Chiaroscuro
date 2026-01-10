using System.Windows.Threading;

namespace BrowserHost.E2E.Infrastructure;

internal static class StaTestRunner
{
    private static readonly Lazy<StaDispatcherThread> _sta = new(() => new StaDispatcherThread());

    public static async Task RunAsync(Func<Task> test)
    {
        var maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _sta.Value.RunAsync(test);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                WriteRetryWarning(attempt, maxAttempts, ex);
            }
        }
    }

    private static void WriteRetryWarning(int attempt, int maxAttempts, Exception ex)
    {
        var message = $"E2E test attempt {attempt}/{maxAttempts} failed; retrying ({attempt + 1}/{maxAttempts}). {ex.GetType().Name}: {ex.Message}";

        if (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"::warning::{EscapeGitHubActionsCommandData(message)}");
            return;
        }

        Console.WriteLine($"WARNING: {message}");
    }

    private static string EscapeGitHubActionsCommandData(string value)
    {
        return value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
    }

    private sealed class StaDispatcherThread
    {
        private readonly Dispatcher _dispatcher;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public StaDispatcherThread()
        {
            var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);

            var thread = new Thread(() =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                ready.TrySetResult(dispatcher);
                Dispatcher.Run();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            _dispatcher = ready.Task.GetAwaiter().GetResult();
        }

        public async Task RunAsync(Func<Task> action)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await _dispatcher.InvokeAsync(action).Task.Unwrap().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
