using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext(ITabBrowser? tab = null) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = tab;

    public ModifierKeys CurrentKeyboardModifiers { get; set; }

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

        public ZoomFeature BuildZoomFeature()
        {
            var context = new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            return new ZoomFeature(null!, context);
        }
    }
}