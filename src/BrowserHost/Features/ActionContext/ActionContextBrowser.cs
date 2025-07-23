using BrowserHost.CefInfrastructure;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Features.Tabs;
using BrowserHost.Features.Workspaces;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBrowserApi TabListApi { get; }
    public FileDownloadsBrowserApi FileDownloadsApi { get; }
    public WorkspacesBrowserApi WorkspacesApi { get; }

    public ActionContextBrowser()
        : base("/action-context", disableContextMenu: true)
    {
        TabListApi = new TabListBrowserApi();
        FileDownloadsApi = new FileDownloadsBrowserApi();
        WorkspacesApi = new WorkspacesBrowserApi();

        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
        RegisterSecondaryApi(WorkspacesApi, "workspacesApi");
    }
}

