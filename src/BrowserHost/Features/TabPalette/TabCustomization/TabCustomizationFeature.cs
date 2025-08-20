using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeCustomSettings());
        PubSub.Subscribe<CustomTitleChangedEvent>((e) =>
        {

        });
    }

    public void InitializeCustomSettings()
    {
        Window.TabPaletteBrowserControl.InitCustomTitle("");
    }
}
