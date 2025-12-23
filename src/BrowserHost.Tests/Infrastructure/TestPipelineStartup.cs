using BrowserHost.Utilities;
using Xunit.Sdk;
using Xunit.v3;

namespace BrowserHost.Tests.Infrastructure;

/// <summary>
/// Registered as the test pipeline startup, configures PubSub to use direct dispatching.
/// </summary>
internal class TestPipelineStartup : ITestPipelineStartup
{
    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        PubSub.Instance.DispatchStrategy = new DirectPubSubDispatchStrategy();

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync()
    {
        return ValueTask.CompletedTask;
    }
}
