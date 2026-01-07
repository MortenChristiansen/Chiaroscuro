using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.ActionContext.Workspaces;

public class WorkspacesFeatureTest
{
    [Fact]
    public void Starting_the_feature_restores_workspaces_activates_the_first_one_and_sets_the_workspace_color()
    {
        var feature = CreateFeature
            .WithCurrentTab()
            .CaptureContext(out var context)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context,
            new WorkspaceDtoV1(
                WorkspaceId: "ws-1",
                Name: "One",
                Color: "#112233",
                Icon: "🌐",
                Tabs: [],
                EphemeralTabStartIndex: 0
            )
        );

        feature.Start();

        Assert.Equal((Color)ColorConverter.ConvertFromString("#112233"), context.WorkspaceColor);
        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "setWorkspaces");
        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspaceActivated");
        var activated = Assert.Single(PubSubMessages.OfType<WorkspaceActivatedEvent>());
        Assert.Equal("ws-1", activated.WorkspaceId);
        Assert.Equal("One", feature.CurrentWorkspace.Name);
    }

    [Fact]
    public void Sending_a_CreateWorkspaceCommand_publishes_a_WorkspaceCreatedEvent_and_activates_the_new_workspace()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context, new WorkspaceDtoV1("ws-1", "One", "#111111", "🌐", [], 0));
        feature.Start();
        context.WorkspacesBrowserApi.ClearInvocations();

        context.PubSub.Send(new CreateWorkspaceCommand("ws-2", "Two", "🔥", "#00ff00"));

        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspacesChanged");
        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspaceActivated");
        Assert.Equal((Color)ColorConverter.ConvertFromString("#00ff00"), context.WorkspaceColor);
        var created = Assert.Single(PubSubMessages.OfType<WorkspaceCreatedEvent>());
        Assert.Equal("ws-2", created.WorkspaceId);
        Assert.Contains(PubSubMessages.OfType<WorkspaceActivatedEvent>(), e => e.WorkspaceId == "ws-2");
        Assert.Equal("Two", feature.CurrentWorkspace.Name);
    }

    [Fact]
    public void Sending_an_UpdateWorkspaceCommand_for_the_current_workspace_updates_the_workspace_color_and_publishes_a_WorkspaceUpdatedEvent()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context, new WorkspaceDtoV1("ws-1", "One", "#111111", "🌐", [], 0));
        feature.Start();

        context.PubSub.Send(new UpdateWorkspaceCommand("ws-1", "One Updated", "⭐", "#aabbcc"));

        Assert.Equal((Color)ColorConverter.ConvertFromString("#aabbcc"), context.WorkspaceColor);
        var updated = Assert.Single(PubSubMessages.OfType<WorkspaceUpdatedEvent>());
        Assert.Equal("ws-1", updated.WorkspaceId);
        Assert.Equal("One Updated", feature.CurrentWorkspace.Name);
        Assert.Equal("#aabbcc", feature.CurrentWorkspace.Color);
    }

    [Fact]
    public void Sending_a_DeleteWorkspaceCommand_when_it_is_the_last_workspace_throws()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context, new WorkspaceDtoV1("ws-1", "One", "#111111", "🌐", [], 0));
        feature.Start();

        Assert.Throws<InvalidOperationException>(() => context.PubSub.Send(new DeleteWorkspaceCommand("ws-1")));
    }

    [Fact]
    public void Sending_a_DeleteWorkspaceCommand_for_the_current_workspace_activates_the_first_remaining_workspace()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context,
            new WorkspaceDtoV1("ws-1", "One", "#112233", "🌐", [], 0),
            new WorkspaceDtoV1("ws-2", "Two", "#445566", "🔥", [], 0)
        );
        feature.Start();
        context.PubSub.Send(new ActivateWorkspaceCommand("ws-2"));
        context.WorkspacesBrowserApi.ClearInvocations();

        context.PubSub.Send(new DeleteWorkspaceCommand("ws-2"));

        var deleted = Assert.Single(PubSubMessages.OfType<WorkspaceDeletedEvent>());
        Assert.Equal("ws-2", deleted.WorkspaceId);
        Assert.Contains(PubSubMessages.OfType<WorkspaceActivatedEvent>(), e => e.WorkspaceId == "ws-1");
        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspaceActivated");
        Assert.Equal((Color)ColorConverter.ConvertFromString("#112233"), context.WorkspaceColor);
    }

    [Fact]
    public void Pressing_Ctrl_and_2_activates_the_second_workspace_and_is_handled()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context,
            new WorkspaceDtoV1("ws-1", "One", "#112233", "🌐", [], 0),
            new WorkspaceDtoV1("ws-2", "Two", "#445566", "🔥", [], 0)
        );
        feature.Start();
        context.WorkspacesBrowserApi.ClearInvocations();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.D2));

        Assert.True(handled);
        Assert.Contains(PubSubMessages.OfType<WorkspaceActivatedEvent>(), e => e.WorkspaceId == "ws-2");
        Assert.Contains(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspaceActivated");
    }

    [Fact]
    public void Trying_to_activate_a_workspace_that_does_not_exist_is_ignored()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context,
            new WorkspaceDtoV1("ws-1", "One", "#112233", "🌐", [], 0),
            new WorkspaceDtoV1("ws-2", "Two", "#445566", "🔥", [], 0)
        );
        feature.Start();
        context.WorkspacesBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.D3));

        Assert.False(handled);
        Assert.Empty(PubSubMessages.OfType<WorkspaceActivatedEvent>());
        Assert.DoesNotContain(context.WorkspacesBrowserApi.Invocations, i => i.Method == "workspaceActivated");
    }

    [Fact]
    public void Pressing_Ctrl_Shift_and_2_moves_the_current_tab_to_the_second_workspace_and_is_handled()
    {
        var feature = CreateFeature
            .WithCurrentTab("tab-1")
            .CaptureContext(out var context)
            .IncludeRequiredFeature(b => b.BuildPinnedTabsFeature())
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control | ModifierKeys.Shift)
            .BuildWorkspacesFeature();
        SeedWorkspaces(context,
            new WorkspaceDtoV1(
                WorkspaceId: "ws-1",
                Name: "One",
                Color: "#112233",
                Icon: "🌐",
                Tabs:
                [
                    new WorkspaceTabStateDtoV1(
                        TabId: "tab-1",
                        Address: "https://example.com",
                        Title: "Example",
                        Favicon: null,
                        IsActive: true,
                        Created: DateTimeOffset.UtcNow
                    )
                ],
                EphemeralTabStartIndex: 0
            ),
            new WorkspaceDtoV1("ws-2", "Two", "#445566", "🔥", [], 0)
        );
        feature.Start();
        context.TabsBrowserApi.ClearInvocations();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.D2));

        Assert.True(handled);
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "closeTab");
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "addTab");
        Assert.Contains(PubSubMessages.OfType<WorkspaceActivatedEvent>(), e => e.WorkspaceId == "ws-2");
    }

    // TODO: Test that you cannot move a pinned tab between workspaces, once the TabsFeature and PinnedTabsFeature are made testable

    private static void SeedWorkspaces(TestBrowserContext context, params WorkspaceDtoV1[] workspaces)
    {
        var state = new PersistentData<WorkspacesDataDtoV1>
        {
            Version = 1,
            Data = new WorkspacesDataDtoV1(workspaces)
        };

        var json = JsonSerializer.Serialize(state, BrowserHostJsonContext.Default.PersistentDataWorkspacesDataDtoV1);

        var directory = context.FileSystem.Path.GetDirectoryName(WorkspaceStateManager.PersistedStatePath);
        if (!string.IsNullOrWhiteSpace(directory))
            context.FileSystem.Directory.CreateDirectory(directory);

        context.FileSystem.File.WriteAllText(WorkspaceStateManager.PersistedStatePath, json);
    }
}
