using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public class PinnedTabsFeature(MainWindow window) : Feature(window)
{
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
        PubSub.Subscribe<TabPinnedEvent>(e =>
        {
            var tab = Window.GetFeature<TabsFeature>().GetTabBrowserById(e.TabId);
            var activateTabId = Window.CurrentTab?.Id;
            AddPinnedTabToState(new PinnedTabDtoV1(e.TabId, tab.Title, tab.Favicon, tab.Address), activateTabId);
            Window.ActionContext.CloseTab(e.TabId, activateNext: false);
            NotifyFrontendOfUpdatedPinnedTabs();
        });
        PubSub.Subscribe<TabUnpinnedEvent>(e =>
        {
            var tab = Window.GetFeature<TabsFeature>().GetTabBrowserById(e.TabId);
            RemovePinnedTabFromState(e.TabId);
            NotifyFrontendOfUpdatedPinnedTabs();
            Window.ActionContext.AddTab(new(e.TabId, tab.Title, tab.Favicon, DateTimeOffset.UtcNow)); // We don't currently store creation info for pinned tabs
        });
        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            RemovePinnedTabFromState(e.Tab.Id);
            NotifyFrontendOfUpdatedPinnedTabs();
        });
        PubSub.Subscribe<TabUrlLoadedSuccessfullyEvent>(e => UpdatePinnedTabState(e.TabId));
        PubSub.Subscribe<TabFaviconUrlChangedEvent>(e => UpdatePinnedTabState(e.TabId));
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

        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control && activeTabId != null)
        {
            if (_pinnedTabData.ActiveTabId != null)
                PubSub.Publish(new TabUnpinnedEvent(_pinnedTabData.ActiveTabId));
            else
                PubSub.Publish(new TabPinnedEvent(activeTabId));

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void UpdatePinnedTabState(string tabId)
    {
        var updatedTab = Window.GetFeature<TabsFeature>().GetTabBrowserById(tabId);
        if (!IsTabPinned(tabId))
            return;

        var customizations = TabCustomizationFeature.GetCustomizationsForTab(tabId);
        if (customizations.DisableFixedAddress == true)
        {
            // By default, the persisted state for pinned tabs is not updated. However, if fixed addresses are disable then we do want to update it.
            _pinnedTabData = PinnedTabsStateManager.SavePinnedTabs(_pinnedTabData with
            {
                PinnedTabs = [.. _pinnedTabData.PinnedTabs.Where(t => t.Id != tabId), new PinnedTabDtoV1(tabId, updatedTab.Title, updatedTab.Favicon, updatedTab.Address)]
            });
        }
    }

    public PinnedTabDtoV1[] GetPinnedTabs() =>
        _pinnedTabData.PinnedTabs;

    public bool IsTabPinned(string tabId) =>
        _pinnedTabData.PinnedTabs.Any(t => t.Id == tabId);

    public string? GetActivePinnedTabId() =>
        _pinnedTabData.ActiveTabId;
}
