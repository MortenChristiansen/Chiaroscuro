using BrowserHost.Utilities;

namespace BrowserHost.Tests.Infrastructure;

/// <summary>
/// Provides a dispatch strategy for publishing messages directly to subscribers without intermediate processing or
/// queuing.
/// </summary>
internal class DirectPubSubDispatchStrategy : PubSub.IPubSubDispatchStrategy
{
    public Action? OnDispatched { get; set; }

    public void Invoke<T>(Action<T> action, T message)
    {
        OnDispatched?.Invoke();
        action(message);
    }

    public async Task InvokeAsync<T>(Func<T, Task> action, T message)
    {
        OnDispatched?.Invoke();
        await action(message);
    }
}