using BrowserHost.Utilities;
using Xunit.Sdk;
using Xunit.v3;

namespace BrowserHost.Tests.Infrastructure;

internal class TestStartup : ITestPipelineStartup
{
    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        PubSub.DispatchStrategy = new DirectPubSubDispatchStrategy();

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync()
    {
        return ValueTask.CompletedTask;
    }
}
