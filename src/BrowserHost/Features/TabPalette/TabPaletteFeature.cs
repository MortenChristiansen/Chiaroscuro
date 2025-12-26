using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette;

public record TabPaletteRequestedEvent();
public record TabPaletteDismissedEvent();

public class TabPaletteFeature(MainWindow window, IBrowserContext browserContext, TabPaletteBrowserApi tabPaletteApi) : Feature(window)
{
    private bool _tabPaletteIsOpen;

    public override void Configure()
    {
        PubSub.Instance.Subscribe<TabPaletteRequestedEvent>((_) => OpenTabPalette());
        PubSub.Instance.Subscribe<TabPaletteDismissedEvent>((_) => CloseTabPalette());
        PubSub.Instance.Subscribe<TabDeactivatedEvent>((_) => CloseTabPalette());
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            if (_tabPaletteIsOpen)
                PubSub.Instance.Publish(new TabPaletteDismissedEvent());
            else
                PubSub.Instance.Publish(new TabPaletteRequestedEvent());

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    public void OpenTabPalette()
    {
        _tabPaletteIsOpen = true;
        tabPaletteApi.Init();
        browserContext.ShowTabPalette();
    }

    private void CloseTabPalette()
    {
        if (!_tabPaletteIsOpen)
            return;

        _tabPaletteIsOpen = false;
        browserContext.HideTabPalette();
    }
}
