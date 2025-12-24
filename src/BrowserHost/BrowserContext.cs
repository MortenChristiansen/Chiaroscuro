using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = window.CurrentTab;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;
}
