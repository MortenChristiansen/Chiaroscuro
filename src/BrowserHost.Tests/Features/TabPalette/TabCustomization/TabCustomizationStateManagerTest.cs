using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.TabPalette.TabCustomization;

public class TabCustomizationStateManagerTest
{
    [Fact]
    public void Saving_a_tab_customization_persists_it_and_it_can_be_restored()
    {
        var fileSystem = new MockFileSystem();
        var manager = new TabCustomizationStateManager(fileSystem);

        manager.SaveCustomization("tab-1", c => c with { CustomTitle = "Hello" });

        var restored = new TabCustomizationStateManager(fileSystem).GetAllCustomizations();
        var customization = Assert.Single(restored);
        Assert.Equal("tab-1", customization.TabId);
        Assert.Equal("Hello", customization.CustomTitle);
    }

    [Fact]
    public void Saving_a_tab_customization_with_no_changes_does_not_modify_the_persisted_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new TabCustomizationStateManager(fileSystem);
        var rootFolder = Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "tab-customization");
        manager.SaveCustomization("tab-1", c => c with { CustomTitle = "Hello" });
        var file = Assert.Single(fileSystem.Directory.EnumerateFiles(rootFolder, "customization.json", System.IO.SearchOption.AllDirectories));
        var expectedWriteTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        fileSystem.File.SetLastWriteTimeUtc(file, expectedWriteTime);
        var expectedContents = fileSystem.File.ReadAllText(file);

        manager.SaveCustomization("tab-1", c => c with { CustomTitle = "Hello" });

        Assert.Equal(expectedWriteTime, fileSystem.File.GetLastWriteTimeUtc(file));
        Assert.Equal(expectedContents, fileSystem.File.ReadAllText(file));
    }
}
