using BrowserHost.Features.AppState;
using BrowserHost.Utilities;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.AppState;

public class AppStateStateManagerTest
{
    [Fact]
    public void Saving_action_context_width_clamps_to_the_minimum_and_can_be_restored()
    {
        var fileSystem = new MockFileSystem();
        var manager = new AppStateStateManager(fileSystem);

        var saved = manager.SaveActionContextWidth(100);
        var restored = new AppStateStateManager(fileSystem).RestoreAppStateFromDisk();

        Assert.Equal(200, saved.ActionContextWidth);
        Assert.Equal(200, restored.ActionContextWidth);
    }

    [Fact]
    public void Saving_the_same_app_state_does_not_modify_the_persisted_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new AppStateStateManager(fileSystem);
        var statePath = AppDataPathManager.GetAppDataFilePath("appState.json");
        manager.SaveTabPaletteWidth(400);
        var expectedWriteTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        fileSystem.File.SetLastWriteTimeUtc(statePath, expectedWriteTime);
        var expectedContents = fileSystem.File.ReadAllText(statePath);

        manager.SaveTabPaletteWidth(400);

        Assert.Equal(expectedWriteTime, fileSystem.File.GetLastWriteTimeUtc(statePath));
        Assert.Equal(expectedContents, fileSystem.File.ReadAllText(statePath));
    }
}
