using BrowserHost.CefInfrastructure;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Features.Tabs;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBrowserApi TabListApi { get; }
    public FileDownloadsBrowserApi FileDownloadsApi { get; }

    public ActionContextBrowser()
        : base("/action-context")
    {
        TabListApi = new TabListBrowserApi(this);
        FileDownloadsApi = new FileDownloadsBrowserApi(this);

        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
    }
}

