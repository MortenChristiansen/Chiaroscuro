using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled);
public record DomainCssEditRequestedEvent(string Domain);
public record DomainCustomCssRemovedEvent(string Domain);

public class DomainCustomizationBackendApi(PubSub pubSub) : BackendApi
{
    public void SetCssEnabled(bool enabled)
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Publish(new DomainCustomizationChangedEvent(domain, enabled));
    }

    public void EditCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Publish(new DomainCssEditRequestedEvent(domain));
    }

    public void RemoveCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            pubSub.Publish(new DomainCustomCssRemovedEvent(domain));
    }
}