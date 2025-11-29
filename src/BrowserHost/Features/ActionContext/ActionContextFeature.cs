using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BrowserHost.Features.ActionContext;

public class ActionContextFeature(MainWindow window) : Feature(window)
{
    private bool _isHidden;
    private bool _initialized;
    private DispatcherTimer? _animationTimer;
    private double? _minWidth;

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ToggleActionContextHidden();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void ToggleActionContextHidden()
    {
        var column = Window.ActionContextColumn;
        var splitterCol = Window.ActionContextSplitterColumn;
        var splitter = Window.ActionContextGridSplitter;
        var browser = Window.ActionContext;

        if (!_initialized)
        {
            Window.InitializeSidePanel(column, splitterCol, 8, isExpanded: true);
            _minWidth = column.MinWidth;
            _initialized = true;
        }

        StopTimerFromPreviousAnimation();

        if (!_isHidden)
        {
            var duration = TimeSpan.FromMilliseconds(200);
            // Allow the column to animate down to zero (overrides XAML MinWidth=200 during collapse)
            column.MinWidth = 0;
            Window.CollapseSidePanel(column, splitterCol, browser, splitter, duration);
            _isHidden = true;
        }
        else
        {
            var duration = TimeSpan.FromMilliseconds(300);

            // Ensure the splitter column has its expanded width baseline (8) before expanding
            if (splitterCol.Width.Value == 0)
                splitterCol.Width = new GridLength(8);

            Window.ExpandSidePanel(column, splitterCol, browser, splitter, duration);

            RestoreMinimumWidthAfterAnimation(column, duration);
            _isHidden = false;
        }
    }

    private void RestoreMinimumWidthAfterAnimation(System.Windows.Controls.ColumnDefinition column, TimeSpan duration)
    {
        _animationTimer = new() { Interval = duration };
        _animationTimer.Tick += (s, e) =>
        {
            _animationTimer?.Stop();
            column.MinWidth = _minWidth ?? 200;
        };
        _animationTimer.Start();
    }

    private void StopTimerFromPreviousAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer = null;
    }
}
