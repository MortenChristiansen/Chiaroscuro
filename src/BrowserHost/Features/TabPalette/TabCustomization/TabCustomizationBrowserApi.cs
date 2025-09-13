using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle);
public record TabDisableFixedAddressChangedEvent(string TabId, bool IsDisabled);

public class TabCustomizationBrowserApi : BrowserApi
{
    public void SetCustomTitle(string? newTitle) =>
        PubSub.Publish(new TabCustomTitleChangedEvent(MainWindow.Instance.CurrentTab!.Id, newTitle));

    public void SetDisableFixedAddress(bool disabled) =>
        PubSub.Publish(new TabDisableFixedAddressChangedEvent(MainWindow.Instance.CurrentTab!.Id, disabled));
}
