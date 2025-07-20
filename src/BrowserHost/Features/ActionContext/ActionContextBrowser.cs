using BrowserHost.CefInfrastructure;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Features.Tabs;
using BrowserHost.Features.Workspaces;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBrowserApi TabListApi { get; }
    public FileDownloadsBrowserApi FileDownloadsApi { get; }
    public WorkspaceListBrowserApi WorkspaceListApi { get; private set; } = null!;

    public ActionContextBrowser()
        : base("/action-context", disableContextMenu: true)
    {
        TabListApi = new TabListBrowserApi();
        FileDownloadsApi = new FileDownloadsBrowserApi();

        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
    }

    public void SetWorkspacesFeature(WorkspacesFeature workspacesFeature)
    {
        WorkspaceListApi = new WorkspaceListBrowserApi(workspacesFeature);
        RegisterSecondaryApi(WorkspaceListApi, "workspacesApi");
    }
}

