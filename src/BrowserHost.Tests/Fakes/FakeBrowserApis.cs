using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Terminal;

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

public class FakeCustomWindowChromeBrowserApi() : CustomWindowChromeBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeWorkspacesBrowserApi() : WorkspacesBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakePinnedTabsBrowserApi() : PinnedTabsBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeActionDialogBrowserApi() : ActionDialogBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeDownloadsBrowserApi() : DownloadsBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeTerminalBrowserApi() : TerminalBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}

public class FakeLocalWebAppBrowserApi() : LocalWebAppBrowserApi(null!), IFakeBrowserApi
{
    public List<BrowserApiInvocation> Invocations { get; } = [];

    public override void CallClientApi(string api, string? arguments = null) =>
        Invocations.Add(new(api, arguments));
}
