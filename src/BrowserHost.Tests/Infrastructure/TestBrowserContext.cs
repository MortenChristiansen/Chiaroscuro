using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.Tests.Fakes.StateManagers;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext(ITabBrowser? tab = null) : IBrowserContext
{
    public FakeTabPaletteBrowserApi TabPaletteBrowserApi { get; } = new();
    public FakeTabCustomizationBrowserApi TabCustomizationBrowserApi { get; } = new();

    public FakeTabCustomizationStateManager TabCustomizationStateManager { get; } = new();

    public ITabBrowser? CurrentTab { get; private set; } = tab;
    public string? CurrentTabId => CurrentTab?.Id;

    public ModifierKeys CurrentKeyboardModifiers { get; set; }

    public bool ShowTabPaletteCalled { get; private set; }
    public bool HideTabPaletteCalled { get; private set; }

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

        public TestBrowserContextBuilder WithCurrentTab(out FakeTabBrowser tab, Action<FakeTabBrowser>? configureTab = null)
        {
            tab = new FakeTabBrowser();
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
            var feature = new TabPaletteFeature(null!, context, context.TabPaletteBrowserApi);
            feature.Configure();
            return feature;
        }

        public TabCustomizationFeature BuildTabCustomizationFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new TabCustomizationFeature(null!, context, context.TabCustomizationBrowserApi, context.TabCustomizationStateManager);
            feature.Configure();
            return feature;
        }
    }
}