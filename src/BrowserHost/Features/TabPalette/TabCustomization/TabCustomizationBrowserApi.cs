using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record CustomTitleChangedEvent(string? NewTitle);

public class TabCustomizationBrowserApi : BrowserApi
{
    public void SetCustomTitle(string? newTitle) =>
        PubSub.Publish(new CustomTitleChangedEvent(newTitle));
}
