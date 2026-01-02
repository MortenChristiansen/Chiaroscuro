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
        PubSub.Handle<ChangeTabCustomTitleCommand>(cmd =>
        {
            var customization = state.SaveCustomization(cmd.TabId, c => c with { CustomTitle = cmd.CustomTitle });
            tabsApi.UpdateTabCustomization(new(cmd.TabId, customization?.CustomTitle));
            PubSub.Publish(new TabCustomTitleChangedEvent(cmd.TabId, cmd.CustomTitle));
        });
        PubSub.Handle<ChangeTabDisableFixedAddressCommand>(cmd =>
        {
            state.SaveCustomization(cmd.TabId, c => c with { DisableFixedAddress = cmd.IsDisabled });
            PubSub.Publish(new TabDisableFixedAddressChangedEvent(cmd.TabId, cmd.IsDisabled));
        });
        PubSub.Handle<ExpireEphemeralTabsCommand>(cmd =>
        {
            foreach (var tabId in cmd.TabIds)
                state.DeleteCustomization(tabId);

            PubSub.Publish(new EphemeralTabsExpiredEvent(cmd.TabIds));
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
