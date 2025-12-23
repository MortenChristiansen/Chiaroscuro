using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled);
public record DomainCssEditRequestedEvent(string Domain);
public record DomainCustomCssRemovedEvent(string Domain);

public class DomainCustomizationBrowserApi : BrowserApi
{
    public void SetCssEnabled(bool enabled)
    {
        var domain = GetCurrentDomain();
        if (domain != null)
            PubSub.Instance.Publish(new DomainCustomizationChangedEvent(domain, enabled));
    }

    public void EditCss()
    {
        var domain = GetCurrentDomain();
        if (domain != null)
            PubSub.Instance.Publish(new DomainCssEditRequestedEvent(domain));
    }

    public void RemoveCss()
    {
        var domain = GetCurrentDomain();
        if (domain == null) return;

        PubSub.Instance.Publish(new DomainCustomCssRemovedEvent(domain));
    }

    private static string? GetCurrentDomain()
    {
        var address = MainWindow.Instance.CurrentTab?.Address;
        if (string.IsNullOrWhiteSpace(address)) return null;
        try
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme is not "http" and not "https") return null;
            return string.IsNullOrEmpty(uri.Host) ? null : uri.Host;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to extract domain from address {address}: {ex}");
            return null;
        }
    }
}