using BrowserHost.CefInfrastructure;
using BrowserHost.Tab;
using System.Windows;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabBrowser(string? id = null) : ITabBrowser
{
    public string Id { get; set; } = id ?? $"{Guid.NewGuid()}";
    public double ZoomLevel { get; set; }

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

    public string Address { get; set; } = "";

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

    public record FindInvocation(string SearchText, bool Forward, bool MatchCase, bool FindNext);
    public record RegisteredContentPageApi(string Name, BackendApi Api);
}
