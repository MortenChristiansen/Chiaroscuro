using BrowserHost.Features;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.DragDrop;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserHost;

public interface IBrowserContext
{
    Options AppOptions { get; }

    public ITabBrowser? CurrentTab { get; }
    public IDragDropHost DragDropHost { get; }
    public ITabsHost TabsHost { get; }
    public string? CurrentTabId { get; }
    ModifierKeys CurrentKeyboardModifiers { get; }

    Color WorkspaceColor { get; set; }

    TFeature GetFeature<TFeature>() where TFeature : Feature;

    WindowState WindowState { get; set; }

    void SetClipboardText(string text);

    void SetCurrentTab(ITabBrowser? tab);

    ITabBrowser CreateNewTab(string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser);
    ITabBrowser CreateExistingTab(string tabId, string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser);

    void ToggleActionContextDevTools();
    void ToggleTabPaletteDevTools();

    void ShowTabPalette();
    void HideTabPalette();
    void FocusTabPalette();

    bool ActionRequiresDispatch { get; }
    void Dispatch(Action action);
}
