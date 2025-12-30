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
    public void Publishing_a_DomainCustomCssRemovedEvent_deletes_css_removes_it_from_the_tab_and_disables_it()
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
}
