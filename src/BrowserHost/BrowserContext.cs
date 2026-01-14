using BrowserHost.Features;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Tab;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost;

public class BrowserContext : IBrowserContext
{
    private readonly MainWindow _window;

    public BrowserContext(MainWindow window)
    {
        _window = window;

        DragDropHost = new MainWindowDragDropHost(_window);
        TabsHost = new MainWindowTabsHost(_window);
    }

    public Options AppOptions => App.Options;

    public ITabBrowser? CurrentTab => _window.CurrentTab;
    public IDragDropHost DragDropHost { get; }
    public ITabsHost TabsHost { get; }
    public string? CurrentTabId => _window.CurrentTab?.Id;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public Color WorkspaceColor
    {
        get => _window.WorkspaceColor;
        set => _window.WorkspaceColor = value;
    }

    public TFeature GetFeature<TFeature>() where TFeature : Feature =>
        _window.GetFeature<TFeature>();

    public WindowState WindowState
    {
        get => _window.WindowState;
        set => _window.WindowState = value;
    }

    public void SetClipboardText(string text) => Clipboard.SetText(text);

    public void SetCurrentTab(ITabBrowser? tab)
    {
        if (tab is null)
        {
            _window.SetCurrentTab(null);
            return;
        }

        if (tab is not TabBrowser tabBrowser)
            throw new InvalidOperationException("Cannot set a non-TabBrowser instance as current tab.");

        _window.SetCurrentTab(tabBrowser);
    }

    public ITabBrowser CreateNewTab(string address, TabsBrowserApi tabsApi, global::BrowserHost.Utilities.PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
        CreateExistingTab($"{Guid.NewGuid()}", address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser);

    public ITabBrowser CreateExistingTab(string tabId, string address, TabsBrowserApi tabsApi, global::BrowserHost.Utilities.PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
        new TabBrowser(tabId, address, tabsApi, pubSub, setManualAddress: setManualAddress, favicon: favicon, isChildBrowser: isChildBrowser, GetFeature<SettingsFeature>());

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
