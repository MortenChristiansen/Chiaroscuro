using BrowserHost.Utilities;
using System.Reflection;
using Xunit.v3;

namespace BrowserHost.Tests.Infrastructure;

/// <summary>
/// Provides an xUnit test attribute that creates and disposes a dedicated PubSubContext for each test method, ensuring
/// test isolation for Pub/Sub operations. This atribute is automatically applied to all tests in the assembly.
/// </summary>
public sealed class PerTestPubSubContextAttribute : BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<IDisposable?> _scope = new();

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var context = new PubSubContext
        {
            DispatchStrategy = new DirectPubSubDispatchStrategy(),
        };

        _scope.Value = PubSub.PushContext(context);
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        _scope.Value?.Dispose();
        _scope.Value = null;
    }
}
