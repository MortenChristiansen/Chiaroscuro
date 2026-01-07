using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;

namespace BrowserHost.Tests.Features.ActionContext.Tabs;

public class TabsFeatureTest
{
    [Fact]
    public void Starting_navigation_in_a_new_inactive_tab_preloads_the_tab_in_the_tabs_host()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");

        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));

        Assert.Contains("tab-1", context.FakeTabsHost.PreloadedTabIds);
        Assert.Null(context.CurrentTab);
        var creation = Assert.Single(context.TabCreations);
        Assert.Equal("https://example.com", creation.Address);
        Assert.True(creation.SetManualAddress);
    }

    [Fact]
    public void Activating_a_preloaded_tab_sets_it_as_current_tab_and_removes_it_from_the_preload_host()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));

        context.PubSub.Send(new ActivateTabCommand("tab-1"));

        var createdTab = Assert.Single(context.CreatedTabs, t => t.Id == "tab-1");
        Assert.Equal(createdTab, context.CurrentTab);
        Assert.DoesNotContain("tab-1", context.FakeTabsHost.PreloadedTabIds);
        Assert.Contains("tab-1", context.FakeTabsHost.RemovedTabIds);
    }

    [Fact]
    public void Closing_a_preloaded_tab_disposes_it_and_removes_it_from_the_preload_host()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));

        context.PubSub.Send(new CloseTabCommand("tab-1"));

        var createdTab = Assert.Single(context.CreatedTabs, t => t.Id == "tab-1");
        Assert.True(createdTab.DisposeCalled);
        Assert.DoesNotContain("tab-1", context.FakeTabsHost.PreloadedTabIds);
        Assert.Contains("tab-1", context.FakeTabsHost.RemovedTabIds);
    }

    [Fact]
    public void Activating_a_different_tab_publishes_a_TabDeactivatedEvent_for_the_previous_tab()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab, tabId: "tab-previous", address: "https://old.example")
            .CaptureContext(out var context)
            .BuildTabsFeature();

        context.NewTabIdsToGenerate.Enqueue("tab-next");
        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));

        Assert.Contains(PubSubMessages.OfType<TabDeactivatedEvent>(), e => e.TabId == "tab-previous");
    }

    [Fact]
    public void Starting_navigation_in_a_new_tab_adds_the_tab_to_the_tabs_browser_api_with_the_expected_activation_flag()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabsFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        context.TabsBrowserApi.ClearInvocations();

        context.PubSub.Send(new StartNavigationCommand("https://example.com", UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));

        Assert.Contains(context.TabsBrowserApi.Invocations, i =>
            i.Method == "addTab" &&
            i.Arguments != null &&
            i.Arguments.Contains("\"tab-1\"", StringComparison.Ordinal) &&
            i.Arguments.Contains("false", StringComparison.Ordinal)
        );
    }
}
