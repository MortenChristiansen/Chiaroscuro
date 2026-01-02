using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record ChangeDomainCustomizationCommand(string Domain, bool CssEnabled) : ICommand;
public record EditDomainCssCommand(string Domain) : ICommand;
public record RemoveDomainCustomCssCommand(string Domain) : ICommand;

public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled) : IEvent;
public record DomainCssEditRequestedEvent(string Domain) : IEvent;
public record DomainCustomCssRemovedEvent(string Domain) : IEvent;

public class DomainCustomizationBackendApi(PubSub pubSub) : BackendApi
{
    public void SetCssEnabled(bool enabled)
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Send(new ChangeDomainCustomizationCommand(domain, enabled));
    }

    public void EditCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Send(new EditDomainCssCommand(domain));
    }

    public void RemoveCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Send(new RemoveDomainCustomCssCommand(domain));
    }
}