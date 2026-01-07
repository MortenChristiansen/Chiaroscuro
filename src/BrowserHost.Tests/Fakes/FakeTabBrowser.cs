using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Tab;
using System.Windows;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabBrowser(string? id = null) : ITabBrowser
{
    public string Id { get; set; } = id ?? $"{Guid.NewGuid()}";
    public double ZoomLevel { get; set; }

    public string Address { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Favicon { get; set; }

    public string? CurrentDomain
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Address))
                return null;

            return Uri.TryCreate(Address, UriKind.Absolute, out var uri) ? uri.Host : null;
        }
    }

    public bool HasDevTools { get; private set; }
    public bool ShowDevToolsCalled { get; private set; }
    public bool CloseDevToolsCalled { get; private set; }

    public event DependencyPropertyChangedEventHandler? AddressChanged;

    public List<string> ExecutedScripts { get; } = [];

    public List<FindInvocation> FindInvocations { get; } = [];
    public bool StopFindingCalled { get; private set; }
    public bool StopFindingClearSelection { get; private set; }

    public bool SetZoomCalled { get; private set; }
    public bool ResetZoomCalled { get; private set; }

    public Task<double> GetZoomLevelAsync() => Task.FromResult(ZoomLevel);

    public bool IsLoading { get; set; }

    public bool RegisterContentPageApiCalled { get; private set; }
    public List<RegisteredContentPageApi> RegisteredContentPageApis { get; } = [];


    public bool SavePersistableStateCalled { get; private set; }
    public bool DisposeCalled { get; private set; }
    public List<(string Address, bool SetManualAddress)> SetAddressInvocations { get; } = [];

    public void SetAddress(string address, bool setManualAddress)
    {
        Address = address;
        SetAddressInvocations.Add((address, setManualAddress));
    }

    public void SavePersistableState() => SavePersistableStateCalled = true;

    public void Dispose() => DisposeCalled = true;

    public void SetZoomLevel(double level)
    {
        SetZoomCalled = true;
        ZoomLevel = level;
    }

    public void ResetZoomLevel()
    {
        ResetZoomCalled = true;
        ZoomLevel = 1.0;
    }

    public void Find(string searchText, bool forward, bool matchCase, bool findNext) =>
        FindInvocations.Add(new(searchText, forward, matchCase, findNext));

    public void StopFinding(bool clearSelection)
    {
        StopFindingCalled = true;
        StopFindingClearSelection = clearSelection;
    }

    public Task ExecuteScriptAsync(string script)
    {
        ExecutedScripts.Add(script);
        return Task.CompletedTask;
    }

    public void ShowDevTools()
    {
        ShowDevToolsCalled = true;
        HasDevTools = true;
    }

    public void CloseDevTools()
    {
        CloseDevToolsCalled = true;
        HasDevTools = false;
    }

    public void RegisterContentPageApi(BackendApi api, string name)
    {
        RegisterContentPageApiCalled = true;
        RegisteredContentPageApis.Add(new RegisteredContentPageApi(name, api));
    }

    public string GetAddressToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        Address;

    public string GetTitleToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        Title;

    public string? GetFaviconToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        Favicon;

    public bool OriginalAddressRestored { get; private set; }
    public void RestoreOriginalAddress()
    {
        OriginalAddressRestored = true;
    }

    public record FindInvocation(string SearchText, bool Forward, bool MatchCase, bool FindNext);
    public record RegisteredContentPageApi(string Name, BackendApi Api);
}
