using BrowserHost.Tab;

namespace BrowserHost.Tests.Infrastructure;

internal class TestTabBrowser : ITabBrowser
{
    public string Id { get; set; } = $"{Guid.NewGuid()}";
    public double ZoomLevel { get; set; }

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
    }
}
