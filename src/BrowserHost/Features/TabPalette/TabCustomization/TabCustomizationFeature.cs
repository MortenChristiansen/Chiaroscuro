using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Utilities;
using System.Linq;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(
    MainWindow window,
    PubSub pubSub,
    IBrowserContext browserContext,
    TabCustomizationBrowserApi tabCustomizationApi,
    TabsBrowserApi tabsApi,
    TabCustomizationStateManager state
    ) : Feature(window, pubSub)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Subscribe<TabCustomTitleChangedEvent>((e) =>
        {
            var customization = state.SaveCustomization(e.TabId, c => c with { CustomTitle = e.CustomTitle });
            tabsApi.UpdateTabCustomization(new(e.TabId, customization?.CustomTitle));
        });
        PubSub.Subscribe<TabDisableFixedAddressChangedEvent>((e) =>
        {
            state.SaveCustomization(e.TabId, c => c with { DisableFixedAddress = e.IsDisabled });
        });
        PubSub.Subscribe<TabClosedEvent>((e) => state.DeleteCustomization(e.Tab.Id));
        PubSub.Subscribe<EphemeralTabsExpiredEvent>((e) =>
        {
            foreach (var tabId in e.TabIds)
                state.DeleteCustomization(tabId);
        });

        InitializeCustomizations();
    }

    private void InitializeCustomizations()
    {
        var allCustomizations = state.GetAllCustomizations();
        tabsApi.SetTabCustomizations([.. allCustomizations.Select(c => new TabCustomizationDto(c.TabId, c.CustomTitle))]);
    }

    public void InitializeCustomSettings()
    {
        if (browserContext.CurrentTabId is not { } tabId)
            return;

        var customization = state.GetCustomization(tabId);
        tabCustomizationApi.InitCustomSettings(customization);
    }

    public TabCustomizationDataV1 GetCustomizationsForTab(string tabId) =>
        state.GetCustomization(tabId);
}
