using BrowserHost.CefInfrastructure;
using BrowserHost.Features.Tabs;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBrowserApi TabListApi { get; }

    public ActionContextBrowser()
        : base("/action-context")
    {
        TabListApi = new TabListBrowserApi(this);

        RegisterSecondaryApi(new TabListBrowserApi(this), "tabsApi");
    }
}

