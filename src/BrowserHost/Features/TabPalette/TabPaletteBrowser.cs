using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.TabPalette;


public class TabPaletteBrowser : Browser<TabPaletteBrowserApi>
{
    public override TabPaletteBrowserApi Api { get; } = new();

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    { }

    public void FindStatusChanged(int totalMatches)
    {
        CallClientApi("findStatusChanged", $"{totalMatches}");
    }
}
