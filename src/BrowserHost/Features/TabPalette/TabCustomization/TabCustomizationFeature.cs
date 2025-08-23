using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System.Linq;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Subscribe<TabCustomizationChangedEvent>((e) =>
        {
            var customization = TabCustomizationStateManager.SaveCustomization(new(e.TabId, e.CustomTitle));
            Window.ActionContext.UpdateTabCustomization(new(e.TabId, customization?.CustomTitle));
        });
        PubSub.Subscribe<TabClosedEvent>((e) => TabCustomizationStateManager.DeleteCustomization(e.Tab.Id));

        InitializeCustomizations();
    }

    private void InitializeCustomizations()
    {
        var allCustomizations = TabCustomizationStateManager.GetAllCustomizations();
        Window.ActionContext.SetTabCustomizations([.. allCustomizations.Select(c => new TabCustomizationDto(c.TabId, c.CustomTitle))]);
    }

    public void InitializeCustomSettings()
    {
        var customization = TabCustomizationStateManager.GetCustomization(Window.CurrentTab!.Id);
        Window.TabPaletteBrowserControl.InitCustomTitle(customization.CustomTitle);
    }
}
