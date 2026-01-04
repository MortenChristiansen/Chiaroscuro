using BrowserHost.Features.DragDrop;
using BrowserHost.Tab;
using System;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost;

public interface IBrowserContext
{
    public ITabBrowser? CurrentTab { get; }
    public IDragDropHost DragDropHost { get; }
    public string? CurrentTabId { get; }
    ModifierKeys CurrentKeyboardModifiers { get; }

    WindowState WindowState { get; set; }

    void SetClipboardText(string text);

    void ToggleActionContextDevTools();
    void ToggleTabPaletteDevTools();

    void ShowTabPalette();
    void HideTabPalette();
    void FocusTabPalette();

    bool ActionRequiresDispatch { get; }
    void Dispatch(Action action);
}
