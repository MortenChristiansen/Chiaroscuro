using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle);
public record TabDisableStaticAddressChangedEvent(string TabId, bool DisableStaticAddress);

public class TabCustomizationBrowserApi : BrowserApi
{
    public void SetCustomTitle(string? newTitle) =>
        PubSub.Publish(new TabCustomTitleChangedEvent(MainWindow.Instance.CurrentTab!.Id, newTitle));

    public void SetDisableStaticAddress(bool disabled) =>
        PubSub.Publish(new TabDisableStaticAddressChangedEvent(MainWindow.Instance.CurrentTab!.Id, disabled));
}
