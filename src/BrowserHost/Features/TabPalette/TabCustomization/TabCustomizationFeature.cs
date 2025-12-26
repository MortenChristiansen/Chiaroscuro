using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Utilities;
using System.Linq;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(MainWindow window, IBrowserContext browserContext, TabCustomizationBrowserApi tabCustomizationApi, TabCustomizationStateManager state) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Instance.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Instance.Subscribe<TabCustomTitleChangedEvent>((e) =>
        {
            var customization = state.SaveCustomization(e.TabId, c => c with { CustomTitle = e.CustomTitle });
            tabCustomizationApi.UpdateTabCustomization(new(e.TabId, customization?.CustomTitle));
        });
        PubSub.Instance.Subscribe<TabDisableFixedAddressChangedEvent>((e) =>
        {
            state.SaveCustomization(e.TabId, c => c with { DisableFixedAddress = e.IsDisabled });
        });
        PubSub.Instance.Subscribe<TabClosedEvent>((e) => state.DeleteCustomization(e.Tab.Id));
        PubSub.Instance.Subscribe<EphemeralTabsExpiredEvent>((e) =>
        {
            foreach (var tabId in e.TabIds)
                state.DeleteCustomization(tabId);
        });

        InitializeCustomizations();
    }

    private void InitializeCustomizations()
    {
        var allCustomizations = state.GetAllCustomizations();
        tabCustomizationApi.SetTabCustomizations([.. allCustomizations.Select(c => new TabCustomizationDto(c.TabId, c.CustomTitle))]);
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
