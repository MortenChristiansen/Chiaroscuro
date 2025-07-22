using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;

namespace BrowserHost.Features.Workspaces;

public class WorkspacesFeature(MainWindow window) : Feature(window)
{
    private WorkspaceDtoV1[] _workspaces = [];
    private WorkspaceDtoV1 _currentWorkspace = null!;

    public override void Configure()
    {
        _workspaces = WorkspaceStateManager.RestoreWorkspacesFromDisk();
        _currentWorkspace = _workspaces[0];
        RestoreWorkspaces();

        var tabsFeature = Window.GetFeature<TabsFeature>();
        PubSub.Subscribe<TabsChangedEvent>(e =>
            WorkspaceStateManager.SaveWorkspacesToDisk(
                _currentWorkspace.WorkspaceId,
                e.Tabs.Select(t => new WorkspaceTabStateDtoV1(
                    t.Id,
                    tabsFeature.GetTabById(t.Id)?.Address ?? "",
                    t.Title,
                    t.Favicon,
                    t.IsActive,
                    t.Created)
                ),
                e.EphemeralTabStartIndex
            )
        );
    }

    public override void Start()
    {
        PubSub.Publish(new WorkspaceActivatedEvent(_currentWorkspace.WorkspaceId));
    }

    private void RestoreWorkspaces()
    {
        Window.ActionContext.SetWorkspaces(
            [.. _workspaces.Select(ws => new WorkspaceDto(
                ws.WorkspaceId,
                ws.Name,
                ws.Color,
                ws.Icon,
                [..ws.Tabs.Select(t => new TabDto(t.TabId, t.Title, t.Favicon, t.Created))],
                ws.EphemeralTabStartIndex,
                ws.Tabs.FirstOrDefault(t => t.IsActive)?.TabId
            ))]
        );
    }

    public WorkspaceDtoV1 GetWorkspaceById(string workspaceId) =>
        _workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId)
            ?? throw new ArgumentException($"Workspace with ID {workspaceId} not found.");
}
