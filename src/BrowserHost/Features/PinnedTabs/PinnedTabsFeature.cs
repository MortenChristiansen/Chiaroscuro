using BrowserHost.Features.Tabs;
using BrowserHost.Features.Workspaces;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.PinnedTabs;

public class PinnedTabsFeature(MainWindow window) : Feature(window)
{
    // TODO
    // - Click to activate pinned tab
    // - Drag to reorder pinned tabs?
    // - Drag to unpin pinned tab
    // - Drag to pin tab
    // - Horizontal layout for pinned tabs (scalable)
    // - A different icon for unpinning
    // - Handle updates to pinned tabs (e.g. title, favicon, address)
    // - Are we absolutely sure that treating pinned tabs as something of a workspace makes sense?

    public const string WorkspaceId = "pinned-tabs-workspace";

    private PinnedTabDataV1 _pinnedTabData = null!;

    public override void Configure()
    {
        _pinnedTabData = PinnedTabsStateManager.RestorePinnedTabsFromDisk();
        NotifyFrontendOfUpdatedPinnedTabs();

        PubSub.Subscribe<TabActivatedEvent>(e =>
        {
            var newActiveTab = _pinnedTabData.PinnedTabs.FirstOrDefault(t => t.Id == e.TabId);

            _pinnedTabData = PinnedTabsStateManager.SavePinnedTabs(_pinnedTabData with
            {
                ActiveTabId = newActiveTab?.Id,
                PinnedTabs = _pinnedTabData.PinnedTabs
            });
            NotifyFrontendOfUpdatedPinnedTabs();
        });
        PubSub.Subscribe<PinnedTabUnpinnedEvent>(e => UnpinTab(e.TabId));
        PubSub.Subscribe<TabMovedToNewWorkspaceEvent>(e =>
        {
            var workspaceFeature = Window.GetFeature<WorkspacesFeature>();
            if (e.MoveType == WorkspaceTabMoveType.Pin)
            {
                var tab = Window.GetFeature<TabsFeature>().GetTabBrowserById(e.TabId);
                var activateTabId = Window.CurrentTab?.Id;
                AddPinnedTabToState(new PinnedTabDtoV1(e.TabId, tab.Title, tab.Favicon, tab.Address), activateTabId);
                NotifyFrontendOfUpdatedPinnedTabs();
                Window.ActionContext.CloseTab(e.TabId);
            }
            if (e.MoveType == WorkspaceTabMoveType.Unpin)
            {
                var tab = Window.GetFeature<TabsFeature>().GetTabBrowserById(e.TabId);
                RemovePinnedTabFromState(e.TabId);
                NotifyFrontendOfUpdatedPinnedTabs();
                Window.ActionContext.AddTab(new(e.TabId, tab.Title, tab.Favicon, DateTimeOffset.UtcNow)); // We don't currently store creation info for pinned tabs
            }
        });
    }

    private void NotifyFrontendOfUpdatedPinnedTabs()
    {
        Window.ActionContext.SetPinnedTabs(
            [.. _pinnedTabData.PinnedTabs.Select(t => new PinnedTabDto(t.Id, t.Title, t.Favicon))],
            _pinnedTabData.ActiveTabId
        );
    }

    private void AddPinnedTabToState(PinnedTabDtoV1 tab, string? activateTabId)
    {
        var activeTabIsPinned = _pinnedTabData.PinnedTabs.Any(t => t.Id == activateTabId) || activateTabId == tab.Id;
        _pinnedTabData = PinnedTabsStateManager.SavePinnedTabs(_pinnedTabData with
        {
            ActiveTabId = activeTabIsPinned ? activateTabId : null,
            PinnedTabs = [.. _pinnedTabData.PinnedTabs, tab]
        });
    }

    private void RemovePinnedTabFromState(string tabId)
    {
        _pinnedTabData = PinnedTabsStateManager.SavePinnedTabs(_pinnedTabData with
        {
            ActiveTabId = _pinnedTabData.ActiveTabId == tabId ? null : _pinnedTabData.ActiveTabId,
            PinnedTabs = [.. _pinnedTabData.PinnedTabs.Where(t => t.Id != tabId)]
        });
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        var activeTabId = Window.CurrentTab?.Id;

        if (e.Key == Key.P && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && activeTabId != null)
        {
            if (_pinnedTabData.ActiveTabId != null)
                UnpinTab(_pinnedTabData.ActiveTabId);
            else
                PinTab(activeTabId);

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void PinTab(string activeTabId)
    {
        PubSub.Publish(new TabMovedToNewWorkspaceEvent(
            activeTabId,
            Window.GetFeature<WorkspacesFeature>().CurrentWorkspace.WorkspaceId,
            WorkspaceId,
            WorkspaceTabMoveType.Pin
        ));
    }

    private void UnpinTab(string tabId)
    {
        PubSub.Publish(new TabMovedToNewWorkspaceEvent(
            tabId,
            WorkspaceId,
            Window.GetFeature<WorkspacesFeature>().CurrentWorkspace.WorkspaceId,
            WorkspaceTabMoveType.Unpin
        ));
    }

    public PinnedTabDtoV1[] GetPinnedTabs()
    {
        return _pinnedTabData.PinnedTabs;
    }
}
