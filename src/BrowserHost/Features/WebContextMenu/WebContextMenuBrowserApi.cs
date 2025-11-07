using BrowserHost.CefInfrastructure;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuBrowserApi : BrowserApi
{
    public void DismissContextMenu() =>
        Application.Current.Dispatcher.Invoke(() =>
            Application.Current.Windows.OfType<WebContextMenuWindow>().Where(w => w.IsActive).FirstOrDefault()?.Close()
        );
}
