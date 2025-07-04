using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogFeature(MainWindow window) : Feature<ActionDialogBrowserApi>(window, window.ActionDialog.Api)
{
    public override void Register()
    {
        PubSub.Subscribe<ActionDialogDismissedEvent>(_ => DismissDialog());
        PubSub.Subscribe<NavigationStartedEvent>(HandleNavigationStarted);
        PubSub.Subscribe<ActionDialogValueChangedEvent>(HandleValueChanged);
        PubSub.Subscribe<TabsChangedEvent>(HandleTabsChanged);
    }

    private void HandleNavigationStarted(NavigationStartedEvent e)
    {
        // For now, save the address with basic info
        // The title and favicon will be updated when the page loads
        NavigationHistoryStateManager.SaveNavigationEntry(e.Address, null, null);
    }

    private void HandleTabsChanged(TabsChangedEvent e)
    {
        // Update navigation history with current tab information
        var currentTab = e.Tabs.FirstOrDefault(t => t.IsActive);
        if (currentTab != null)
        {
            var tabFeature = Window.GetFeature<TabsFeature>();
            var tabBrowser = tabFeature.GetTabById(currentTab.Id);
            if (tabBrowser != null && !string.IsNullOrEmpty(tabBrowser.ManualAddress))
            {
                NavigationHistoryStateManager.SaveNavigationEntry(tabBrowser.ManualAddress, currentTab.Title, currentTab.Favicon);
            }
        }
    }

    private void HandleValueChanged(ActionDialogValueChangedEvent e)
    {
        // Get suggestions based on the current input
        var suggestions = NavigationHistoryStateManager.GetSuggestions(e.Value);

        // Send suggestions to frontend
        Window.ActionDialog.UpdateSuggestions(suggestions);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ShowDialog();
            return true;
        }
        return false;
    }

    private void ShowDialog()
    {
        if (Window.ActionDialog.Visibility == Visibility.Visible)
            return;

        ShowActionDialogControl();
        AddGlassOverlayToCurrentTab();
    }

    private void ShowActionDialogControl()
    {
        Window.ActionDialog.Opacity = 0;
        Window.ActionDialog.Visibility = Visibility.Visible;
        Window.ActionDialog.Focus();
        Window.ActionDialog.CallClientApi("showDialog");

        if (Window.ActionDialog.RenderTransform is not ScaleTransform)
        {
            var scale = new ScaleTransform(0, 0, 0.5, 0.5);
            Window.ActionDialog.RenderTransform = scale;
            Window.ActionDialog.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ((ScaleTransform)Window.ActionDialog.RenderTransform).ScaleX = 0;
            ((ScaleTransform)Window.ActionDialog.RenderTransform).ScaleY = 0;
        }

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        Window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((ScaleTransform)Window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        ((ScaleTransform)Window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    private void AddGlassOverlayToCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }

    private void DismissDialog()
    {
        if (Window.ActionDialog.Visibility == Visibility.Hidden)
            return;

        HideActionDialogControl();
        HideGlassOverlayFromCurrentTab();
    }

    private void HideActionDialogControl()
    {
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
        fadeOut.Completed += (s, e) =>
        {
            Window.ActionDialog.Visibility = Visibility.Hidden;
        };
        Window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (Window.ActionDialog.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }

    private void HideGlassOverlayFromCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }
}
