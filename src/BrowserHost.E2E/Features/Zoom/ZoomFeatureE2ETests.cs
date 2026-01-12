using BrowserHost.E2E.Infrastructure;
using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost.E2E.Features.Zoom;

public class ZoomFeatureE2ETests
{
    [Fact]
    public Task Zoom_in_E2E_test() =>
        StaTestRunner.RunAsync(async () =>
        {
            using var host = E2ETabBrowserHost.Create();
            TabBrowser tab = host.Tab;

            tab.SetAddress("about:blank", setManualAddress: false);
            await host.WaitForPageLoad();

            host.SimulateMouseWheel(delta: 120, modifiers: ModifierKeys.Control);

            var zoomLevel = await tab.GetZoomLevelAsync();
            Assert.Equal(0.2, zoomLevel);
        });
}
