﻿using BrowserHost.Features.PinnedTabs;
using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost.Features.Workspaces;

public class WorkspacesFeature(MainWindow window) : Feature(window)
{
    private WorkspaceDtoV1[] _workspaces = [];
    private string _currentWorkspaceId = null!;

    public WorkspaceDtoV1 CurrentWorkspace => _workspaces.FirstOrDefault(ws => ws.WorkspaceId == _currentWorkspaceId) ?? throw new ArgumentException("Error getting current workspace");

    public override void Configure()
    {
        _workspaces = WorkspaceStateManager.RestoreWorkspacesFromDisk();
        _currentWorkspaceId = _workspaces[0].WorkspaceId;
        RestoreWorkspaces();

        var tabsFeature = Window.GetFeature<TabsFeature>();
        PubSub.Subscribe<TabsChangedEvent>(e =>
            _workspaces = WorkspaceStateManager.SaveWorkspaceTabs(
                _currentWorkspaceId,
                e.Tabs.Select(t => new WorkspaceTabStateDtoV1(
                    t.Id,
                    tabsFeature.GetTabBrowserById(t.Id)?.Address ?? "",
                    t.Title,
                    t.Favicon,
                    t.IsActive,
                    t.Created)
                ),
                e.EphemeralTabStartIndex,
                e.Folders.Select(f => new FolderDtoV1(
                    f.Id,
                    f.Name,
                    f.StartIndex,
                    f.EndIndex
                ))
            )
        );
        PubSub.Subscribe<WorkspaceActivatedEvent>(e =>
        {
            _currentWorkspaceId = e.WorkspaceId;
            Window.ActionContext.WorkspaceActivated(e.WorkspaceId);
            Window.WorkspaceColor = GetCurrentWorkspaceColor();
        });
        PubSub.Subscribe<WorkspaceCreatedEvent>(e =>
        {
            var newWorkspace = new WorkspaceDtoV1(
                e.WorkspaceId,
                e.Name,
                e.Color,
                e.Icon,
                [],
                0
            );
            _workspaces = WorkspaceStateManager.CreateWorkspace(newWorkspace);
            NotifyFrontendOfUpdatedWorkspaces();

            PubSub.Publish(new WorkspaceActivatedEvent(newWorkspace.WorkspaceId));
        });
        PubSub.Subscribe<WorkspaceUpdatedEvent>(e =>
        {
            var workspace = GetWorkspaceById(e.WorkspaceId);
            workspace = workspace with
            {
                Name = e.Name,
                Color = e.Color,
                Icon = e.Icon
            };
            if (e.WorkspaceId == _currentWorkspaceId)
            {
                _currentWorkspaceId = e.WorkspaceId;
                Window.WorkspaceColor = GetCurrentWorkspaceColor();
            }

            _workspaces = WorkspaceStateManager.UpdateWorkspace(workspace);
            NotifyFrontendOfUpdatedWorkspaces();
        });
        PubSub.Subscribe<WorkspaceDeletedEvent>(e =>
        {
            if (_workspaces.Length == 1)
                throw new InvalidOperationException("Cannot delete the last workspace.");

            _workspaces = WorkspaceStateManager.DeleteWorkspace(e.WorkspaceId);
            NotifyFrontendOfUpdatedWorkspaces();

            if (e.WorkspaceId == _currentWorkspaceId)
                PubSub.Publish(new WorkspaceActivatedEvent(_workspaces[0].WorkspaceId));
        });
    }

    public override void Start()
    {
        Window.WorkspaceColor = GetCurrentWorkspaceColor();
        PubSub.Publish(new WorkspaceActivatedEvent(_currentWorkspaceId));
    }

    private Color GetCurrentWorkspaceColor() =>
        (Color)ColorConverter.ConvertFromString(CurrentWorkspace.Color);

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var index = e.Key switch
            {
                Key.D1 => 0,
                Key.D2 => 1,
                Key.D3 => 2,
                Key.D4 => 3,
                Key.D5 => 4,
                Key.D6 => 5,
                Key.D7 => 6,
                Key.D8 => 7,
                Key.D9 => 8,
                _ => -1
            };

            if (index >= 0 && index < _workspaces.Length && _workspaces[index] != CurrentWorkspace)
            {
                var targetWorkspace = _workspaces[index];
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    MoveCurrentTabToWorkspace(targetWorkspace);
                }
                else
                {
                    PubSub.Publish(new WorkspaceActivatedEvent(targetWorkspace.WorkspaceId));
                }
            }
        }
        return base.HandleOnPreviewKeyDown(e);
    }

    private void MoveCurrentTabToWorkspace(WorkspaceDtoV1 targetWorkspace)
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null)
            return;

        if (Window.GetFeature<PinnedTabsFeature>().IsTabPinned(currentTab.Id))
            return;

        var tab = GetTabById(currentTab.Id);
        Window.ActionContext.CloseTab(tab.TabId);
        RemoveTabFromWorkspace(tab.TabId);

        PubSub.Publish(new WorkspaceActivatedEvent(targetWorkspace.WorkspaceId));
        Window.ActionContext.AddTab(new(tab.TabId, tab.Title, tab.Favicon, tab.Created));
    }

    private void RemoveTabFromWorkspace(string tabId)
    {
        var isPersistentTab = CurrentWorkspace.Tabs.ToList().FindIndex(t => t.TabId == tabId) < CurrentWorkspace.EphemeralTabStartIndex;
        var updatedWorkspace = CurrentWorkspace with
        {
            Tabs = [.. CurrentWorkspace.Tabs.Where(t => t.TabId != tabId)],
            EphemeralTabStartIndex = isPersistentTab ? CurrentWorkspace.EphemeralTabStartIndex - 1 : CurrentWorkspace.EphemeralTabStartIndex,
        };
        _workspaces = WorkspaceStateManager.UpdateWorkspace(updatedWorkspace);
    }

    private void NotifyFrontendOfUpdatedWorkspaces()
    {
        Window.ActionContext.WorkspacesChanged([.. _workspaces.Select(ws => new WorkspaceDescriptionDto(ws.WorkspaceId, ws.Name, ws.Color, ws.Icon))]);
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
                ws.Tabs.FirstOrDefault(t => t.IsActive)?.TabId,
                [..ws.Folders.Select(f => new FolderDto(f.Id, f.Name, f.StartIndex, f.EndIndex))]
            ))]
        );
    }

    public WorkspaceDtoV1 GetWorkspaceById(string workspaceId) =>
        _workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId)
            ?? throw new ArgumentException($"Workspace with ID {workspaceId} not found.");

    public WorkspaceTabStateDtoV1 GetTabById(string tabId) =>
        _workspaces.SelectMany(ws => ws.Tabs)
            .FirstOrDefault(t => t.TabId == tabId) ?? throw new ArgumentException($"Tab with ID {tabId} not found in current workspace.");

    public WorkspaceTabStateDtoV1[] GetTabsForWorkspace(string workspaceId) =>
        _workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId)?.Tabs
            ?? throw new ArgumentException($"No tabs found for workspace with ID {workspaceId}.");
}
