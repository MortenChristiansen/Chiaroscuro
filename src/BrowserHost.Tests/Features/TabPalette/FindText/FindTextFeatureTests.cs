using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.FindText;
using System.Windows.Input;
using static BrowserHost.Tests.Fakes.TestBrowserApiExtensions;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.TabPalette.FindText;

public class FindTextFeatureTests
{
    [Fact]
    public void Sending_a_FindTextCommand_starts_finding_text_in_the_current_tab()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildFindTextFeature();

        context.PubSub.Send(new FindTextCommand("hello"));

        var invocation = Assert.Single(tab.FindInvocations);
        Assert.Equal("hello", invocation.SearchText);
        Assert.True(invocation.Forward);
        Assert.False(invocation.MatchCase);
        Assert.True(invocation.FindNext);
    }

    [Fact]
    public void Pressing_Tab_while_finding_text_moves_to_the_next_match()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.None)
            .BuildFindTextFeature();
        context.PubSub.Send(new FindTextCommand("hello"));

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Tab));

        Assert.True(handled);
        Assert.Equal(2, tab.FindInvocations.Count);
        Assert.True(tab.FindInvocations.Last().Forward);
    }

    [Fact]
    public void Pressing_Shift_Tab_while_finding_text_moves_to_the_previous_match()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Shift)
            .BuildFindTextFeature();
        context.PubSub.Send(new FindTextCommand("hello"));

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Tab));

        Assert.True(handled);
        Assert.Equal(2, tab.FindInvocations.Count);
        Assert.False(tab.FindInvocations.Last().Forward);
    }

    [Fact]
    public void Pressing_Escape_while_finding_text_stops_finding_and_clears_the_status()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildFindTextFeature();
        context.PubSub.Send(new FindTextCommand("hello"));

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Escape));

        Assert.True(handled);
        Assert.True(tab.StopFindingCalled);
        Assert.True(tab.StopFindingClearSelection);
        Assert.True(context.FindTextBrowserApi.WasCalledWith("findStatusChanged", ""));
    }

    [Fact]
    public void Pressing_Ctrl_F_when_not_finding_text_requests_the_tab_palette_and_focuses_the_find_input()
    {
        var builder = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control);
        builder.BuildTabPaletteFeature();
        var feature = builder.BuildFindTextFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F));

        Assert.True(handled);
        Assert.Single(PubSubMessages.OfType<TabPaletteRequestedEvent>());
        Assert.True(context.FocusTabPaletteCalled);
        Assert.True(context.FindTextBrowserApi.WasCalledWith("focusFindTextInput"));
    }

    [Fact]
    public void Sending_a_ChangeFindStatusCommand_sends_the_match_count_to_the_frontend()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildFindTextFeature();

        context.PubSub.Send(new ChangeFindStatusCommand(5));

        Assert.True(context.FindTextBrowserApi.WasCalledWith("findStatusChanged", "5"));
    }

    [Fact]
    public void Publishing_a_TabDeactivatedEvent_stops_finding_text()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildFindTextFeature();
        context.PubSub.Send(new FindTextCommand("hello"));

        context.PubSub.Publish(new TabDeactivatedEvent("tab-1"));

        Assert.True(tab.StopFindingCalled);
    }
}
