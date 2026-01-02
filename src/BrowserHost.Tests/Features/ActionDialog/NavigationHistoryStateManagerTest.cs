using BrowserHost.Features.ActionDialog;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Features.ActionDialog;

public class NavigationHistoryStateManagerTest
{
    [Fact]
    public void Saving_a_navigation_entry_persists_it_and_it_can_be_suggested()
    {
        var fileSystem = new MockFileSystem();
        var manager = new NavigationHistoryStateManager(fileSystem);

        manager.SaveNavigationEntry("https://example.com/", "Example", null);

        var suggestions = new NavigationHistoryStateManager(fileSystem).GetSuggestions("exa");
        var suggested = Assert.Single(suggestions, s => s.Address == "example.com");
        Assert.Equal("Example", suggested.Title);
    }

    [Fact]
    public void Saving_a_file_url_does_not_persist_anything()
    {
        var fileSystem = new MockFileSystem();
        var manager = new NavigationHistoryStateManager(fileSystem);
        var historyPath = NavigationHistoryStateManager.NavigationHistoryPath;

        manager.SaveNavigationEntry("file://c:/temp/a.txt", "A", null);

        Assert.False(fileSystem.File.Exists(historyPath));
    }
}
