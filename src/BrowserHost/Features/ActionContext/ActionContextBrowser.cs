using BrowserHost.CefInfrastructure;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Features.PinnedTabs;
using BrowserHost.Features.Tabs;
using BrowserHost.Features.Workspaces;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBrowserApi TabListApi { get; } = new();
    public FileDownloadsBrowserApi FileDownloadsApi { get; } = new();
    public WorkspacesBrowserApi WorkspacesApi { get; } = new();
    public PinnedTabsBrowserApi PinnedTabsApi { get; } = new();

    public ActionContextBrowser()
        : base("/action-context", disableContextMenu: true)
    {
        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
        RegisterSecondaryApi(WorkspacesApi, "workspacesApi");
        RegisterSecondaryApi(PinnedTabsApi, "pinnedTabsApi");
    }
}

