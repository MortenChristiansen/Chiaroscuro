using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomizationChangedEvent(string TabId, string? CustomTitle);

public class TabCustomizationBrowserApi : BrowserApi
{
    public void SetCustomTitle(string? newTitle) =>
        PubSub.Publish(new TabCustomizationChangedEvent(MainWindow.Instance.CurrentTab!.Id, newTitle));
}
