using CefSharp;
using CefSharp.Wpf;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost;

public partial class MainWindow : Window
{
    private BrowserApi _browserApi;

    public MainWindow()
    {
        InitializeComponent();

        // Not sure if this does anything
        WebContent.BrowserSettings.WindowlessFrameRate = 120;
        WebContent.BrowserSettings.WebGl = CefState.Enabled;
        ChromeUI.BrowserSettings.WindowlessFrameRate = 120;
        ChromeUI.BrowserSettings.WebGl = CefState.Enabled;

        _browserApi = new BrowserApi(this);
        ChromeUI.JavascriptObjectRepository.Register("api", _browserApi);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        // Improves issue with rendering stopping on resize
        RootGrid.Children.OfType<ChromiumWebBrowser>()
            .ToList()
            .ForEach(browser => browser.GetBrowserHost()?.Invalidate(PaintElementType.View));
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _browserApi.ShowActionDialog();
            e.Handled = true;
        }

        if (e.Key == Key.F5)
        {
            var ignoreCache = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            WebContent.Reload(ignoreCache);
        }
    }

    public ChromiumWebBrowser Chrome => ChromeUI;
    public ChromiumWebBrowser CurrentTab => WebContent;
}
