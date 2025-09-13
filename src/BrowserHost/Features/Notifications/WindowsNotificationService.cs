using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public record ShowNotificationEvent(string TabId, string Title, string Body, string? Icon, string Origin);

public class WindowsNotificationService : Feature
{
    public WindowsNotificationService(MainWindow window) : base(window)
    {
    }

    public override void Configure()
    {
        PubSub.Subscribe<ShowNotificationEvent>(HandleShowNotification);
    }

    private void HandleShowNotification(ShowNotificationEvent e)
    {
        var customization = TabCustomizationFeature.GetCustomizationsForTab(e.TabId);
        
        // Only show notification if permission is granted
        if (customization.NotificationPermission != NotificationPermissionStatus.Granted)
            return;

        try
        {
            ShowWindowsNotification(e.Title, e.Body, e.Icon, e.Origin);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show Windows notification: {ex.Message}");
        }
    }

    private void ShowWindowsNotification(string title, string body, string? icon, string origin)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                // Use Windows 10/11 toast notifications
                var toastXml = $@"
                    <toast>
                        <visual>
                            <binding template='ToastGeneric'>
                                <text>{System.Security.SecurityElement.Escape(title)}</text>
                                <text>{System.Security.SecurityElement.Escape(body)}</text>
                                <text placement='attribution'>{System.Security.SecurityElement.Escape(origin)}</text>
                            </binding>
                        </visual>
                    </toast>";

                // For Windows 10/11, we would typically use Windows.UI.Notifications.ToastNotificationManager
                // However, since this is a WPF app, we'll use a simpler approach with MessageBox or system tray
                // For a full implementation, you would need to add Windows Runtime support

                // Fallback: Show a simple notification using system tray or MessageBox
                ShowFallbackNotification(title, body, origin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show toast notification: {ex.Message}");
                ShowFallbackNotification(title, body, origin);
            }
        });
    }

    private void ShowFallbackNotification(string title, string body, string origin)
    {
        // Simple fallback using MessageBox for demonstration
        // In a real implementation, you might want to use a custom notification window
        // or integrate with Windows notification system properly
        var message = $"{title}\n\n{body}\n\nFrom: {origin}";
        MessageBox.Show(message, "Website Notification", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}