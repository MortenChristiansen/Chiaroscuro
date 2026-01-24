using BrowserHost.XamlUtilities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BrowserHost.Features.Terminal;

public class TerminalWindowOperations
{
    private RowDefinition _terminalRow = null!;
    private RowDefinition _terminalSplitterRow = null!;
    private FrameworkElement _terminalElement = null!;
    private GridSplitter _terminalSplitter = null!;

    private bool _initialized;
    protected bool IsVisible;

    protected TerminalWindowOperations() { }

    public TerminalWindowOperations(MainWindow window)
    {
        _terminalRow = window.TerminalRow;
        _terminalSplitterRow = window.TerminalSplitterRow;
        _terminalElement = window.TerminalBrowserControl;
        _terminalSplitter = window.TerminalGridSplitter;
    }

    public virtual bool IsTerminalVisible => IsVisible;

    public virtual void ShowTerminal()
    {
        if (IsVisible) return;

        if (!_initialized)
        {
            InitializeTerminalPanel();
            _initialized = true;
        }

        IsVisible = true;
        var duration = TimeSpan.FromMilliseconds(200);
        _terminalElement.Visibility = Visibility.Visible;
        _terminalSplitter.Visibility = Visibility.Visible;
        GridAnimationBehavior.SetDuration(_terminalRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalRow, true);
        GridAnimationBehavior.SetDuration(_terminalSplitterRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalSplitterRow, true);
    }

    public virtual void HideTerminal()
    {
        if (!IsVisible) return;

        IsVisible = false;
        var duration = TimeSpan.FromMilliseconds(150);
        GridAnimationBehavior.SetDuration(_terminalRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalRow, false);
        GridAnimationBehavior.SetDuration(_terminalSplitterRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalSplitterRow, false);

        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            _terminalElement.Visibility = Visibility.Collapsed;
            _terminalSplitter.Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }

    private void InitializeTerminalPanel()
    {
        _terminalRow.Height = new GridLength(200);
        GridAnimationBehavior.Initialize(_terminalRow);
        _terminalSplitterRow.Height = new GridLength(6);
        GridAnimationBehavior.Initialize(_terminalSplitterRow);
        GridAnimationBehavior.SetIsExpanded(_terminalRow, false);
        GridAnimationBehavior.SetIsExpanded(_terminalSplitterRow, false);
    }
}
