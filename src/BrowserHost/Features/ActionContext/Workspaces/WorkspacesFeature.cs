using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Logging;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost.Features.ActionContext.Workspaces;

public class WorkspacesFeature(
    MainWindow window,
    PubSub pubSub,
    IBrowserContext context,
    WorkspacesBrowserApi workspacesApi,
    TabsBrowserApi tabsApi,
    WorkspaceStateManager stateManager
) : Feature(window, pubSub)
{
    private WorkspaceDtoV1[] _workspaces = [];
    private string _currentWorkspaceId = null!;
    private bool _hasLoggedInitialWorkspaceTime = false;

    public WorkspaceDtoV1 CurrentWorkspace => _workspaces.FirstOrDefault(ws => ws.WorkspaceId == _currentWorkspaceId) ?? throw new ArgumentException("Error getting current workspace");

    public override void Configure()
    {
        PubSub.Handle<ChangeTabsCommand>(cmd =>
        {
            var tabsFeature = context.GetFeature<TabsFeature>();
            _workspaces = stateManager.SaveWorkspaceTabs(
                _currentWorkspaceId,
                cmd.Tabs.Select(t => CreateTabState(t, tabsFeature)),
                cmd.EphemeralTabStartIndex,
                cmd.Folders.Select(f => new FolderDtoV1(
                    f.Id,
                    f.Name,
                    f.StartIndex,
                    f.EndIndex
                ))
            );

            PubSub.Publish(new TabsChangedEvent(cmd.Tabs, cmd.EphemeralTabStartIndex, cmd.Folders));
        });
        PubSub.Handle<ActivateWorkspaceCommand>(cmd =>
        {
            _currentWorkspaceId = cmd.WorkspaceId;
            workspacesApi.WorkspaceActivated(cmd.WorkspaceId);
            context.WorkspaceColor = GetCurrentWorkspaceColor();

            if (!_hasLoggedInitialWorkspaceTime)
            {
                _hasLoggedInitialWorkspaceTime = true;
                Measure.Event("Initial workspace loaded");
            }

            PubSub.Publish(new WorkspaceActivatedEvent(cmd.WorkspaceId));
        });
        PubSub.Handle<CreateWorkspaceCommand>(cmd =>
        {
            var newWorkspace = new WorkspaceDtoV1(
                cmd.WorkspaceId,
                cmd.Name,
                cmd.Color,
                cmd.Icon,
                [],
                0
            );
            _workspaces = stateManager.CreateWorkspace(newWorkspace);
            NotifyFrontendOfUpdatedWorkspaces();

            PubSub.Publish(new WorkspaceCreatedEvent(cmd.WorkspaceId, cmd.Name, cmd.Icon, cmd.Color));
            PubSub.Send(new ActivateWorkspaceCommand(newWorkspace.WorkspaceId));
        });
        PubSub.Handle<UpdateWorkspaceCommand>(cmd =>
        {
            var workspace = GetWorkspaceById(cmd.WorkspaceId);
            workspace = workspace with
            {
                Name = cmd.Name,
                Color = cmd.Color,
                Icon = cmd.Icon
            };

            _workspaces = stateManager.UpdateWorkspace(workspace);

            if (cmd.WorkspaceId == _currentWorkspaceId)
                context.WorkspaceColor = GetCurrentWorkspaceColor();

            NotifyFrontendOfUpdatedWorkspaces();

            PubSub.Publish(new WorkspaceUpdatedEvent(cmd.WorkspaceId, cmd.Name, cmd.Icon, cmd.Color));
        });
        PubSub.Handle<DeleteWorkspaceCommand>(cmd =>
        {
            if (_workspaces.Length == 1)
                throw new InvalidOperationException("Cannot delete the last workspace.");

            _workspaces = stateManager.DeleteWorkspace(cmd.WorkspaceId);
            NotifyFrontendOfUpdatedWorkspaces();

            PubSub.Publish(new WorkspaceDeletedEvent(cmd.WorkspaceId));

            if (cmd.WorkspaceId == _currentWorkspaceId)
                PubSub.Send(new ActivateWorkspaceCommand(_workspaces[0].WorkspaceId));
        });
    }

    public override void Start()
    {
        _workspaces = stateManager.RestoreWorkspacesFromDisk();
        _currentWorkspaceId = _workspaces[0].WorkspaceId;
        RestoreFrontendWorkspaces();

        context.WorkspaceColor = GetCurrentWorkspaceColor();
        PubSub.Send(new ActivateWorkspaceCommand(_currentWorkspaceId));

        if (context.AppOptions.LaunchUrl != null)
            PubSub.Send(new StartNavigationCommand(context.AppOptions.LaunchUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));
    }

    private WorkspaceTabStateDtoV1 CreateTabState(TabUiStateDto tab, TabsFeature tabsFeature)
    {
        var customization = context.GetFeature<TabCustomizationFeature>().GetCustomizationsForTab(tab.Id);
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
        if (e.Key == Key.R && context.CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            RestoreOriginalTabAddress();
            return true;
        }

        if (context.CurrentKeyboardModifiers == ModifierKeys.Control || context.CurrentKeyboardModifiers == (ModifierKeys.Control | ModifierKeys.Shift))
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
                if (context.CurrentKeyboardModifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    MoveCurrentTabToWorkspace(targetWorkspace);
                    return true;
                }
                else
                {
                    PubSub.Send(new ActivateWorkspaceCommand(targetWorkspace.WorkspaceId));
                    return true;
                }
            }
        }
        return base.HandleOnPreviewKeyDown(e);
    }

    private void MoveCurrentTabToWorkspace(WorkspaceDtoV1 targetWorkspace)
    {
        var currentTab = context.CurrentTab;
        if (currentTab == null)
            return;

        if (context.GetFeature<PinnedTabsFeature>().IsTabPinned(currentTab.Id))
            return;

        var tab = GetTabById(currentTab.Id);
        tabsApi.CloseTab(tab.TabId);
        RemoveTabFromWorkspace(tab.TabId);

        PubSub.Send(new ActivateWorkspaceCommand(targetWorkspace.WorkspaceId));
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
        _workspaces = stateManager.UpdateWorkspace(updatedWorkspace);
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
        if (context.CurrentTab is not ITabBrowser tab)
            return;

        var isPinned = context.GetFeature<PinnedTabsFeature>().IsTabPinned(tab.Id);
        var isBookmarked = IsTabBookmarked(tab.Id);

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
