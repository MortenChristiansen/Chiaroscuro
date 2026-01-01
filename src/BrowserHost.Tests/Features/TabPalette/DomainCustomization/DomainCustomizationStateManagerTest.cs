using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Utilities;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.TabPalette.DomainCustomization;

public class DomainCustomizationStateManagerTest
{
    [Fact]
    public void Saving_a_domain_customization_persists_it_and_it_can_be_restored()
    {
        var fileSystem = new MockFileSystem();
        var manager = new DomainCustomizationStateManager(fileSystem);

        manager.SaveCustomization(new DomainCustomizationDataV1("example.com", CssEnabled: true, HasCustomCss: false));

        var restored = new DomainCustomizationStateManager(fileSystem).GetCustomization("example.com");
        Assert.True(restored.CssEnabled);
        Assert.False(restored.HasCustomCss);
    }

    [Fact]
    public void Saving_a_domain_customization_sets_has_custom_css_when_the_css_file_exists()
    {
        var fileSystem = new MockFileSystem();
        var manager = new DomainCustomizationStateManager(fileSystem);
        var domainFolder = Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "domain-settings", "example.com");
        fileSystem.Directory.CreateDirectory(domainFolder);
        fileSystem.File.WriteAllText(Path.Combine(domainFolder, "custom.css"), "body { color: red; }");

        var saved = manager.SaveCustomization(new DomainCustomizationDataV1("example.com", CssEnabled: true, HasCustomCss: false));

        Assert.NotNull(saved);
        Assert.True(saved.HasCustomCss);
    }

    [Fact]
    public void Removing_custom_css_deletes_the_css_file()
    {
        var fileSystem = new MockFileSystem();
        var manager = new DomainCustomizationStateManager(fileSystem);
        var domainFolder = Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "domain-settings", "example.com");
        var cssPath = Path.Combine(domainFolder, "custom.css");
        fileSystem.Directory.CreateDirectory(domainFolder);
        fileSystem.File.WriteAllText(cssPath, "body { color: red; }");

        manager.RemoveCustomCss("example.com");

        Assert.False(fileSystem.File.Exists(cssPath));
    }
}
