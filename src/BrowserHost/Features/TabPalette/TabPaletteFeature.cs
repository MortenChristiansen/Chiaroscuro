using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette;

public record RequestTabPaletteCommand() : Utilities.ICommand;
public record DismissTabPaletteCommand() : Utilities.ICommand;

public record TabPaletteRequestedEvent() : IEvent;
public record TabPaletteDismissedEvent() : IEvent;

public class TabPaletteFeature(MainWindow window, PubSub pubSub, IBrowserContext browserContext, TabPaletteBrowserApi tabPaletteApi) : Feature(window, pubSub)
{
    private bool _tabPaletteIsOpen;

    public override void Configure()
    {
        PubSub.Handle<RequestTabPaletteCommand>(_ =>
        {
            if (_tabPaletteIsOpen)
                return;

            OpenTabPalette();
            PubSub.Publish(new TabPaletteRequestedEvent());
        });
        PubSub.Handle<DismissTabPaletteCommand>(_ =>
        {
            if (!_tabPaletteIsOpen)
                return;

            CloseTabPalette();
            PubSub.Publish(new TabPaletteDismissedEvent());
        });

        PubSub.Subscribe<TabDeactivatedEvent>(_ => PubSub.Send(new DismissTabPaletteCommand()));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            if (_tabPaletteIsOpen)
                PubSub.Send(new DismissTabPaletteCommand());
            else
                PubSub.Send(new RequestTabPaletteCommand());

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
