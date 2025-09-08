using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost.Features.ActionContext.Workspaces;

public class WorkspacesFeature(MainWindow window) : Feature(window)
{
    private WorkspaceDtoV1[] _workspaces = [];
    private string _currentWorkspaceId = null!;

    public WorkspaceDtoV1 CurrentWorkspace => _workspaces.FirstOrDefault(ws => ws.WorkspaceId == _currentWorkspaceId) ?? throw new ArgumentException("Error getting current workspace");

    public override void Configure()
    {
        var tabsFeature = Window.GetFeature<TabsFeature>();
        PubSub.Subscribe<TabsChangedEvent>(e =>
            _workspaces = WorkspaceStateManager.SaveWorkspaceTabs(
                _currentWorkspaceId,
                e.Tabs.Select(t => CreateWorkspaceTabState(t, e.EphemeralTabStartIndex, tabsFeature)),
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
        _workspaces = WorkspaceStateManager.RestoreWorkspacesFromDisk();
        _currentWorkspaceId = _workspaces[0].WorkspaceId;
        RestoreFrontendWorkspaces();

        Window.WorkspaceColor = GetCurrentWorkspaceColor();
        PubSub.Publish(new WorkspaceActivatedEvent(_currentWorkspaceId));

        if (App.Options.LaunchUrl != null)
            PubSub.Publish(new NavigationStartedEvent(App.Options.LaunchUrl, UseCurrentTab: false, SaveInHistory: true));
    }

    private Color GetCurrentWorkspaceColor() =>
        (Color)ColorConverter.ConvertFromString(CurrentWorkspace.Color);

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
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
                if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
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

    private void RestoreFrontendWorkspaces()
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

    private WorkspaceTabStateDtoV1 CreateWorkspaceTabState(TabUiStateDto tabUiState, int ephemeralTabStartIndex, TabsFeature tabsFeature)
    {
        var currentAddress = tabsFeature.GetTabBrowserById(tabUiState.Id)?.Address ?? "";
        var tabIndex = Array.FindIndex(_workspaces.FirstOrDefault(ws => ws.WorkspaceId == _currentWorkspaceId)?.Tabs ?? [], t => t.TabId == tabUiState.Id);
        var isNewTab = tabIndex == -1;
        var isPersistentTab = !isNewTab && tabIndex < ephemeralTabStartIndex;
        
        string? originalAddress = null;
        if (isPersistentTab && !isNewTab)
        {
            // Preserve existing original address for persistent tabs
            var existingTab = CurrentWorkspace.Tabs[tabIndex];
            originalAddress = existingTab.OriginalAddress ?? existingTab.Address;
        }
        else if (isPersistentTab && isNewTab)
        {
            // New persistent tab (bookmark) - set current address as original
            originalAddress = currentAddress;
        }
        // For ephemeral tabs, OriginalAddress remains null

        return new WorkspaceTabStateDtoV1(
            tabUiState.Id,
            currentAddress,
            tabUiState.Title,
            tabUiState.Favicon,
            tabUiState.IsActive,
            tabUiState.Created,
            originalAddress);
    }

    public WorkspaceTabStateDtoV1 GetTabById(string tabId) =>
        _workspaces.SelectMany(ws => ws.Tabs)
            .FirstOrDefault(t => t.TabId == tabId) ?? throw new ArgumentException($"Tab with ID {tabId} not found in current workspace.");

    public WorkspaceTabStateDtoV1[] GetTabsForWorkspace(string workspaceId) =>
        _workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId)?.Tabs
            ?? throw new ArgumentException($"No tabs found for workspace with ID {workspaceId}.");

    public void ReturnToOriginalAddress(string tabId)
    {
        var tab = GetTabById(tabId);
        var workspace = _workspaces.FirstOrDefault(ws => ws.Tabs.Any(t => t.TabId == tabId));
        if (workspace == null)
            return;

        var tabIndex = Array.FindIndex(workspace.Tabs, t => t.TabId == tabId);
        var isPersistentTab = tabIndex < workspace.EphemeralTabStartIndex;
        
        if (!isPersistentTab || string.IsNullOrEmpty(tab.OriginalAddress))
            return;

        var tabBrowser = Window.GetFeature<TabsFeature>().GetTabBrowserById(tabId);
        tabBrowser.SetAddress(tab.OriginalAddress, setManualAddress: true);
    }
}
