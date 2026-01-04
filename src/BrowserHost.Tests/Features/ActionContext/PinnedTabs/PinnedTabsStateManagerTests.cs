using BrowserHost.Features.ActionContext.PinnedTabs;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.ActionContext.PinnedTabs;

public class PinnedTabsStateManagerTests
{
    [Fact]
    public void Restoring_pinned_tabs_when_no_state_file_exists_returns_empty_state()
    {
        var fileSystem = new MockFileSystem();
        var manager = new PinnedTabsStateManager(fileSystem);

        var restored = manager.RestorePinnedTabsFromDisk();

        Assert.Empty(restored.PinnedTabs);
        Assert.Null(restored.ActiveTabId);
    }

    [Fact]
    public void Saving_pinned_tabs_persists_the_state_and_it_can_be_restored_in_a_new_instance()
    {
        var fileSystem = new MockFileSystem();
        var manager = new PinnedTabsStateManager(fileSystem);
        var data = new PinnedTabDataV1(
            [new PinnedTabDtoV1("tab-1", "Example", null, "https://example.com/")],
            ActiveTabId: "tab-1"
        );

        manager.SavePinnedTabs(data);
        var restored = new PinnedTabsStateManager(fileSystem).RestorePinnedTabsFromDisk();

        Assert.Equal("tab-1", restored.ActiveTabId);
        var tab = Assert.Single(restored.PinnedTabs);
        Assert.Equal(new PinnedTabDtoV1("tab-1", "Example", null, "https://example.com/"), tab);
    }

    [Fact]
    public void Restoring_pinned_tabs_ignores_the_state_file_when_the_version_does_not_match()
    {
        var fileSystem = new MockFileSystem();
        var statePath = PinnedTabsStateManager.PersistedStatePath;
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(statePath)!);
        fileSystem.File.WriteAllText(statePath, "{\"Version\":0,\"Data\":{\"PinnedTabs\":[{\"Id\":\"tab-1\",\"Title\":\"Example\",\"Favicon\":null,\"Address\":\"https://example.com/\"}],\"ActiveTabId\":\"tab-1\"}}");

        var restored = new PinnedTabsStateManager(fileSystem).RestorePinnedTabsFromDisk();

        Assert.Empty(restored.PinnedTabs);
        Assert.Null(restored.ActiveTabId);
    }

    [Fact]
    public void Saving_pinned_tabs_with_no_changes_does_not_modify_the_persisted_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new PinnedTabsStateManager(fileSystem);
        var statePath = PinnedTabsStateManager.PersistedStatePath;
        var initialState = new PinnedTabDataV1(
            [new PinnedTabDtoV1("tab-1", "Example", null, "https://example.com/")],
            ActiveTabId: "tab-1"
        );
        manager.SavePinnedTabs(initialState);
        var expectedWriteTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        fileSystem.File.SetLastWriteTimeUtc(statePath, expectedWriteTime);
        var expectedContents = fileSystem.File.ReadAllText(statePath);

        manager.SavePinnedTabs(new PinnedTabDataV1(
            [new PinnedTabDtoV1("tab-1", "Example", null, "https://example.com/")],
            ActiveTabId: "tab-1"
        ));

        Assert.Equal(expectedWriteTime, fileSystem.File.GetLastWriteTimeUtc(statePath));
        Assert.Equal(expectedContents, fileSystem.File.ReadAllText(statePath));
    }
}
