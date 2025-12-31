using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;

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

public class FakeTabsBrowserApi() : TabsBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeFindTextBrowserApi() : FindTextBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeDomainCustomizationBrowserApi() : DomainCustomizationBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeSettingsBrowserApi() : SettingsBrowserApi(() => null), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    // This is special because it forwards the arguments to the current tab browser CallClientApi method.
    public override void SettingsLoaded(SettingUiStateDto settings) =>
        Invocations.Add(new("settingsLoaded", settings.ToJsonObject()));
}