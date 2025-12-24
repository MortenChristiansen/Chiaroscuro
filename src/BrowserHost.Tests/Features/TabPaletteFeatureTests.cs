using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette;
using BrowserHost.Utilities;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.EventArgHelpers;

namespace BrowserHost.Tests.Features;

public class TabPaletteFeatureTests
{
    [Fact]
    public void Pressing_F1_when_the_tab_palette_is_closed_opens_it()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithNoCurrentTab()
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1));

        Assert.True(handled);
        Assert.True(context.InitTabPaletteCalled);
        Assert.True(context.ShowTabPaletteCalled);
        Assert.False(context.HideTabPaletteCalled);
    }

    [Fact]
    public void Pressing_F1_when_the_tab_palette_is_open_closes_it()
    {
        var feature = TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        Assert.True(feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1)));
        Assert.True(feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1)));

        Assert.True(context.InitTabPaletteCalled);
        Assert.True(context.ShowTabPaletteCalled);
        Assert.True(context.HideTabPaletteCalled);
    }

    [Fact]
    public void Publishing_a_TabPaletteDismissedEvent_does_not_hide_tab_palette_when_it_is_already_closed()
    {
        TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        PubSub.Instance.Publish(new TabPaletteDismissedEvent());

        Assert.False(context.HideTabPaletteCalled);
    }

    [Fact]
    public void Publishing_a_TabDeactivatedEvent_closes_tab_palette_if_it_is_open()
    {
        var feature = TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();
        feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1));

        PubSub.Instance.Publish(new TabDeactivatedEvent("tab-1"));

        Assert.True(context.HideTabPaletteCalled);
    }
}
