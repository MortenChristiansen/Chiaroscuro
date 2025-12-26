using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public ITabBrowser? CurrentTab => window.CurrentTab;
    public string? CurrentTabId => window.CurrentTab?.Id;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public void ShowTabPalette() => window.ShowTabPalette();
    public void HideTabPalette() => window.HideTabPalette();
}
