using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost;

public interface IBrowserContext
{
    public ITabBrowser? CurrentTab { get; }
    public string? CurrentTabId { get; }
    ModifierKeys CurrentKeyboardModifiers { get; }

    void ShowTabPalette();
    void HideTabPalette();
}
