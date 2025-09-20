using BrowserHost.Features.Notifications;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using CefSharp;
using System.Threading.Tasks;

namespace BrowserHost.Features.ActionContext.Tabs;

/// <summary>
/// Browser API for handling JavaScript notification requests
/// </summary>
public class NotificationBrowserApi : BrowserApi
{
    private readonly string _tabId;

    public NotificationBrowserApi(string tabId)
    {
        _tabId = tabId;
    }

    public void ShowNotification(string title, string body, string? icon, string origin)
    {
        PubSub.Publish(new ShowNotificationEvent(_tabId, title, body, icon, origin));
    }

    public Task<int> GetCurrentPermissionStatus()
    {
        var customization = TabCustomizationFeature.GetCustomizationsForTab(_tabId);
        return Task.FromResult((int)(customization.NotificationPermission ?? NotificationPermissionStatus.NotAsked));
    }
}

/// <summary>
/// Injects JavaScript to override the Notification API and route it through our system
/// </summary>
public static class NotificationApiInjector
{
    public static void InjectNotificationApi(IWebBrowser browser, string tabId)
    {
        var script = $@"
(function() {{
    if (window.chiaroscuroNotificationApiInjected) return;
    window.chiaroscuroNotificationApiInjected = true;

    // Store original Notification constructor
    const OriginalNotification = window.Notification;
    
    // Override Notification constructor
    window.Notification = function(title, options) {{
        options = options || {{}};
        
        // Get current origin
        const origin = window.location.origin;
        
        // Create a fake notification object that mimics the real API
        const fakeNotification = {{
            title: title,
            body: options.body || '',
            icon: options.icon || null,
            tag: options.tag || null,
            data: options.data || null,
            silent: options.silent || false,
            requireInteraction: options.requireInteraction || false,
            onclick: null,
            onshow: null,
            onerror: null,
            onclose: null,
            close: function() {{
                if (this.onclose) this.onclose();
            }},
            addEventListener: function(type, listener) {{
                this['on' + type] = listener;
            }},
            removeEventListener: function(type, listener) {{
                this['on' + type] = null;
            }}
        }};
        
        // Send notification to backend
        if (window.notificationApi) {{
            window.notificationApi.showNotification(title, options.body || '', options.icon || null, origin);
            
            // Simulate show event
            setTimeout(() => {{
                if (fakeNotification.onshow) fakeNotification.onshow();
            }}, 10);
        }}
        
        return fakeNotification;
    }};
    
    // Copy static properties and methods
    window.Notification.permission = 'default';
    window.Notification.requestPermission = function() {{
        return new Promise((resolve) => {{
            // Permission is handled by our PermissionHandler
            resolve(window.Notification.permission);
        }});
    }};
    
    // Update permission status when it changes
    window.updateNotificationPermission = function(status) {{
        switch(status) {{
            case 1: // Granted
                window.Notification.permission = 'granted';
                break;
            case 2: // Denied
                window.Notification.permission = 'denied';
                break;
            default: // Not asked
                window.Notification.permission = 'default';
                break;
        }}
    }};
    
    // Initialize permission status from backend
    setTimeout(() => {{
        if (window.notificationApi && window.notificationApi.getCurrentPermissionStatus) {{
            window.notificationApi.getCurrentPermissionStatus().then(status => {{
                window.updateNotificationPermission(status);
            }});
        }}
    }}, 100);
}})();
";

        browser.ExecuteScriptAsync(script);
    }

    public static void UpdatePermissionStatus(IWebBrowser browser, NotificationPermissionStatus status)
    {
        browser.ExecuteScriptAsync($"if (window.updateNotificationPermission) window.updateNotificationPermission({(int)status});");
    }
}