using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System;
using System.Text.Json;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.ActionContext.Folders;

public class FoldersFeatureTest
{
    [Fact]
    public void Pressing_Ctrl_and_G_creates_a_new_folder_for_the_current_bookmarked_tab_and_is_handled()
    {
        var foldersFeature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .IncludeRequiredFeature(b => b.BuildPinnedTabsFeature())
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .IncludeRequiredFeature(b => b.BuildTabCustomizationFeature())
            .IncludeRequiredFeature(b => b.BuildWorkspacesFeature(), out var workspacesFeature)
            .BuildFoldersFeature();
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
                    ),
                    new WorkspaceTabStateDtoV1(
                        TabId: "tab-2",
                        Address: "https://two.example.com",
                        Title: "Two",
                        Favicon: null,
                        IsActive: false,
                        Created: DateTimeOffset.UtcNow
                    )
                ],
                EphemeralTabStartIndex: 2
            )
        );
        workspacesFeature.Start();
        context.TabsBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        var handled = foldersFeature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.G));

        Assert.True(handled);
        var changed = Assert.Single(PubSubMessages.OfType<TabsChangedEvent>());
        var folder = Assert.Single(changed.Folders);
        Assert.Equal("New Folder", folder.Name);
        Assert.Equal(0, folder.StartIndex);
        Assert.Equal(0, folder.EndIndex);
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "updateFolders");
    }

    [Fact]
    public void Pressing_Ctrl_and_G_when_the_current_tab_is_already_in_a_folder_removes_it_from_the_folder()
    {
        var foldersFeature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .IncludeRequiredFeature(b => b.BuildPinnedTabsFeature())
            .IncludeRequiredFeature(b => b.BuildTabsFeature())
            .IncludeRequiredFeature(b => b.BuildTabCustomizationFeature())
            .IncludeRequiredFeature(b => b.BuildWorkspacesFeature(), out var workspacesFeature)
            .BuildFoldersFeature();
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
                EphemeralTabStartIndex: 1
            ) { Folders = [new FolderDtoV1("folder-1", "Folder", StartIndex: 0, EndIndex: 0)] }
        );
        workspacesFeature.Start();
        context.TabsBrowserApi.ClearInvocations();
        PubSubMessages.Clear();

        var handled = foldersFeature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.G));

        Assert.True(handled);
        var changed = Assert.Single(PubSubMessages.OfType<TabsChangedEvent>());
        Assert.Empty(changed.Folders);
        Assert.Contains(context.TabsBrowserApi.Invocations, i => i.Method == "updateFolders");
    }

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
