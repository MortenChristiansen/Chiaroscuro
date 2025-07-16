using BrowserHost.CefInfrastructure;
using System.Windows.Input;

namespace BrowserHost.Features;

public abstract class Feature<TApi>(MainWindow window, TApi api) : Feature(window) where TApi : BrowserApi
{
    public TApi Api { get; } = api;
}

public abstract class Feature(MainWindow window)
{
    protected MainWindow Window { get; } = window;

    public abstract void Register();

    public virtual bool HandleOnPreviewKeyDown(KeyEventArgs e) => false;
    public virtual bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e) => false;
}
