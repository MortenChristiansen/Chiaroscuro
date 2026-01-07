using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Windows;

namespace BrowserHost.Features.CustomWindowChrome;

public partial class CustomWindowChromeFeature(
    PubSub pubSub,
    IBrowserContext browserContext,
    CustomWindowChromeBrowserApi customWindowChromeApi) : Feature(pubSub)
{
    private static readonly HashSet<string> _googleAdTrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "gclid",
        "gclsrc",
        "gbraid",
        "wbraid",
        "gad_source",
        "gad_campaignid",
        "srsltid"
    };

    public override void Configure()
    {
        PubSub.Handle<MinimizeWindowCommand>(_ =>
        {
            Minimize();
            PubSub.Publish(new WindowMinimizedEvent());
        });
        PubSub.Handle<ToggleWindowStateCommand>(_ =>
        {
            ToggleMaximizedState();
            PubSub.Publish(new WindowStateToggledEvent());
        });
        PubSub.Handle<CopyAddressCommand>(_ =>
        {
            var address = browserContext.CurrentTab?.Address;
            if (string.IsNullOrEmpty(address))
                return;

            var sanitized = RemoveGoogleAdTrackingParameters(address);
            browserContext.SetClipboardText(sanitized);

            PubSub.Publish(new AddressCopiedEvent());
        });

        PubSub.Subscribe<TabLoadingStateChangedEvent>(OnTabLoadingStateChanged);
        PubSub.Subscribe<TabActivatedEvent>(OnTabActivated);

        browserContext.EnableCustomWindowChromeIntegration(customWindowChromeApi);
    }

    private static string RemoveGoogleAdTrackingParameters(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return address;

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Query))
            return address;

        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
            return address;

        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return address;

        var filtered = new List<string>(parts.Length);
        var removed = false;

        foreach (var part in parts)
        {
            var key = part;
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex >= 0)
                key = part[..equalsIndex];

            var decodedKey = Uri.UnescapeDataString(key);
            if (_googleAdTrackingParameters.Contains(decodedKey))
            {
                removed = true;
                continue;
            }

            filtered.Add(part);
        }

        if (!removed)
            return address;

        var builder = new UriBuilder(uri)
        {
            Query = filtered.Count > 0 ? string.Join("&", filtered) : string.Empty
        };
        return builder.Uri.ToString();
    }

    private void OnTabLoadingStateChanged(TabLoadingStateChangedEvent e)
    {
        if (browserContext.CurrentTab?.Id == e.TabId)
            customWindowChromeApi.UpdateLoadingState(e.IsLoading);
    }

    private void OnTabActivated(TabActivatedEvent e)
    {
        var isLoading = e.PreviousTab?.IsLoading ?? false;
        customWindowChromeApi.UpdateLoadingState(isLoading);
    }

    private void ToggleMaximizedState()
    {
        if (browserContext.WindowState == WindowState.Maximized)
            browserContext.WindowState = WindowState.Normal;
        else
            browserContext.WindowState = WindowState.Maximized;
    }

    private void Minimize() => browserContext.WindowState = WindowState.Minimized;
}
