using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = window.CurrentTab;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public void InitTabPalette() => window.TabPaletteBrowserControl.Init();
    public void ShowTabPalette() => window.ShowTabPalette();
    public void HideTabPalette() => window.HideTabPalette();
}
