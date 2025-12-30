using BrowserHost.Tab;
using System;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public ITabBrowser? CurrentTab => window.CurrentTab;
    public string? CurrentTabId => window.CurrentTab?.Id;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public void ShowTabPalette() => window.ShowTabPalette();
    public void HideTabPalette() => window.HideTabPalette();
    public void FocusTabPalette() => window.TabPaletteBrowserControl.Focus();

    public bool ActionRequiresDispatch
    {
        get
        {
            var dispatcher = Application.Current?.Dispatcher;
            return dispatcher is not null && !dispatcher.CheckAccess();
        }
    }

    public void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Dispatcher is not available. Use ActionRequiresDispatch to verify if dispatching is needed.");
        dispatcher.Invoke(action);
    }
}
