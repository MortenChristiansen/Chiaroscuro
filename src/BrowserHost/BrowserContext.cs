using BrowserHost.Features.DragDrop;
using BrowserHost.Features;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public Options AppOptions => App.Options;

    public ITabBrowser? CurrentTab => window.CurrentTab;
    public IDragDropHost DragDropHost { get; } = new MainWindowDragDropHost(window);
    public string? CurrentTabId => window.CurrentTab?.Id;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public Color WorkspaceColor
    {
        get => window.WorkspaceColor;
        set => window.WorkspaceColor = value;
    }

    public TFeature GetFeature<TFeature>() where TFeature : Feature =>
        window.GetFeature<TFeature>();

    public WindowState WindowState
    {
        get => window.WindowState;
        set => window.WindowState = value;
    }

    public void SetClipboardText(string text) => Clipboard.SetText(text);

    public void ToggleActionContextDevTools() => ToggleDevTools(window.ActionContext.GetBrowserHost());
    public void ToggleTabPaletteDevTools() => ToggleDevTools(window.TabPaletteBrowserControl.GetBrowserHost());

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

    private static void ToggleDevTools(IBrowserHost? browserHost)
    {
        if (browserHost != null)
        {
            if (browserHost.HasDevTools)
                browserHost.CloseDevTools();
            else
                browserHost.ShowDevTools();
        }
    }
}
