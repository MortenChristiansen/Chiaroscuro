using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost;

public interface IBrowserContext
{
    public ITabBrowser? CurrentTab { get; }
    ModifierKeys CurrentKeyboardModifiers { get; }

    void InitTabPalette();
    void ShowTabPalette();
    void HideTabPalette();
}
