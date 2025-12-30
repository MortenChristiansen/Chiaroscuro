using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Utilities;

namespace BrowserHost.Tests.Features.TabPalette;

public class DomainCustomizationFeatureTest
{
    private static readonly string _domain = $"{Guid.NewGuid():N}.example";

    [Fact]
    public void Publishing_a_TabPaletteRequestedEvent_initializes_domain_settings_when_the_current_tab_has_a_domain()
    {
        CreateFeature
            .WithCurrentDomainTab(out _, $"https://{_domain}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();

        PubSub.Instance.Publish(new TabPaletteRequestedEvent());

        var invocation = Assert.Single(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "initDomainSettings");
        Assert.Equal($"'{_domain}', false, false", invocation.Arguments);
    }

    [Fact]
    public void Publishing_a_DomainCustomizationChangedEvent_persists_css_enabled_and_notifies_the_frontend()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();

        PubSub.Instance.Publish(new DomainCustomizationChangedEvent(_domain, CssEnabled: true));

        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.True(customization.CssEnabled);
        var invocation = Assert.Single(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Equal($"'{_domain}', true, false", invocation.Arguments);
    }

    [Fact]
    public void Publishing_a_DomainCustomCssRemovedEvent_deletes_CSS_removes_it_from_the_tab_and_disables_it()
    {
        CreateFeature
            .WithCurrentDomainTab(out var tab, $"https://{_domain}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        context.DomainCustomizationStateManager.SetCustomCss(_domain, "body { background: red; }");
        context.DomainCustomizationStateManager.SaveCustomization(new DomainCustomizationDataV1(_domain, CssEnabled: true, HasCustomCss: true));
        PubSub.Instance.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        PubSub.Instance.Publish(new DomainCustomCssRemovedEvent(_domain));

        var disabledEvent = Assert.Single(PubSubMessages.OfType<DomainCustomizationChangedEvent>());
        Assert.Equal(_domain, disabledEvent.Domain);
        Assert.False(disabledEvent.CssEnabled);
        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.False(customization.CssEnabled);
        Assert.False(customization.HasCustomCss);
        Assert.Contains(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Contains(tab.GetTabWebBrowser().ExecutedScripts, s => s.Contains("chiaroscuro-domain-css") && s.Contains("remove"));
    }

    [Fact]
    public void Publishing_a_DomainCssEditRequestedEvent_creates_CSS_enables_it_and_starts_watching_for_changes()
    {
        CreateFeature
            .WithCurrentDomainTab(out var tab, $"https://{_domain}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        PubSub.Instance.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        PubSub.Instance.Publish(new DomainCssEditRequestedEvent(_domain));

        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.True(customization.CssEnabled);
        Assert.True(customization.HasCustomCss);
        Assert.Contains(_domain, context.DomainCustomizationStateManager.WatchCustomCssInvocations);
    }

    [Fact]
    public void When_the_CSS_watcher_signals_a_change_the_CSS_is_reloaded_and_the_frontend_is_notified_via_dispatch()
    {
        CreateFeature
            .WithCurrentDomainTab(out var tab, $"https://{_domain}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.ActionRequiresDispatch = true)
            .BuildDomainCustomizationFeature();
        context.DomainCustomizationStateManager.SetCustomCss(_domain, "body { background: red; }");
        context.DomainCustomizationStateManager.SaveCustomization(new DomainCustomizationDataV1(_domain, CssEnabled: true, HasCustomCss: true));
        PubSub.Instance.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        context.DomainCustomizationStateManager.TriggerWatchEvent(_domain, DomainCustomizationStateManager.CustomCssWatchEventKind.Changed);

        Assert.True(context.DispatchCalled);
        Assert.Contains(_domain, context.DomainCustomizationStateManager.RefreshCacheInvocations);
        Assert.Contains(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Contains(tab.GetTabWebBrowser().ExecutedScripts, s => s.Contains("chiaroscuro-domain-css") && s.Contains("expectedCss"));
    }

    [Fact]
    public void When_the_CSS_watcher_signals_that_CSS_was_removed_a_DomainCustomCssRemovedEvent_is_published_without_dispatching()
    {
        CreateFeature
            .WithCurrentDomainTab(out var tab, $"https://{_domain}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.ActionRequiresDispatch = true)
            .BuildDomainCustomizationFeature();
        PubSub.Instance.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        context.DomainCustomizationStateManager.TriggerWatchEvent(_domain, DomainCustomizationStateManager.CustomCssWatchEventKind.Removed);

        var removedEvent = Assert.Single(PubSubMessages.OfType<DomainCustomCssRemovedEvent>());
        Assert.Equal(_domain, removedEvent.Domain);
        Assert.False(context.DispatchCalled);
        Assert.DoesNotContain(_domain, context.DomainCustomizationStateManager.RefreshCacheInvocations);
        Assert.Contains(tab.GetTabWebBrowser().ExecutedScripts, s => s.Contains("chiaroscuro-domain-css") && s.Contains("remove"));
    }

    [Fact]
    public void When_the_current_domain_changes_the_previous_CSS_watcher_is_disposed_and_a_new_one_is_created()
    {
        var domain1 = $"{Guid.NewGuid():N}.example";
        var domain2 = $"{Guid.NewGuid():N}.example";
        CreateFeature
            .WithCurrentDomainTab(out var tab1, $"https://{domain1}/", tabId: "tab-1")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        PubSub.Instance.Publish(new TabActivatedEvent(tab1.Id, PreviousTab: null));
        var firstSubscription = Assert.Single(context.DomainCustomizationStateManager.WatchSubscriptions);
        Assert.False(firstSubscription.IsDisposed);

        var tab2 = TypeConstructor.CreateTabBrowser("tab-2");
        tab2.SetTabAddress($"https://{domain2}/");
        context.SetCurrentTab(tab2);
        PubSub.Instance.Publish(new TabActivatedEvent(tab2.Id, PreviousTab: tab1));

        Assert.True(firstSubscription.IsDisposed);
        Assert.Contains(domain2, context.DomainCustomizationStateManager.WatchCustomCssInvocations);
        Assert.Equal(2, context.DomainCustomizationStateManager.WatchSubscriptions.Count);
    }
}
