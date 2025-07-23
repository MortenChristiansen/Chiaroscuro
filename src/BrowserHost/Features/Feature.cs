using System.Windows.Input;

namespace BrowserHost.Features;

public abstract class Feature(MainWindow window)
{
    protected MainWindow Window { get; } = window;

    public virtual void Configure() { }
    public virtual void Start() { }

    public virtual bool HandleOnPreviewKeyDown(KeyEventArgs e) => false;
    public virtual bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e) => false;
}
