using BrowserHost.Utilities;

namespace BrowserHost.Tests.Infrastructure;

/// <summary>
/// Provides a dispatch strategy for publishing messages directly to subscribers without intermediate processing or
/// queuing.
/// </summary>
internal class DirectPubSubDispatchStrategy : PubSub.IPubSubDispatchStrategy
{
    public void Invoke<T>(Action<T> action, T message)
    {
        action(message);
    }

    public async Task InvokeAsync<T>(Func<T, Task> action, T message)
    {
        await action(message);
    }
}