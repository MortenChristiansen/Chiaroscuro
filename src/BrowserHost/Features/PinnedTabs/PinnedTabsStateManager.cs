namespace BrowserHost.Features.PinnedTabs;

public record PinnedTabData(PinnedTabDtoV1[] PinnedTabs, string? ActiveTabId);
public record PinnedTabDtoV1(string Id, string? Title, string? Favicon, string Address);

public static class PinnedTabsStateManager
{
    //private static readonly string _pinnedTabsFilePath = "pinned_tabs.json";
    //private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    //private const int _currentVersion = 1;
    private static PinnedTabData _lastSavedPinnedTabsData = new([], null);

    public static PinnedTabData SavePinnedTabs(PinnedTabData pinnedTabsData)
    {
        _lastSavedPinnedTabsData = pinnedTabsData;

        // TODO: Implement file saving logic

        return _lastSavedPinnedTabsData;
    }

    public static PinnedTabData RestorePinnedTabsFromDisk()
    {
        // TODO: Implement file loading logic

        return _lastSavedPinnedTabsData;
    }
}
