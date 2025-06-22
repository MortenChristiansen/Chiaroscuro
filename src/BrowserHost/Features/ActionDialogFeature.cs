using CefSharp;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features;

public class ActionDialogFeature(MainWindow window, BrowserApi api)
{
    public void Register()
    {
        window.ActionDialog.Address = ContentServer.GetUiAddress("/action-dialog");
        window.ActionDialog.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"ActionDialog: {e.Message}");
        };
        window.ActionDialog.JavascriptObjectRepository.Register("api", api);
    }

    public bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ShowDialog();
            e.Handled = true;
            return true;
        }
        return false;
    }

    private void ShowDialog()
    {
        if (window.ActionDialog.Visibility == Visibility.Visible)
            return;

        ShowActionDialogControl();
        AddGlassOverlayToCurrentTab();
    }

    private void ShowActionDialogControl()
    {
        window.ActionDialog.Opacity = 0;
        window.ActionDialog.Visibility = Visibility.Visible;
        window.ActionDialog.Focus();
        window.ActionDialog.ExecuteScriptAsync("window.angularApi.showDialog()");

        if (window.ActionDialog.RenderTransform is not ScaleTransform)
        {
            var scale = new ScaleTransform(0, 0, 0.5, 0.5);
            window.ActionDialog.RenderTransform = scale;
            window.ActionDialog.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ((ScaleTransform)window.ActionDialog.RenderTransform).ScaleX = 0;
            ((ScaleTransform)window.ActionDialog.RenderTransform).ScaleY = 0;
        }

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((ScaleTransform)window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        ((ScaleTransform)window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    private void AddGlassOverlayToCurrentTab()
    {
        window.CurrentTab.ExecuteScriptAsync(@"(function() {
            var id = 'chiaroscuro-glass-overlay';
            var overlay = document.getElementById(id);
            if (overlay) {
                overlay.style.transition = 'opacity 0.25s';
                overlay.style.opacity = '0';
                setTimeout(function() {
                    overlay.style.opacity = '1';
                }, 10);
            } else {
                overlay = document.createElement('div');
                overlay.id = id;
                overlay.style.position = 'fixed';
                overlay.style.top = '0';
                overlay.style.left = '0';
                overlay.style.width = '100vw';
                overlay.style.height = '100vh';
                overlay.style.zIndex = '2147483647';
                overlay.style.pointerEvents = 'auto';
                overlay.style.background = 'rgba(150,150,190,0.14)';
                overlay.style.backdropFilter = 'blur(12px)';
                overlay.style.webkitBackdropFilter = 'blur(12px)';
                overlay.style.transition = 'opacity 0.25s';
                overlay.style.opacity = '0';
                document.body.appendChild(overlay);
                setTimeout(function() {
                    overlay.style.opacity = '1';
                }, 10);
            }
        })();");
    }

    public void DismissDialog()
    {
        if (window.ActionDialog.Visibility == Visibility.Hidden)
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
            window.ActionDialog.Visibility = Visibility.Hidden;
        };
        window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (window.ActionDialog.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }

    private void HideGlassOverlayFromCurrentTab()
    {
        window.CurrentTab.ExecuteScriptAsync(@"(function() {
            var overlay = document.getElementById('chiaroscuro-glass-overlay');
            if (overlay) {
                overlay.style.transition = 'opacity 0.25s';
                overlay.style.opacity = '0';
                setTimeout(function() {
                    if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                }, 250);
            }
         })();");
    }
}
