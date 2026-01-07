using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using System;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.ActionContext.PinnedTabs;

public class PinnedTabsFeatureTest
{
    [Fact]
    public void Sending_a_PinTabCommand_adds_the_tab_to_pinned_tabs_closes_it_and_publishes_a_tab_pinned_event()
    {
        var pinnedTabsFeature = CreateFeature
            .CaptureContext(out var context)
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .BuildPinnedTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        context.PinnedTabsBrowserApi.ClearInvocations();
        context.TabsBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));
        var createdTab = Assert.Single(context.CreatedTabs, t => t.Id == "tab-1");
        createdTab.Title = "Example";
        createdTab.Favicon = "icon";

        context.PubSub.Send(new PinTabCommand("tab-1"));

        Assert.True(pinnedTabsFeature.IsTabPinned("tab-1"));
        Assert.Equal("tab-1", pinnedTabsFeature.GetActivePinnedTabId());
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "closeTab" && i.Arguments != null && i.Arguments.Contains("tab-1", StringComparison.Ordinal) && i.Arguments.Contains("false", StringComparison.Ordinal));
        Assert.Contains(context.PinnedTabsBrowserApi.Invocations, i => i.Method == "setPinnedTabs");
        Assert.Single(PubSubMessages.OfType<TabPinnedEvent>(), e => e.TabId == "tab-1");
    }

    [Fact]
    public void Pressing_Ctrl_and_P_with_an_active_pinned_tab_unpins_it_and_is_handled()
    {
        var pinnedTabsFeature = CreateFeature
            .CaptureContext(out var context)
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildPinnedTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        context.TabsBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));
        context.PubSub.Send(new PinTabCommand("tab-1"));
        context.TabsBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        var handled = pinnedTabsFeature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.P));

        Assert.True(handled);
        Assert.False(pinnedTabsFeature.IsTabPinned("tab-1"));
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "addTab" && i.Arguments != null && i.Arguments.Contains("tab-1", StringComparison.Ordinal));
        Assert.Single(PubSubMessages.OfType<TabUnpinnedEvent>(), e => e.TabId == "tab-1");
    }
}
