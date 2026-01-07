using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BrowserHost.Features.ActionContext;

public class ActionContextWindowOperations
{
    private MainWindow _window = null!;
    private bool _hidden;
    private bool _initialized;
    private DispatcherTimer? _minWidthTimer;
    private double? _minWidth;

    private event Action? _resizeCompleted;

    protected ActionContextWindowOperations() { }

    public ActionContextWindowOperations(MainWindow window)
    {
        _window = window;
        _window.ActionContextGridSplitter.DragCompleted += (_, __) => _resizeCompleted?.Invoke();
    }

    public virtual void RegisterActionContextResizeCompletedHandler(Action handler) => _resizeCompleted += handler;

    public virtual double ActionContextActualWidth => _window.ActionContextColumn.ActualWidth;

    public virtual void SetActionContextWidth(double width)
    {
        if (width > 0)
            _window.ActionContextColumn.Width = new GridLength(width);
    }

    public virtual void ToggleActionContextVisibility()
    {
        var column = _window.ActionContextColumn;
        var splitterCol = _window.ActionContextSplitterColumn;
        var splitter = _window.ActionContextGridSplitter;
        var browser = _window.ActionContext;

        if (!_initialized)
        {
            _window.InitializeSidePanel(column, splitterCol, 8, isExpanded: true);
            _minWidth = column.MinWidth;
            _initialized = true;
        }

        StopMinWidthTimerFromPreviousAnimation();

        if (!_hidden)
        {
            var duration = TimeSpan.FromMilliseconds(200);
            column.MinWidth = 0;
            _window.CollapseSidePanel(column, splitterCol, browser, splitter, duration);
            _hidden = true;
        }
        else
        {
            var duration = TimeSpan.FromMilliseconds(300);

            if (splitterCol.Width.Value == 0)
                splitterCol.Width = new GridLength(8);

            _window.ExpandSidePanel(column, splitterCol, browser, splitter, duration);

            RestoreMinimumWidthAfterAnimation(column, duration);
            _hidden = false;
        }
    }

    private void RestoreMinimumWidthAfterAnimation(ColumnDefinition column, TimeSpan duration)
    {
        _minWidthTimer = new() { Interval = duration };
        _minWidthTimer.Tick += (_, __) =>
        {
            _minWidthTimer?.Stop();
            column.MinWidth = _minWidth ?? 200;
        };
        _minWidthTimer.Start();
    }

    private void StopMinWidthTimerFromPreviousAnimation()
    {
        _minWidthTimer?.Stop();
        _minWidthTimer = null;
    }
}
