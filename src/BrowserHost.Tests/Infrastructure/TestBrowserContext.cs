using BrowserHost.Features.TabPalette;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext(ITabBrowser? tab = null) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = tab;

    public ModifierKeys CurrentKeyboardModifiers { get; set; }

    public bool InitTabPaletteCalled { get; private set; }
    public bool ShowTabPaletteCalled { get; private set; }
    public bool HideTabPaletteCalled { get; private set; }

    public void InitTabPalette() => InitTabPaletteCalled = true;
    public void ShowTabPalette() => ShowTabPaletteCalled = true;
    public void HideTabPalette() => HideTabPaletteCalled = true;

    public void SetCurrentTab(ITabBrowser? tab)
    {
        CurrentTab = tab;
    }

    public static TestBrowserContextBuilder CreateFeature =>
        new();

    public class TestBrowserContextBuilder
    {
        private ITabBrowser? _tab;
        private Action<TestBrowserContext>? _configureContext;
        private TestBrowserContext? _context;

        public TestBrowserContextBuilder WithCurrentTab(out TestTabBrowser tab, Action<TestTabBrowser>? configureTab = null)
        {
            tab = new TestTabBrowser();
            configureTab?.Invoke(tab);
            _tab = tab;
            return this;
        }

        public TestBrowserContextBuilder WithNoCurrentTab()
        {
            _tab = null;
            return this;
        }

        public TestBrowserContextBuilder ConfigureContext(Action<TestBrowserContext> configure)
        {
            _configureContext = configure;
            return this;
        }

        public TestBrowserContextBuilder CaptureContext(out TestBrowserContext context)
        {
            _context = context = new TestBrowserContext(_tab);
            return this;
        }

        public ZoomFeature BuildZoomFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new ZoomFeature(null!, context);
            feature.Configure();
            return feature;
        }

        public TabPaletteFeature BuildTabPaletteFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new TabPaletteFeature(null!, context);
            feature.Configure();
            return feature;
        }
    }
}