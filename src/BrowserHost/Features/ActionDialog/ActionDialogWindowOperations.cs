using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogWindowOperations
{
    private MainWindow _window = null!;

    protected ActionDialogWindowOperations() { }

    public ActionDialogWindowOperations(MainWindow window)
    {
        _window = window;
    }

    public virtual bool ActionDialogIsVisible => _window.ActionDialog.Visibility == Visibility.Visible;

    public virtual void ShowActionDialog()
    {
        if (_window.ActionDialog.Visibility == Visibility.Visible)
            return;

        _window.ActionDialog.Opacity = 0;
        _window.ActionDialog.Visibility = Visibility.Visible;
        _window.ActionDialog.Focus();

        if (_window.ActionDialog.RenderTransform is not ScaleTransform)
        {
            var scale = new ScaleTransform(0, 0, 0.5, 0.5);
            _window.ActionDialog.RenderTransform = scale;
            _window.ActionDialog.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ((ScaleTransform)_window.ActionDialog.RenderTransform).ScaleX = 0;
            ((ScaleTransform)_window.ActionDialog.RenderTransform).ScaleY = 0;
        }

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((ScaleTransform)_window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        ((ScaleTransform)_window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    public virtual void HideActionDialog()
    {
        if (_window.ActionDialog.Visibility == Visibility.Hidden)
            return;

        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, __) =>
        {
            _window.ActionDialog.Visibility = Visibility.Hidden;
        };
        _window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (_window.ActionDialog.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }
}
