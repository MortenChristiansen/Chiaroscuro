using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Utilities;
using System.Linq;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(MainWindow window, IBrowserContext browserContext, TabCustomizationBrowserApi tabCustomizationApi) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Instance.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Instance.Subscribe<TabCustomTitleChangedEvent>((e) =>
        {
            var customization = TabCustomizationStateManager.SaveCustomization(e.TabId, c => c with { CustomTitle = e.CustomTitle });
            tabCustomizationApi.UpdateTabCustomization(new(e.TabId, customization?.CustomTitle));
        });
        PubSub.Instance.Subscribe<TabDisableFixedAddressChangedEvent>((e) =>
        {
            TabCustomizationStateManager.SaveCustomization(e.TabId, c => c with { DisableFixedAddress = e.IsDisabled });
        });
        PubSub.Instance.Subscribe<TabClosedEvent>((e) => TabCustomizationStateManager.DeleteCustomization(e.Tab.Id));
        PubSub.Instance.Subscribe<EphemeralTabsExpiredEvent>((e) =>
        {
            foreach (var tabId in e.TabIds)
                TabCustomizationStateManager.DeleteCustomization(tabId);
        });

        InitializeCustomizations();
    }

    private void InitializeCustomizations()
    {
        var allCustomizations = TabCustomizationStateManager.GetAllCustomizations();
        tabCustomizationApi.SetTabCustomizations([.. allCustomizations.Select(c => new TabCustomizationDto(c.TabId, c.CustomTitle))]);
    }

    public void InitializeCustomSettings()
    {
        if (browserContext.CurrentTabId is not { } tabId)
            return;

        var customization = TabCustomizationStateManager.GetCustomization(tabId);
        tabCustomizationApi.InitCustomSettings(customization);
    }

    public static TabCustomizationDataV1 GetCustomizationsForTab(string tabId) =>
        TabCustomizationStateManager.GetCustomization(tabId);
}
