using System.Windows.Threading;

namespace BrowserHost.E2E.Infrastructure;

internal static class StaTestRunner
{
    private static readonly Lazy<StaDispatcherThread> _sta = new(() => new StaDispatcherThread());

    public static Task RunAsync(Func<Task> action) => _sta.Value.RunAsync(action);

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
