using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette;

public record TabPaletteRequestedEvent();
public record TabPaletteDismissedEvent();

public class TabPaletteFeature(MainWindow window) : Feature(window)
{
    private bool _tabPaletteIsOpen;

    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => OpenTabPalette());
        PubSub.Subscribe<TabPaletteDismissedEvent>((_) => CloseTabPalette());
        PubSub.Subscribe<TabDeactivatedEvent>((_) => CloseTabPalette());
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            if (_tabPaletteIsOpen)
                PubSub.Publish(new TabPaletteDismissedEvent());
            else
                PubSub.Publish(new TabPaletteRequestedEvent());

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    public void OpenTabPalette()
    {
        _tabPaletteIsOpen = true;
        Window.TabPaletteBrowserControl.Init();
        Window.ShowTabPalette();
    }

    private void CloseTabPalette()
    {
        if (!_tabPaletteIsOpen)
            return;

        _tabPaletteIsOpen = false;
        Window.HideTabPalette();
    }
}
