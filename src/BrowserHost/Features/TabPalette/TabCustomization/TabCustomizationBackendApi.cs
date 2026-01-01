using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle);
public record TabDisableFixedAddressChangedEvent(string TabId, bool IsDisabled);

public class TabCustomizationBackendApi(PubSub pubSub) : BackendApi
{
    public void SetCustomTitle(string? newTitle)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            pubSub.Publish(new TabCustomTitleChangedEvent(tab.Id, newTitle));
    }

    public void SetDisableFixedAddress(bool disabled)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            pubSub.Publish(new TabDisableFixedAddressChangedEvent(tab.Id, disabled));
    }
}
