using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser() : Browser("/tab-palette", disableContextMenu: true)
{
}
