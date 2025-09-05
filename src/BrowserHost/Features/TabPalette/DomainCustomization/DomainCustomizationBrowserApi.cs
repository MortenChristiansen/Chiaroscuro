using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled);
public record DomainCssEditRequestedEvent(string Domain);

public class DomainCustomizationBrowserApi : BrowserApi
{
    public void SetCssEnabled(bool enabled)
    {
        var domain = GetCurrentDomain();
        if (domain != null)
            PubSub.Publish(new DomainCustomizationChangedEvent(domain, enabled));
    }

    public void EditCss()
    {
        var domain = GetCurrentDomain();
        if (domain != null)
            PubSub.Publish(new DomainCssEditRequestedEvent(domain));
    }

    public void RemoveCss()
    {
        var domain = GetCurrentDomain();
        if (domain == null) return;

        try
        {
            var cssPath = DomainCustomizationStateManager.GetCustomCssPath(domain);
            if (File.Exists(cssPath))
            {
                File.Delete(cssPath);
                DomainCustomizationStateManager.RefreshCacheForDomain(domain);

                // Notify about the change
                var customization = DomainCustomizationStateManager.GetCustomization(domain);
                PubSub.Publish(new DomainCustomizationChangedEvent(domain, customization.CssEnabled));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove CSS for domain {domain}: {ex.Message}");
        }
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