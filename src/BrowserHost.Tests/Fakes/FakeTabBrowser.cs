using BrowserHost.Tab;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabBrowser(string? id = null) : ITabBrowser
{
    public string Id { get; set; } = id ?? $"{Guid.NewGuid()}";
    public double ZoomLevel { get; set; }

    public record FindInvocation(string SearchText, bool Forward, bool MatchCase, bool FindNext);
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
}
