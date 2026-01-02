using System.Windows.Input;
using BrowserHost.Utilities;

namespace BrowserHost.Features;

public abstract class Feature(MainWindow window, PubSub pubSub)
{
    protected MainWindow Window { get; } = window;
    protected PubSub PubSub { get; } = pubSub;

    public virtual void Configure() { }
    public virtual void Start() { }

    public virtual bool HandleOnPreviewKeyDown(KeyEventArgs e) => false;
    public virtual bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e) => false;
}
