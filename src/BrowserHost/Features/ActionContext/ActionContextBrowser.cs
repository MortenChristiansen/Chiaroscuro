using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;

namespace BrowserHost.Features.ActionContext;

public class ActionContextBrowser : Browser
{
    public TabListBackendApi TabListApi { get; } = new(App.PubSub);
    public FileDownloadsBackendApi FileDownloadsApi { get; } = new(App.PubSub);
    public WorkspacesBackendApi WorkspacesApi { get; } = new(App.PubSub);
    public PinnedTabsBackendApi PinnedTabsApi { get; } = new(App.PubSub);

    public ActionContextBrowser()
        : base("/action-context", disableContextMenu: true)
    {
        RegisterSecondaryApi(TabListApi, "tabsApi");
        RegisterSecondaryApi(FileDownloadsApi, "fileDownloadsApi");
        RegisterSecondaryApi(WorkspacesApi, "workspacesApi");
        RegisterSecondaryApi(PinnedTabsApi, "pinnedTabsApi");
    }
}

