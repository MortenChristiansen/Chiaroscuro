using BrowserHost.Features.Settings;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.Settings;

public class SettingsStateManagerTests
{
    [Fact]
    public void Saving_settings_persists_them_and_they_can_be_restored_in_a_new_instance()
    {
        var fileSystem = new MockFileSystem();
        var manager = new SettingsStateManager(fileSystem);
        var settings = new SettingsDataV1("UA", ["a.com", "b.com"], true);

        manager.SaveSettings(settings);

        var restored = new SettingsStateManager(fileSystem).RestoreSettingsFromDisk();
        Assert.Equal("UA", restored.UserAgent);
        Assert.Equal(new[] { "a.com", "b.com" }, restored.SsoEnabledDomains);
        Assert.True(restored.AutoAddSsoDomains);
    }

    [Fact]
    public void Saving_settings_with_no_changes_does_not_modify_the_persisted_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new SettingsStateManager(fileSystem);
        var settingsPath = SettingsStateManager.PersistedStatePath;
        var settings = new SettingsDataV1("UA", ["a.com"], false);
        manager.SaveSettings(settings);
        var expectedWriteTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        fileSystem.File.SetLastWriteTimeUtc(settingsPath, expectedWriteTime);
        var expectedContents = fileSystem.File.ReadAllText(settingsPath);

        manager.SaveSettings(new SettingsDataV1("UA", ["a.com"], false));

        Assert.Equal(expectedWriteTime, fileSystem.File.GetLastWriteTimeUtc(settingsPath));
        Assert.Equal(expectedContents, fileSystem.File.ReadAllText(settingsPath));
    }
}
