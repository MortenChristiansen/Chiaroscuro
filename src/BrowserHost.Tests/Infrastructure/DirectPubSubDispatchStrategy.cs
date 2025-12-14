using BrowserHost.Utilities;

namespace BrowserHost.Tests.Infrastructure;

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