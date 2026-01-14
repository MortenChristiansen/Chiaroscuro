using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Tests.Features.TabPalette.TabCustomization;

public class TabCustomizationFeatureTests
{
    [Fact]
    public void Configuring_the_feature_sends_all_existing_custom_titles_to_the_action_context()
    {
        CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx =>
            {
                ctx.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "One" });
                ctx.TabCustomizationStateManager.SaveCustomization("tab-2", c => c with { CustomTitle = "Two" });
            })
            .BuildTabCustomizationFeature();

        var invocation = Assert.Single(context.TabsBrowserApi.Invocations, i => i.Method == "setTabCustomizations");
        Assert.Equal("""
            [{"tabId":"tab-1","customTitle":"One"},{"tabId":"tab-2","customTitle":"Two"}]
            """,
            invocation.Arguments
        );
    }

    [Fact]
    public void Publishing_a_TabPaletteRequestedEvent_initializes_custom_settings_when_there_is_a_current_tab()
    {
        CreateFeature
            .WithCurrentTab(out var tab, "tab-1")
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "Hello" });

        context.PubSub.Publish(new TabPaletteRequestedEvent());

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
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        Assert.DoesNotContain(context.TabCustomizationBrowserApi.Invocations, i => i.Method == "initCustomSettings");
    }

    [Fact]
    public void Sending_a_ChangeTabCustomTitleCommand_updates_action_context_and_persists_the_custom_title()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        context.PubSub.Send(new ChangeTabCustomTitleCommand("tab-1", "Custom"));

        var invocation = Assert.Single(context.TabsBrowserApi.Invocations, i => i.Method == "updateTabCustomization");
        Assert.Equal("""
            {"tabId":"tab-1","customTitle":"Custom"}
            """,
            invocation.Arguments
        );
        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.Equal("Custom", customization.CustomTitle);
    }

    [Fact]
    public void Sending_a_ChangeTabDisableFixedAddressCommand_persists_the_flag_value()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();

        context.PubSub.Send(new ChangeTabDisableFixedAddressCommand("tab-1", true));

        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.True(customization.DisableFixedAddress);
    }

    [Fact]
    public void Publishing_a_TabClosedEvent_deletes_customizations_for_that_tab_id()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "A" });
        var tab = TypeConstructor.CreateTabBrowser("tab-1");

        context.PubSub.Publish(new TabClosedEvent(tab.Id, tab));

        var customization = context.TabCustomizationStateManager.GetCustomization("tab-1");
        Assert.Null(customization.CustomTitle);
        Assert.False(customization.DisableFixedAddress);
        Assert.DoesNotContain(context.TabCustomizationStateManager.GetAllCustomizations(), c => c.TabId == "tab-1");
    }

    [Fact]
    public void Sending_an_ExpireEphemeralTabsCommand_deletes_customizations_for_those_tab_ids()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "A", DisableFixedAddress = true });
        context.TabCustomizationStateManager.SaveCustomization("tab-2", c => c with { CustomTitle = "B", DisableFixedAddress = true });

        context.PubSub.Send(new ExpireEphemeralTabsCommand(["tab-1", "tab-2"]));

        var after1 = context.TabCustomizationStateManager.GetCustomization("tab-1");
        var after2 = context.TabCustomizationStateManager.GetCustomization("tab-2");
        Assert.Null(after1.CustomTitle);
        Assert.Null(after2.CustomTitle);
        Assert.False(after1.DisableFixedAddress);
        Assert.False(after2.DisableFixedAddress);
        Assert.DoesNotContain(context.TabCustomizationStateManager.GetAllCustomizations(), c => c.TabId is "tab-1" or "tab-2");
    }

    [Fact]
    public void Getting_customizations_for_a_tab_returns_the_current_state_manager_value()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildTabCustomizationFeature();
        context.TabCustomizationStateManager.SaveCustomization("tab-1", c => c with { CustomTitle = "X", DisableFixedAddress = true });

        var customization = feature.GetCustomizationsForTab("tab-1");

        Assert.Equal("tab-1", customization.TabId);
        Assert.Equal("X", customization.CustomTitle);
        Assert.True(customization.DisableFixedAddress);
    }
}
