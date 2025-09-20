using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public record ShowNotificationEvent(string TabId, string Title, string Body, string? Icon, string Origin);

public class WindowsNotificationFeature(MainWindow window) : Feature(window)
{
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
                // Try Windows 10/11 toast notifications first
                if (ShowToastNotification(title, body, origin))
                    return;

                // Fallback to system tray balloon notification
                if (ShowBalloonNotification(title, body, origin))
                    return;

                // Last resort fallback
                ShowFallbackNotification(title, body, origin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show notification: {ex.Message}");
                ShowFallbackNotification(title, body, origin);
            }
        });
    }

    private bool ShowToastNotification(string title, string body, string origin)
    {
        try
        {
            // For Windows 10/11 toast notifications, we would typically use:
            // - Windows.UI.Notifications (requires Windows Runtime support)
            // - Microsoft.Toolkit.Win32.UI.Controls (community toolkit)
            // - Or PInvoke to shell32.dll

            // Since this is a WPF app and we want minimal dependencies,
            // we'll use a PowerShell command as a simple approach
            var escapedTitle = title.Replace("'", "''").Replace("`", "``");
            var escapedBody = body.Replace("'", "''").Replace("`", "``");
            var escapedOrigin = origin.Replace("'", "''").Replace("`", "``");

            var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null;
                $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02);
                $textNodes = $template.GetElementsByTagName('text');
                $textNodes.Item(0).AppendChild($template.CreateTextNode('{escapedTitle}')) | Out-Null;
                $textNodes.Item(1).AppendChild($template.CreateTextNode('{escapedBody}')) | Out-Null;
                $toast = [Windows.UI.Notifications.ToastNotification]::new($template);
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Chiaroscuro');
                $notifier.Show($toast);
            ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // Wait max 5 seconds

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Toast notification failed: {ex.Message}");
            return false;
        }
    }

    private bool ShowBalloonNotification(string title, string body, string origin)
    {
        try
        {
            // Use system tray balloon notification as fallback
            // This would require a NotifyIcon, which we would need to add to MainWindow
            // For now, return false to use the simple fallback
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Balloon notification failed: {ex.Message}");
            return false;
        }
    }

    private void ShowFallbackNotification(string title, string body, string origin)
    {
        // Simple fallback that's always visible but non-intrusive
        var message = $"{title}\n\n{body}\n\nFrom: {origin}";

        // Show as a custom notification window that auto-dismisses after a few seconds
        var notificationWindow = new NotificationWindow(title, body, origin);
        notificationWindow.Show();
    }
}

/// <summary>
/// Simple custom notification window for fallback scenarios
/// </summary>
public class NotificationWindow : Window
{
    public NotificationWindow(string title, string body, string origin)
    {
        Title = "Notification";
        Width = 350;
        Height = 120;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;

        // Position in bottom-right corner
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = SystemParameters.WorkArea.Bottom - Height - 20;

        Background = System.Windows.Media.Brushes.DarkBlue;

        var grid = new System.Windows.Controls.Grid();
        grid.Margin = new Thickness(10);

        var titleBlock = new System.Windows.Controls.TextBlock
        {
            Text = title,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };

        var bodyBlock = new System.Windows.Controls.TextBlock
        {
            Text = body,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };

        var originBlock = new System.Windows.Controls.TextBlock
        {
            Text = $"From: {origin}",
            Foreground = System.Windows.Media.Brushes.LightGray,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };

        var stackPanel = new System.Windows.Controls.StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(bodyBlock);
        stackPanel.Children.Add(originBlock);

        grid.Children.Add(stackPanel);
        Content = grid;

        // Auto-close after 5 seconds
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(5);
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();

        // Click to close
        MouseLeftButtonDown += (s, e) => Close();
    }
}