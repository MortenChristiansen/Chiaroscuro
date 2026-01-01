using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Utilities;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.ActionContext.Workspaces;

public class WorkspaceStateManagerTest
{
    [Fact]
    public void Saving_workspace_tabs_persists_them_and_they_can_be_restored_in_a_new_instance()
    {
        var fileSystem = new MockFileSystem();
        var manager = new WorkspaceStateManager(fileSystem);
        var initialWorkspaces = manager.RestoreWorkspacesFromDisk();
        var workspaceId = initialWorkspaces[0].WorkspaceId;
        var tabs = new[] { new WorkspaceTabStateDtoV1("tab-1", "https://example.com/", "Example", null, IsActive: true, Created: DateTimeOffset.UtcNow) };

        manager.SaveWorkspaceTabs(workspaceId, tabs, ephemeralTabStartIndex: 1, folders: []);
        var restoredWorkspaces = new WorkspaceStateManager(fileSystem).RestoreWorkspacesFromDisk();

        var restored = Assert.Single(restoredWorkspaces, w => w.WorkspaceId == workspaceId);
        var tab = Assert.Single(restored.Tabs);
        Assert.Equal("tab-1", tab.TabId);
        Assert.Equal("https://example.com/", tab.Address);
        Assert.Equal("Example", tab.Title);
        Assert.True(tab.IsActive);
    }

    [Fact]
    public void Saving_workspace_tabs_with_no_changes_does_not_modify_the_persisted_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new WorkspaceStateManager(fileSystem);
        var statePath = AppDataPathManager.GetAppDataFilePath("workspaces.json");
        var initialWorkspaces = manager.RestoreWorkspacesFromDisk();
        var workspaceId = initialWorkspaces[0].WorkspaceId;
        var now = DateTimeOffset.UtcNow;
        var tabs = new[] { new WorkspaceTabStateDtoV1("tab-1", "https://example.com/", "Example", null, IsActive: true, Created: now) };
        manager.SaveWorkspaceTabs(workspaceId, tabs, ephemeralTabStartIndex: 1, folders: []);
        var expectedWriteTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        fileSystem.File.SetLastWriteTimeUtc(statePath, expectedWriteTime);
        var expectedContents = fileSystem.File.ReadAllText(statePath);

        manager.SaveWorkspaceTabs(workspaceId, tabs, ephemeralTabStartIndex: 1, folders: []);

        Assert.Equal(expectedWriteTime, fileSystem.File.GetLastWriteTimeUtc(statePath));
        Assert.Equal(expectedContents, fileSystem.File.ReadAllText(statePath));
    }
}
