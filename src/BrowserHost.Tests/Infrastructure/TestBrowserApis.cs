using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Tests.Infrastructure;

public record BrowserApiInvocation(string Method, string? Arguments);

public interface ITestBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; }
}

public static class TestBrowserApiExtensions
{
    extension(ITestBrowserApi api)
    {
        public void ClearInvocations() =>
            api.Invocations.Clear();

        public bool WasCalledWith(string method, string? arguments = null) =>
            api.Invocations.Exists(i => i == new BrowserApiInvocation(method, arguments));
    }
}

public class TestTabPaletteBrowserApi() : TabPaletteBrowserApi(null!), ITestBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class TestTabCustomizationBrowserApi() : TabCustomizationBrowserApi(null!), ITestBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}