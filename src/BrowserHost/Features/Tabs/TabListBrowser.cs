using BrowserHost.CefInfrastructure;
using CefSharp;
using System.Text.Json;

namespace BrowserHost.Features.Tabs;

public class TabListBrowser : BaseBrowser<TabListBrowserApi>
{
    public override TabListBrowserApi Api { get; }

    public TabListBrowser()
        : base("/tabs")
    {
        Api = new TabListBrowserApi(this);
    }

    private static readonly JsonSerializerOptions _tabSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void AddTab(TabDto tab, bool activate = true)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var tabJson = JsonSerializer.Serialize(tab, _tabSerializationOptions);
            var script = $"window.angularApi.addTab({tabJson}, {(activate ? "true" : "false")})";
            this.ExecuteScriptAsync(script);
        });
    }

    public void UpdateTab(TabDto tab)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var tabJson = JsonSerializer.Serialize(tab, _tabSerializationOptions);
            this.ExecuteScriptAsync($"window.angularApi.updateTab({tabJson})");
        });
    }
}

public class TabListBrowserApi(TabListBrowser browser) : BrowserApi(browser)
{
}
