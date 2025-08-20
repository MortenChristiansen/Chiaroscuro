using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Subscribe<CustomTitleChangedEvent>((e) =>
            TabCustomizationStateManager.SaveCustomization(
                Window.CurrentTab!.Id,
                new() { CustomTitle = e.NewTitle }
            )
        );
        PubSub.Subscribe<TabClosedEvent>((e) => TabCustomizationStateManager.DeleteCustomization(e.Tab.Id));
    }

    public void InitializeCustomSettings()
    {
        var customization = TabCustomizationStateManager.GetCustomization(Window.CurrentTab!.Id);
        Window.TabPaletteBrowserControl.InitCustomTitle(customization.CustomTitle);
    }
}
