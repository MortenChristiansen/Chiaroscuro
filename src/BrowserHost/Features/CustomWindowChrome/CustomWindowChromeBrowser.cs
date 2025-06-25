using BrowserHost.CefInfrastructure;
using CefSharp;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : BaseBrowser<CustomWindowChromeBrowserApi>
{
    public override CustomWindowChromeBrowserApi Api { get; }

    public CustomWindowChromeBrowser()
        : base("/")
    {
        Api = new CustomWindowChromeBrowserApi(this);
    }

    public void ChangeAddress(string address) =>
        this.ExecuteScriptAsync($"window.angularApi.changeAddress('{address}')");
}
