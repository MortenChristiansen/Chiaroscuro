using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Utilities;

namespace BrowserHost.Tests.Features.TabPalette.DomainCustomization;

public class DomainCustomizationFeatureTest
{
    private readonly string _domain = $"{Guid.NewGuid():N}.example";

    [Fact]
    public void Publishing_a_TabPaletteRequestedEvent_initializes_domain_settings_when_the_current_tab_has_a_domain()
    {
        CreateFeature
            .WithCurrentTab(out _, "tab-1", $"https://{_domain}/")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        var invocation = Assert.Single(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "initDomainSettings");
        Assert.Equal($"'{_domain}', false, false", invocation.Arguments);
    }

    [Fact]
    public void Sending_a_ChangeDomainCustomizationCommand_persists_css_enabled_and_notifies_the_frontend()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();

        context.PubSub.Send(new ChangeDomainCustomizationCommand(_domain, CssEnabled: true));

        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.True(customization.CssEnabled);
        var invocation = Assert.Single(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Equal($"'{_domain}', true, false", invocation.Arguments);
    }

    [Fact]
    public void Sending_a_RemoveDomainCustomCssCommand_deletes_CSS_removes_it_from_the_tab_and_disables_it()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1", $"https://{_domain}/")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        Assert.True(context.DomainCustomizationStateManager.EnsureCustomCssExistsAndOpenInEditor(_domain));
        context.PubSub.Send(new ChangeDomainCustomizationCommand(_domain, CssEnabled: true));
        context.PubSub.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        context.PubSub.Send(new RemoveDomainCustomCssCommand(_domain));

        var disabledEvent = Assert.Single(PubSubMessages.OfType<DomainCustomizationChangedEvent>(), e => !e.CssEnabled);
        Assert.Equal(_domain, disabledEvent.Domain);
        Assert.False(disabledEvent.CssEnabled);
        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.False(customization.CssEnabled);
        Assert.False(customization.HasCustomCss);
        Assert.Contains(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Contains(tab.ExecutedScripts, s => s.Contains("chiaroscuro-domain-css") && s.Contains("remove"));
    }

    [Fact]
    public void Sending_a_EditDomainCssCommand_creates_CSS_and_enables_it()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1", $"https://{_domain}/")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        context.PubSub.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));

        context.PubSub.Send(new EditDomainCssCommand(_domain));

        var customization = context.DomainCustomizationStateManager.GetCustomization(_domain);
        Assert.True(customization.CssEnabled);
        Assert.True(customization.HasCustomCss);
        var cssFile = GetCustomCssFilePath(context, _domain);
        Assert.True(context.FileSystem.File.Exists(cssFile));
    }

    [Fact]
    public void When_the_CSS_watcher_signals_a_change_the_CSS_is_reloaded_and_the_frontend_is_notified_via_dispatch()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1", $"https://{_domain}/")
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.ActionRequiresDispatch = true)
            .BuildDomainCustomizationFeature();
        Assert.True(context.DomainCustomizationStateManager.EnsureCustomCssExistsAndOpenInEditor(_domain));
        context.PubSub.Send(new ChangeDomainCustomizationCommand(_domain, CssEnabled: true));
        context.PubSub.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));
        var cssFile = GetCustomCssFilePath(context, _domain);
        context.ResetDispatchCalled(); // TODO: Why is this needed?

        context.FileSystem.File.WriteAllText(cssFile, "body { background: green; } /* token:green */");

        context.WaitForDispatch();
        Assert.Contains(context.DomainCustomizationBrowserApi.Invocations, i => i.Method == "updateDomainSettings");
        Assert.Contains(tab.ExecutedScripts, s => s.Contains("token:green"));
    }

    [Fact]
    public void When_the_CSS_watcher_signals_that_CSS_was_removed_a_DomainCustomCssRemovedEvent_is_published_with_dispatching()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1", $"https://{_domain}/")
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.ActionRequiresDispatch = true)
            .BuildDomainCustomizationFeature();
        Assert.True(context.DomainCustomizationStateManager.EnsureCustomCssExistsAndOpenInEditor(_domain));
        context.PubSub.Send(new ChangeDomainCustomizationCommand(_domain, CssEnabled: true));
        context.PubSub.Publish(new TabActivatedEvent(tab.Id, PreviousTab: null));
        var cssFile = GetCustomCssFilePath(context, _domain);

        context.FileSystem.File.Delete(cssFile);

        context.WaitUntil(() => PubSubMessages.OfType<DomainCustomCssRemovedEvent>().Any());
        var removedEvent = Assert.Single(PubSubMessages.OfType<DomainCustomCssRemovedEvent>());
        Assert.Equal(_domain, removedEvent.Domain);
        Assert.True(context.DispatchCalled);
        Assert.Contains(tab.ExecutedScripts, s => s.Contains("chiaroscuro-domain-css") && s.Contains("remove"));
    }

    [Fact]
    public void When_the_current_domain_changes_we_no_longer_watch_for_changes_to_the_previous_domain_CSS_file()
    {
        var domain1 = $"{Guid.NewGuid():N}.example";
        var domain2 = $"{Guid.NewGuid():N}.example";
        CreateFeature
            .WithCurrentTab(out var tab1, "tab-1", $"https://{domain1}/")
            .CaptureContext(out var context)
            .BuildDomainCustomizationFeature();
        Assert.True(context.DomainCustomizationStateManager.EnsureCustomCssExistsAndOpenInEditor(domain1));
        context.PubSub.Publish(new DomainCustomizationChangedEvent(domain1, CssEnabled: true));
        context.PubSub.Publish(new TabActivatedEvent(tab1.Id, PreviousTab: null));
        var cssFile1 = GetCustomCssFilePath(context, domain1);

        var tab2 = new FakeTabBrowser("tab-2") { Address = $"https://{domain2}/" };
        context.SetCurrentTab(tab2);

        var scriptsBefore = tab2.ExecutedScripts.Count;
        var invocationsBefore = context.DomainCustomizationBrowserApi.Invocations.Count(i => i.Method == "updateDomainSettings");
        context.FileSystem.File.WriteAllText(cssFile1, "body { background: blue; } /* token:domain1 */");
        Assert.Equal(scriptsBefore, tab2.ExecutedScripts.Count);
        Assert.Equal(invocationsBefore, context.DomainCustomizationBrowserApi.Invocations.Count(i => i.Method == "updateDomainSettings"));
    }

    private static string GetCustomCssFilePath(TestBrowserContext context, string domain)
    {
        var appDataRoot = AppDataPathManager.GetAppDataFolderPath();
        var files = context.FileSystem.Directory.EnumerateFiles(appDataRoot, "custom.css", SearchOption.AllDirectories).ToList();
        return files.Single(f => f.Contains(domain.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }
}
