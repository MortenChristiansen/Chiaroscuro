using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette;
using BrowserHost.Utilities;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.TabPalette;

public class TabPaletteFeatureTests
{
    [Fact]
    public void Pressing_F1_when_the_tab_palette_is_closed_opens_it()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1));

        Assert.True(handled);
        Assert.True(context.TabPaletteBrowserApi.WasCalledWith("init"));
        Assert.Equal(1, context.TabPaletteWindowOperations.ShowCallCount);
        Assert.Equal(0, context.TabPaletteWindowOperations.HideCallCount);
    }

    [Fact]
    public void Pressing_F1_when_the_tab_palette_is_open_closes_it()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        Assert.True(feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1)));
        Assert.True(feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1)));

        Assert.True(context.TabPaletteBrowserApi.WasCalledWith("init"));
        Assert.Equal(1, context.TabPaletteWindowOperations.ShowCallCount);
        Assert.Equal(1, context.TabPaletteWindowOperations.HideCallCount);
    }

    [Fact]
    public void Publishing_a_TabPaletteDismissedEvent_does_not_hide_tab_palette_when_it_is_already_closed()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();

        context.PubSub.Publish(new TabPaletteDismissedEvent());

        Assert.Equal(0, context.TabPaletteWindowOperations.HideCallCount);
    }

    [Fact]
    public void Publishing_a_TabDeactivatedEvent_closes_tab_palette_if_it_is_open()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildTabPaletteFeature();
        feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F1));

        context.PubSub.Publish(new TabDeactivatedEvent("tab-1"));

        Assert.Equal(1, context.TabPaletteWindowOperations.HideCallCount);
    }
}
