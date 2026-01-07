using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using System.Net;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.ActionDialog;

public class ActionDialogFeatureTest
{
    [Fact]
    public void Executing_a_command_with_a_search_provider_navigates_to_the_provider_search_url()
    {
        CreateFeature
            .CaptureContext(out var context)
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .BuildActionDialogFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        PubSubMessages.Clear();

        context.PubSub.Send(new ExecuteCommandCommand("!g hello world", Ctrl: false));

        var creation = Assert.Single(context.TabCreations);
        Assert.Equal("https://www.google.com/search?q=" + WebUtility.UrlEncode("hello world"), creation.Address);
        var executed = Assert.Single(PubSubMessages.OfType<CommandExecutedEvent>());
        Assert.Equal("!g hello world", executed.Command);
        Assert.False(executed.Ctrl);
    }

    [Fact]
    public void Executing_the_settings_content_page_opens_the_settings_page_in_a_new_tab_without_saving_history()
    {
        CreateFeature
            .CaptureContext(out var context)
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .BuildActionDialogFeature();
        context.NewTabIdsToGenerate.Enqueue("tab-1");
        PubSubMessages.Clear();

        context.PubSub.Send(new ExecuteCommandCommand("/settings", Ctrl: false));

        var creation = Assert.Single(context.TabCreations);
        Assert.StartsWith("http", creation.Address, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("/settings", creation.Address, StringComparison.OrdinalIgnoreCase);
        Assert.False(creation.SetManualAddress);
    }

    [Fact]
    public void Changing_the_action_dialog_value_updates_suggestions_and_publishes_a_value_changed_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildActionDialogFeature();
        var history = new NavigationHistoryStateManager(context.FileSystem);
        history.SaveNavigationEntry("https://example.com", "Example", favicon: null);
        context.ActionDialogBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        context.PubSub.Send(new ChangeActionDialogValueCommand("ex"));

        Assert.Contains(context.ActionDialogBrowserApi.Invocations, i => i.Method == "updateSuggestions");
        Assert.Contains(PubSubMessages.OfType<ActionDialogValueChangedEvent>(), e => e.Value == "ex");
    }

    [Fact]
    public void Dismissing_the_action_dialog_hides_it_and_publishes_a_dismissed_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildActionDialogFeature();
        context.ActionDialogWindowOperations.ShowActionDialog();
        PubSubMessages.Clear();

        context.PubSub.Send(new DismissActionDialogCommand());

        Assert.Equal(1, context.ActionDialogWindowOperations.HideCallCount);
        Assert.Single(PubSubMessages.OfType<ActionDialogDismissedEvent>());
    }

    [Fact]
    public void Pressing_Ctrl_and_T_shows_the_action_dialog_and_is_handled()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildActionDialogFeature();
        context.ActionDialogBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.T));

        Assert.True(handled);
        Assert.Equal(1, context.ActionDialogWindowOperations.ShowCallCount);
        Assert.Contains(context.ActionDialogBrowserApi.Invocations, i => i.Method == "showDialog");
        Assert.Single(PubSubMessages.OfType<ActionDialogShownEvent>());
    }
}
