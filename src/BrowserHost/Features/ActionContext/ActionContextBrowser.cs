using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBackendApi TabListApi { get; }
    public FileDownloadsBackendApi FileDownloadsApi { get; }
    public WorkspacesBackendApi WorkspacesApi { get; }
    public PinnedTabsBackendApi PinnedTabsApi { get; }

    public ActionContextBrowser(PubSub pubSub)
        : base("/action-context", disableContextMenu: true)
    {
        TabListApi = new TabListBackendApi(pubSub);
        FileDownloadsApi = new FileDownloadsBackendApi(pubSub);
        WorkspacesApi = new WorkspacesBackendApi(pubSub);
        PinnedTabsApi = new PinnedTabsBackendApi(pubSub);

        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
        RegisterSecondaryApi(WorkspacesApi, "workspacesApi");
        RegisterSecondaryApi(PinnedTabsApi, "pinnedTabsApi");
    }
}

