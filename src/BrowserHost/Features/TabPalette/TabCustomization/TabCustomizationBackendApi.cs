using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record ChangeTabCustomTitleCommand(string TabId, string? CustomTitle) : ICommand;
public record ChangeTabDisableFixedAddressCommand(string TabId, bool IsDisabled) : ICommand;

public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle) : IEvent;
public record TabDisableFixedAddressChangedEvent(string TabId, bool IsDisabled) : IEvent;

public class TabCustomizationBackendApi(PubSub pubSub) : BackendApi
{
    public void SetCustomTitle(string? newTitle)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            pubSub.Send(new ChangeTabCustomTitleCommand(tab.Id, newTitle));
    }

    public void SetDisableFixedAddress(bool disabled)
    {
        if (MainWindow.Instance.CurrentTab is { } tab)
            pubSub.Send(new ChangeTabDisableFixedAddressCommand(tab.Id, disabled));
    }
}
