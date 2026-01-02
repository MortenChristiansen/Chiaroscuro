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
        PubSub.Handle<ChangeTabCustomTitleCommand>((e) =>
        {
            var customization = state.SaveCustomization(e.TabId, c => c with { CustomTitle = e.CustomTitle });
            tabsApi.UpdateTabCustomization(new(e.TabId, customization?.CustomTitle));
            PubSub.Publish(new TabCustomTitleChangedEvent(e.TabId, e.CustomTitle));
        });
        PubSub.Handle<ChangeTabDisableFixedAddressCommand>((e) =>
        {
            state.SaveCustomization(e.TabId, c => c with { DisableFixedAddress = e.IsDisabled });
            PubSub.Publish(new TabDisableFixedAddressChangedEvent(e.TabId, e.IsDisabled));
        });
        PubSub.Handle<ExpireEphemeralTabsCommand>((e) =>
        {
            foreach (var tabId in e.TabIds)
                state.DeleteCustomization(tabId);

            PubSub.Publish(new EphemeralTabsExpiredEvent(e.TabIds));
        });

        PubSub.Subscribe<TabClosedEvent>((e) => state.DeleteCustomization(e.TabId));
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());

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
