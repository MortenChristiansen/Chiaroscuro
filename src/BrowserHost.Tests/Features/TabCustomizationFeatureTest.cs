using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;

namespace BrowserHost.Tests.Features;

public class TabCustomizationFeatureTest
{
    [Fact]
    public void Configuring_the_feature_sends_all_existing_custom_titles_to_the_action_context()
    {
        var feature = TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx =>
            {
                ctx.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "One" });
                ctx.TabCustomizationStateManager.SaveCustomization("tab-2", c => c with { CustomTitle = "Two" });
            })
            .BuildTabCustomizationFeature();

        var invocation = Assert.Single(context.TabCustomizationBrowserApi.Invocations, i => i.Method == "setTabCustomizations");
        Assert.Equal("""
            [{"tabId":"tab-1","customTitle":"One"},{"tabId":"tab-2","customTitle":"Two"}]
            """,
            invocation.Arguments
        );
    }

    [Fact]
    public void Publishing_a_TabPaletteRequestedEvent_initializes_custom_settings_when_there_is_a_current_tab()
    {
        TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.Id = "tab-1")
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "Hello" });

        PubSub.Instance.Publish(new TabPaletteRequestedEvent());

        var invocation = Assert.Single(context.TabCustomizationBrowserApi.Invocations, i => i.Method == "initCustomSettings");
        Assert.Equal("""
            {"tabId":"tab-1","customTitle":"Hello","disableFixedAddress":false}
            """,
            invocation.Arguments
        );
    }

    [Fact]
    public void Publishing_a_TabPaletteRequestedEvent_does_nothing_when_there_is_no_current_tab()
    {
        TestBrowserContext.CreateFeature
            .WithNoCurrentTab()
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        PubSub.Instance.Publish(new TabPaletteRequestedEvent());

        Assert.DoesNotContain(context.TabCustomizationBrowserApi.Invocations, i => i.Method == "initCustomSettings");
    }

    [Fact]
    public void Publishing_a_TabCustomTitleChangedEvent_updates_action_context_and_persists_the_custom_title()
    {
        TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        PubSub.Instance.Publish(new TabCustomTitleChangedEvent("tab-1", "Custom"));

        var invocation = Assert.Single(context.TabCustomizationBrowserApi.Invocations, i => i.Method == "updateTabCustomization");
        Assert.Equal("""
            {"tabId":"tab-1","customTitle":"Custom"}
            """,
            invocation.Arguments
        );
        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.Equal("Custom", customization.CustomTitle);
    }

    [Fact]
    public void Publishing_a_TabDisableFixedAddressChangedEvent_persists_the_flag_value()
    {
        TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        PubSub.Instance.Publish(new TabDisableFixedAddressChangedEvent("tab-1", true));

        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.True(customization.DisableFixedAddress);
    }

    [Fact]
    public void Publishing_a_TabClosedEvent_deletes_customizations_for_that_tab_id()
    {
        TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "A" });
        var tab = TypeConstructor.CreateTabBrowser("tab-1");

        PubSub.Instance.Publish(new TabClosedEvent(tab));

        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.Null(customization.CustomTitle);
        Assert.False(customization.DisableFixedAddress);
        Assert.Contains("tab-1", context.TabCustomizationStateManager.DeletedTabIds);
    }

    [Fact]
    public void Publishing_an_EphemeralTabsExpiredEvent_deletes_customizations_for_those_tab_ids()
    {
        TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "A" });
        context.TabCustomizationStateManager.SaveCustomization("tab-2", c => c with { CustomTitle = "B" });

        PubSub.Instance.Publish(new EphemeralTabsExpiredEvent(["tab-1", "tab-2"]));

        var after1 = context.TabCustomizationStateManager.GetCustomization("tab-1");
        var after2 = context.TabCustomizationStateManager.GetCustomization("tab-2");
        Assert.Null(after1.CustomTitle);
        Assert.Null(after2.CustomTitle);
    }

    [Fact]
    public void Getting_customizations_for_a_tab_returns_the_current_state_manager_value()
    {
        var feature = TestBrowserContext.CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "X", DisableFixedAddress = true });

        var customization = feature.GetCustomizationsForTab("tab-1");

        Assert.Equal("tab-1", customization.TabId);
        Assert.Equal("X", customization.CustomTitle);
        Assert.True(customization.DisableFixedAddress);
    }
}
