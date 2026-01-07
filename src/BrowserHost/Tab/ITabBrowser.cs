using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.TabCustomization;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserHost.Tab;

/// <summary>
/// Abstracts the TabBrowser functionality.
/// </summary>
public interface ITabBrowser
{
    string Id { get; }
    string? CurrentDomain { get; }
    string Address { get; }
    string? ManualAddress { get; }
    string? Favicon { get; }
    string Title { get; set; }

    event DependencyPropertyChangedEventHandler? AddressChanged;

    Task ExecuteScriptAsync(string script);
    void RegisterContentPageApi(BackendApi api, string name);
    void Dispose();

    // Custom Windows Chrome

    bool IsLoading { get; }

    // Workspaces

    string GetAddressToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations);
    string GetTitleToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations);
    string? GetFaviconToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations);
    void RestoreOriginalAddress();

    // Tabs

    void SetAddress(string address, bool setManualAddress);
    void SavePersistableState();

    // Dev Tools

    bool HasDevTools { get; }
    void ShowDevTools();
    void CloseDevTools();

    // Zoom

    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();

    // Find Text

    void Find(string searchText, bool forward, bool matchCase, bool findNext);
    void StopFinding(bool clearSelection);
}
