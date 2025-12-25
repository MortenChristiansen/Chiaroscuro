using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Tests.Fakes;

public record BrowserApiInvocation(string Method, string? Arguments);

public interface IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; }
}

public static class TestBrowserApiExtensions
{
    extension(IFakeBrowserApi api)
    {
        public void ClearInvocations() =>
            api.Invocations.Clear();

        public bool WasCalledWith(string method, string? arguments = null) =>
            api.Invocations.Exists(i => i == new BrowserApiInvocation(method, arguments));
    }
}

public class FakeTabPaletteBrowserApi() : TabPaletteBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeTabCustomizationBrowserApi() : TabCustomizationBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}