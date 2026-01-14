using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.CustomWindowChrome;
using System.Windows;

namespace BrowserHost.Tests.Features.CustomWindowChrome;

public class CustomWindowChromeFeatureTests
{
    [Fact]
    public void Sending_a_MinimizeWindowCommand_sets_the_window_state_to_minimized_and_publishes_a_WindowMinimizedEvent()
    {
        CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.WindowState = WindowState.Normal)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Send(new MinimizeWindowCommand());

        Assert.Equal(WindowState.Minimized, context.WindowState);
        Assert.Single(PubSubMessages.OfType<WindowMinimizedEvent>());
    }

    [Fact]
    public void Sending_a_ToggleWindowStateCommand_when_the_window_is_normal_maximizes_it_and_publishes_a_WindowStateToggledEvent()
    {
        CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.WindowState = WindowState.Normal)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Send(new ToggleWindowStateCommand());

        Assert.Equal(WindowState.Maximized, context.WindowState);
        Assert.Single(PubSubMessages.OfType<WindowStateToggledEvent>());
    }

    [Fact]
    public void Sending_a_ToggleWindowStateCommand_when_the_window_is_maximized_restores_it_and_publishes_a_WindowStateToggledEvent()
    {
        CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.WindowState = WindowState.Maximized)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Send(new ToggleWindowStateCommand());

        Assert.Equal(WindowState.Normal, context.WindowState);
        Assert.Single(PubSubMessages.OfType<WindowStateToggledEvent>());
    }

    [Fact]
    public void Sending_a_CopyAddressCommand_copies_the_current_tab_address_without_Google_ad_tracking_parameters()
    {
        CreateFeature
            .WithCurrentTab(out _, "tab-1", "https://example.com/path?a=1&gclid=abc123&b=2")
            .CaptureContext(out var context)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Send(new CopyAddressCommand());

        Assert.Equal("https://example.com/path?a=1&b=2", context.ClipboardText);
        Assert.Single(PubSubMessages.OfType<AddressCopiedEvent>());
    }

    [Fact]
    public void Sending_a_CopyAddressCommand_with_no_current_tab_does_not_copy_anything_and_does_not_publish_an_AddressCopiedEvent()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Send(new CopyAddressCommand());

        Assert.Null(context.ClipboardText);
        Assert.Empty(PubSubMessages.OfType<AddressCopiedEvent>());
    }

    [Fact]
    public void Publishing_a_TabLoadingStateChangedEvent_for_the_current_tab_updates_the_frontend_loading_state()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1", "https://example.com/")
            .CaptureContext(out var context)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Publish(new TabLoadingStateChangedEvent(tab.Id, IsLoading: true));

        var invocation = Assert.Single(context.CustomWindowChromeBrowserApi.Invocations, i => i.Method == "updateLoadingState");
        Assert.Equal("true", invocation.Arguments);
    }

    [Fact]
    public void Publishing_a_TabLoadingStateChangedEvent_for_a_different_tab_does_not_update_the_frontend_loading_state()
    {
        CreateFeature
            .WithCurrentTab(out _, "tab-1", "https://example.com/")
            .CaptureContext(out var context)
            .BuildCustomWindowChromeFeature();

        context.PubSub.Publish(new TabLoadingStateChangedEvent("tab-2", IsLoading: true));

        Assert.Empty(context.CustomWindowChromeBrowserApi.Invocations);
    }

    [Fact]
    public void Publishing_a_TabActivatedEvent_updates_the_frontend_loading_state_from_the_previous_tab_loading_state()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildCustomWindowChromeFeature();
        var previousTab = new FakeTabBrowser("previous") { IsLoading = true };

        context.PubSub.Publish(new TabActivatedEvent(TabId: "tab-1", PreviousTab: previousTab));

        var invocation = Assert.Single(context.CustomWindowChromeBrowserApi.Invocations, i => i.Method == "updateLoadingState");
        Assert.Equal("true", invocation.Arguments);
    }
}
