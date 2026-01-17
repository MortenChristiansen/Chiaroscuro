using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BrowserHost.XamlUtilities;

namespace BrowserHost.Features.Terminal;

public interface ITerminalWindowOperations
{
    void ShowTerminal();
    void HideTerminal();
    bool IsTerminalVisible { get; }
}

public class TerminalWindowOperations : ITerminalWindowOperations
{
    private readonly MainWindow _mainWindow;
    private readonly RowDefinition _terminalRow;
    private readonly RowDefinition _terminalSplitterRow;
    private readonly FrameworkElement _terminalElement;
    private readonly GridSplitter _terminalSplitter;
    private bool _initialized;
    private bool _isVisible;

    public bool IsTerminalVisible => _isVisible;

    public TerminalWindowOperations(
        MainWindow mainWindow,
        RowDefinition terminalRow,
        RowDefinition terminalSplitterRow,
        FrameworkElement terminalElement,
        GridSplitter terminalSplitter)
    {
        _mainWindow = mainWindow;
        _terminalRow = terminalRow;
        _terminalSplitterRow = terminalSplitterRow;
        _terminalElement = terminalElement;
        _terminalSplitter = terminalSplitter;
    }

    public void ShowTerminal()
    {
        if (_isVisible) return;

        if (!_initialized)
        {
            InitializeTerminalPanel();
            _initialized = true;
        }

        _isVisible = true;
        var duration = TimeSpan.FromMilliseconds(200);
        _terminalElement.Visibility = Visibility.Visible;
        _terminalSplitter.Visibility = Visibility.Visible;
        GridAnimationBehavior.SetDuration(_terminalRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalRow, true);
        GridAnimationBehavior.SetDuration(_terminalSplitterRow, duration);
        GridAnimationBehavior.SetIsExpanded(_terminalSplitterRow, true);
    }

    public void HideTerminal()
    {
        if (!_isVisible) return;

        _isVisible = false;
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
