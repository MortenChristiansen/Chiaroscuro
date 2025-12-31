using BrowserHost.Tab;
using System.Windows;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabBrowser(string? id = null) : ITabBrowser
{
    public string Id { get; set; } = id ?? $"{Guid.NewGuid()}";
    public double ZoomLevel { get; set; }
    public string? CurrentDomain => null;

    public event DependencyPropertyChangedEventHandler? AddressChanged;

    public List<string> ExecutedScripts { get; } = [];

    public List<FindInvocation> FindInvocations { get; } = [];
    public bool StopFindingCalled { get; private set; }
    public bool StopFindingClearSelection { get; private set; }

    public bool SetZoomCalled { get; private set; }
    public bool ResetZoomCalled { get; private set; }

    public Task<double> GetZoomLevelAsync() => Task.FromResult(ZoomLevel);

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

    public record FindInvocation(string SearchText, bool Forward, bool MatchCase, bool FindNext);
}

internal static class TabBrowserExtensions
{
    extension(TabBrowser tabBrowser)
    {
        public void SetTabAddress(string address)
        {
            tabBrowser.GetTabWebBrowser().SetAddress(address, setManualAddress: false);
        }

        public FakeTabWebBrowser GetTabWebBrowser()
        {
            var browserField = typeof(TabBrowser).GetField("_browser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?? throw new InvalidOperationException("TabBrowser._browser field not found - internal structure may have changed");
            return (FakeTabWebBrowser)browserField.GetValue(tabBrowser)!;
        }
    }
}
