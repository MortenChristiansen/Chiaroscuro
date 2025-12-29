using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled);
public record DomainCssEditRequestedEvent(string Domain);
public record DomainCustomCssRemovedEvent(string Domain);

public class DomainCustomizationBackendApi : BackendApi
{
    public void SetCssEnabled(bool enabled)
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            PubSub.Instance.Publish(new DomainCustomizationChangedEvent(domain, enabled));
    }

    public void EditCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            PubSub.Instance.Publish(new DomainCssEditRequestedEvent(domain));
    }

    public void RemoveCss()
    {
        var domain = MainWindow.Instance.CurrentTab?.CurrentDomain;
        if (domain != null)
            PubSub.Instance.Publish(new DomainCustomCssRemovedEvent(domain));
    }
}