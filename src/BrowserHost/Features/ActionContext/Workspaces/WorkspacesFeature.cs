using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost.Features.ActionContext.Workspaces;

public class WorkspacesFeature(MainWindow window, WorkspacesBrowserApi workspacesApi, TabsBrowserApi tabsApi) : Feature(window)
{
    private WorkspaceDtoV1[] _workspaces = [];
    private string _currentWorkspaceId = null!;
    private bool _hasLoggedInitialWorkspaceTime = false;

    public WorkspaceDtoV1 CurrentWorkspace => _workspaces.FirstOrDefault(ws => ws.WorkspaceId == _currentWorkspaceId) ?? throw new ArgumentException("Error getting current workspace");

    public override void Configure()
    {
        var tabsFeature = Window.GetFeature<TabsFeature>();
        PubSub.Instance.Subscribe<TabsChangedEvent>(e =>
            _workspaces = WorkspaceStateManager.SaveWorkspaceTabs(
                _currentWorkspaceId,
                e.Tabs.Select((t, idx) => CreateTabState(t, idx, tabsFeature)),
                e.EphemeralTabStartIndex,
                e.Folders.Select(f => new FolderDtoV1(
                    f.Id,
                    f.Name,
                    f.StartIndex,
                    f.EndIndex
                ))
            )
        );
        PubSub.Instance.Subscribe<WorkspaceActivatedEvent>(e =>
        {
            _currentWorkspaceId = e.WorkspaceId;
            workspacesApi.WorkspaceActivated(e.WorkspaceId);
            Window.WorkspaceColor = GetCurrentWorkspaceColor();

            if (!_hasLoggedInitialWorkspaceTime)
            {
                _hasLoggedInitialWorkspaceTime = true;
                Measure.Event("Initial workspace loaded");
            }
        });
        PubSub.Instance.Subscribe<WorkspaceCreatedEvent>(e =>
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

            PubSub.Instance.Publish(new WorkspaceActivatedEvent(newWorkspace.WorkspaceId));
        });
        PubSub.Instance.Subscribe<WorkspaceUpdatedEvent>(e =>
        {
            var workspace = GetWorkspaceById(e.WorkspaceId);
            workspace = workspace with
            {
                Name = e.Name,
                Color = e.Color,
                Icon = e.Icon
            };

            _workspaces = WorkspaceStateManager.UpdateWorkspace(workspace);

            if (e.WorkspaceId == _currentWorkspaceId)
                Window.WorkspaceColor = GetCurrentWorkspaceColor();

            NotifyFrontendOfUpdatedWorkspaces();
        });
        PubSub.Instance.Subscribe<WorkspaceDeletedEvent>(e =>
        {
            if (_workspaces.Length == 1)
                throw new InvalidOperationException("Cannot delete the last workspace.");

            _workspaces = WorkspaceStateManager.DeleteWorkspace(e.WorkspaceId);
            NotifyFrontendOfUpdatedWorkspaces();

            if (e.WorkspaceId == _currentWorkspaceId)
                PubSub.Instance.Publish(new WorkspaceActivatedEvent(_workspaces[0].WorkspaceId));
        });
    }

    public override void Start()
    {
        _workspaces = WorkspaceStateManager.RestoreWorkspacesFromDisk();
        _currentWorkspaceId = _workspaces[0].WorkspaceId;
        RestoreFrontendWorkspaces();

        Window.WorkspaceColor = GetCurrentWorkspaceColor();
        PubSub.Instance.Publish(new WorkspaceActivatedEvent(_currentWorkspaceId));

        if (App.Options.LaunchUrl != null)
            PubSub.Instance.Publish(new NavigationStartedEvent(App.Options.LaunchUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));
    }

    private WorkspaceTabStateDtoV1 CreateTabState(TabUiStateDto tab, int tabIndex, TabsFeature tabsFeature)
    {
        var customization = TabCustomizationFeature.GetCustomizationsForTab(tab.Id);
        var isBookmarked = IsTabBookmarked(tab.Id);
        var browserTab = tabsFeature.GetTabBrowserById(tab.Id);
        return new WorkspaceTabStateDtoV1(
            tab.Id,
            browserTab?.GetAddressToPersist(isBookmarked, customization) ?? "",
            browserTab?.GetTitleToPersist(isBookmarked, customization) ?? "",
            browserTab?.GetFaviconToPersist(isBookmarked, customization) ?? "",
            tab.IsActive,
            tab.Created
        );
    }

    private Color GetCurrentWorkspaceColor() =>
        (Color)ColorConverter.ConvertFromString(CurrentWorkspace.Color);

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
        {
            RestoreOriginalTabAddress();
            return true;
        }

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
                    PubSub.Instance.Publish(new WorkspaceActivatedEvent(targetWorkspace.WorkspaceId));
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
        tabsApi.CloseTab(tab.TabId);
        RemoveTabFromWorkspace(tab.TabId);

        PubSub.Instance.Publish(new WorkspaceActivatedEvent(targetWorkspace.WorkspaceId));
        tabsApi.AddTab(new(tab.TabId, tab.Title, tab.Favicon, tab.Created));
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
        workspacesApi.WorkspacesChanged([.. _workspaces.Select(ws => new WorkspaceDescriptionDto(ws.WorkspaceId, ws.Name, ws.Color, ws.Icon))]);
    }

    private void RestoreFrontendWorkspaces()
    {
        workspacesApi.SetWorkspaces(
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

    private void RestoreOriginalTabAddress()
    {
        var tab = Window.CurrentTab;
        if (tab == null) return;

        var isPinned = Window.GetFeature<PinnedTabsFeature>().IsTabPinned(tab.Id);
        var isBookmarked = Window.GetFeature<WorkspacesFeature>().IsTabBookmarked(tab.Id);

        if (!isPinned && !isBookmarked)
            return;

        tab.RestoreOriginalAddress();
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

    public bool IsTabBookmarked(string tabId)
    {
        var workspace = _workspaces.FirstOrDefault(ws => ws.Tabs.Any(t => t.TabId == tabId));
        if (workspace == null)
            return false;

        var tabIndex = workspace.Tabs.ToList().FindIndex(t => t.TabId == tabId);
        return workspace.EphemeralTabStartIndex > tabIndex;
    }
}
