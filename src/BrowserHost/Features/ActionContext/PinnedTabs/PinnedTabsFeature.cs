using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public class PinnedTabsFeature(MainWindow window, PubSub pubSub, IBrowserContext context, TabsBrowserApi tabsApi, PinnedTabsBrowserApi pinnedTabsApi, PinnedTabsStateManager stateManager) : Feature(window, pubSub)
{
    private PinnedTabDataV1 _pinnedTabData = null!;

    public override void Configure()
    {
        _pinnedTabData = stateManager.RestorePinnedTabsFromDisk();
        NotifyFrontendOfUpdatedPinnedTabs();

        PubSub.Handle<PinTabCommand>(cmd =>
        {
            var tab = context.GetFeature<TabsFeature>().GetTabBrowserById(cmd.TabId);
            var activateTabId = context.CurrentTab?.Id;
            AddPinnedTabToState(new PinnedTabDtoV1(cmd.TabId, tab.Title, tab.Favicon, tab.Address), activateTabId);
            tabsApi.CloseTab(cmd.TabId, activateNext: false);
            NotifyFrontendOfUpdatedPinnedTabs();

            PubSub.Publish(new TabPinnedEvent(cmd.TabId));
        });
        PubSub.Handle<UnpinTabCommand>(cmd =>
        {
            var tab = context.GetFeature<TabsFeature>().GetTabBrowserById(cmd.TabId);
            RemovePinnedTabFromState(cmd.TabId);
            NotifyFrontendOfUpdatedPinnedTabs();
            tabsApi.AddTab(new(cmd.TabId, tab.Title, tab.Favicon, DateTimeOffset.UtcNow)); // We don't currently store creation info for pinned tabs

            PubSub.Publish(new TabUnpinnedEvent(cmd.TabId));
        });

        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            RemovePinnedTabFromState(e.TabId);
            NotifyFrontendOfUpdatedPinnedTabs();
        });
        PubSub.Subscribe<TabActivatedEvent>(e =>
        {
            var newActiveTab = _pinnedTabData.PinnedTabs.FirstOrDefault(t => t.Id == e.TabId);

            _pinnedTabData = stateManager.SavePinnedTabs(_pinnedTabData with
            {
                ActiveTabId = newActiveTab?.Id,
                PinnedTabs = _pinnedTabData.PinnedTabs
            });
            NotifyFrontendOfUpdatedPinnedTabs();
        });
        PubSub.Subscribe<TabUrlLoadedSuccessfullyEvent>(e => UpdatePinnedTabState(e.TabId));
        PubSub.Subscribe<TabFaviconUrlChangedEvent>(e => UpdatePinnedTabState(e.TabId));
    }

    private void NotifyFrontendOfUpdatedPinnedTabs()
    {
        pinnedTabsApi.SetPinnedTabs(
            [.. _pinnedTabData.PinnedTabs.Select(t => new PinnedTabDto(t.Id, t.Title, t.Favicon))],
            _pinnedTabData.ActiveTabId
        );
    }

    private void AddPinnedTabToState(PinnedTabDtoV1 tab, string? activateTabId)
    {
        var activeTabIsPinned = _pinnedTabData.PinnedTabs.Any(t => t.Id == activateTabId) || activateTabId == tab.Id;
        _pinnedTabData = stateManager.SavePinnedTabs(_pinnedTabData with
        {
            ActiveTabId = activeTabIsPinned ? activateTabId : null,
            PinnedTabs = [.. _pinnedTabData.PinnedTabs, tab]
        });
    }

    private void RemovePinnedTabFromState(string tabId)
    {
        if (!IsTabPinned(tabId) && _pinnedTabData.ActiveTabId != tabId)
            return;

        _pinnedTabData = stateManager.SavePinnedTabs(_pinnedTabData with
        {
            ActiveTabId = _pinnedTabData.ActiveTabId == tabId ? null : _pinnedTabData.ActiveTabId,
            PinnedTabs = [.. _pinnedTabData.PinnedTabs.Where(t => t.Id != tabId)]
        });
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        var activeTabId = context.CurrentTab?.Id;

        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control && activeTabId != null)
        {
            if (_pinnedTabData.ActiveTabId != null)
                PubSub.Send(new UnpinTabCommand(_pinnedTabData.ActiveTabId));
            else
                PubSub.Send(new PinTabCommand(activeTabId));

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void UpdatePinnedTabState(string tabId)
    {
        var updatedTab = context.GetFeature<TabsFeature>().GetTabBrowserById(tabId);
        if (!IsTabPinned(tabId))
            return;

        var customizations = context.GetFeature<TabCustomizationFeature>().GetCustomizationsForTab(tabId);
        if (customizations.DisableFixedAddress == true)
        {
            // By default, the persisted state for pinned tabs is not updated. However, if fixed addresses are disabled then we do want to update it.
            _pinnedTabData = stateManager.SavePinnedTabs(_pinnedTabData with
            {
                PinnedTabs = [.. _pinnedTabData
                    .PinnedTabs
                    .Select(t => t.Id == tabId ? new PinnedTabDtoV1(tabId, updatedTab.Title, updatedTab.Favicon, updatedTab.Address) : t)
                ]
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
