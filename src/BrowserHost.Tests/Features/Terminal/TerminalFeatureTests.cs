using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Terminal;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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

    [Fact]
    public void Pressing_the_terminal_toggle_key_with_modifiers_does_not_toggle_terminal()
    {
        var feature = CreateFeature
            .WithCurrentTab()
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Oem5));

        Assert.False(handled);
        Assert.Equal(0, context.TerminalWindowOperations.ShowTerminalCallCount);
    }

    [Fact]
    public void Pressing_the_terminal_toggle_key_in_a_text_box_does_not_toggle_terminal()
    {
        var feature = CreateFeature
            .WithCurrentTab()
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        var args = CreateKeyEventArgs(Key.Oem5);
        SetOriginalSource(args, CreateUninitialized<TextBox>());

        var handled = feature.HandleOnPreviewKeyDown(args);

        Assert.False(handled);
        Assert.Equal(0, context.TerminalWindowOperations.ShowTerminalCallCount);
    }

    [Fact]
    public void Pressing_the_terminal_toggle_key_in_an_embedded_browser_does_not_toggle_terminal()
    {
        var feature = CreateFeature
            .WithCurrentTab()
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        var args = CreateKeyEventArgs(Key.Oem5);
        SetOriginalSource(args, CreateUninitialized<TestHwndHost>());

        var handled = feature.HandleOnPreviewKeyDown(args);

        Assert.False(handled);
        Assert.Equal(0, context.TerminalWindowOperations.ShowTerminalCallCount);
    }

    [Fact]
    public void Toggling_the_terminal_with_no_current_tab_does_not_initialize_the_terminal()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTerminalFeature();

        context.PubSub.Send(new ToggleTerminalCommand());

        Assert.Equal(1, context.TerminalWindowOperations.ShowTerminalCallCount);
        Assert.True(context.TerminalBrowserApi.WasCalledWith("setTerminalVisibility", "true"));
        Assert.False(context.TerminalBrowserApi.Invocations.Any(invocation => invocation.Method == "initTerminal"));
    }

    private static void SetOriginalSource(KeyEventArgs args, object source)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var sourceField = typeof(System.Windows.RoutedEventArgs).GetField("_source", flags)
            ?? throw new InvalidOperationException("RoutedEventArgs._source field not found - internal structure may have changed");
        var originalSourceField = typeof(System.Windows.RoutedEventArgs).GetField("_originalSource", flags)
            ?? throw new InvalidOperationException("RoutedEventArgs._originalSource field not found - internal structure may have changed");

        sourceField.SetValue(args, source);
        originalSourceField.SetValue(args, source);
    }

    private sealed class TestHwndHost : HwndHost
    {
        protected override HandleRef BuildWindowCore(HandleRef hwndParent) => new(this, IntPtr.Zero);

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
