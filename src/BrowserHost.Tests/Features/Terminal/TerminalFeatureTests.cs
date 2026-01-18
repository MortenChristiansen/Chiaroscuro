using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Terminal;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.Terminal;

public class TerminalFeatureTests
{
    [Fact]
    public void Sending_ToggleTerminalCommand_shows_terminal_when_hidden()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new ToggleTerminalCommand());

        Assert.Equal(1, context.TerminalWindowOperations.ShowTerminalCallCount);
        Assert.True(context.TerminalBrowserApi.WasCalledWith("setTerminalVisibility", "true"));
        Assert.True(context.TerminalBrowserApi.WasCalledWith("initTerminal", $"'{tab.Id}'"));
    }

    [Fact]
    public void Sending_ToggleTerminalCommand_hides_terminal_when_visible()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();
        context.TerminalWindowOperations.ShowTerminal();

        context.PubSub.Send(new ToggleTerminalCommand());

        Assert.Equal(1, context.TerminalWindowOperations.HideTerminalCallCount);
        Assert.True(context.TerminalBrowserApi.WasCalledWith("setTerminalVisibility", "false"));
    }

    [Fact]
    public void Sending_ToggleTerminalCommand_publishes_TerminalToggledEvent()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new ToggleTerminalCommand());

        var evt = PubSubMessages.OfType<TerminalToggledEvent>().Single();
        Assert.True(evt.IsVisible);
    }

    [Fact]
    public void Sending_ToggleTerminalCommand_publishes_TerminalToggledEvent_when_hiding()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();
        context.TerminalWindowOperations.ShowTerminal();

        context.PubSub.Send(new ToggleTerminalCommand());

        var evt = PubSubMessages.OfType<TerminalToggledEvent>().Single();
        Assert.False(evt.IsVisible);
    }

    [Fact]
    public void Sending_ClearTerminalCommand_clears_the_specified_tab_terminal()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new ClearTerminalCommand("tab-123"));

        Assert.True(context.TerminalBrowserApi.WasCalledWith("clearTerminal", "'tab-123'"));
    }

    [Fact]
    public void Sending_WriteTerminalOutputCommand_writes_output_to_terminal()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new WriteTerminalOutputCommand("tab-1", "Hello world", IsError: false));

        Assert.True(context.TerminalBrowserApi.WasCalledWith("writeTerminalOutput", "'tab-1', 'Hello world', false"));
    }

    [Fact]
    public void Sending_WriteTerminalOutputCommand_with_error_writes_error_output_to_terminal()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new WriteTerminalOutputCommand("tab-1", "Error occurred", IsError: true));

        Assert.True(context.TerminalBrowserApi.WasCalledWith("writeTerminalOutput", "'tab-1', 'Error occurred', true"));
    }

    [Fact]
    public void Publishing_TabActivatedEvent_initializes_terminal_for_the_new_tab()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab)
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Publish(new TabActivatedEvent("new-tab-id", previousTab));

        Assert.True(context.TerminalBrowserApi.WasCalledWith("initTerminal", "'new-tab-id'"));
    }

    [Fact]
    public void Pressing_the_terminal_toggle_key_shows_terminal()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Oem5));

        Assert.True(handled);
        Assert.Equal(1, context.TerminalWindowOperations.ShowTerminalCallCount);
        Assert.True(context.TerminalBrowserApi.WasCalledWith("setTerminalVisibility", "true"));
        Assert.True(context.TerminalBrowserApi.WasCalledWith("initTerminal", $"'{tab.Id}'"));
    }

    [Fact]
    public void Pressing_other_keys_does_not_toggle_terminal()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.A));

        Assert.False(handled);
        Assert.Equal(0, context.TerminalWindowOperations.ShowTerminalCallCount);
    }
}
