using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle);
public record TabDisableFixedAddressChangedEvent(string TabId, bool IsDisabled);
public record TabNotificationPermissionChangedEvent(string TabId, NotificationPermissionStatus Permission);

public class TabCustomizationBrowserApi : BrowserApi
{
    public void SetCustomTitle(string? newTitle)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            PubSub.Publish(new TabCustomTitleChangedEvent(tab.Id, newTitle));
    }

    public void SetDisableFixedAddress(bool disabled)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            PubSub.Publish(new TabDisableFixedAddressChangedEvent(tab.Id, disabled));
    }

    public void SetNotificationPermission(int permissionStatus)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
        {
            var permission = (NotificationPermissionStatus)permissionStatus;
            PubSub.Publish(new TabNotificationPermissionChangedEvent(tab.Id, permission));
        }
    }
}
