using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System.Windows.Input;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext : IBrowserContext
{
    public TestBrowserContext(ITabBrowser? tab)
    {
        FileSystem = new MockFileSystem();
        CurrentTab = tab;

        PubSub = new PubSub(new DirectPubSubDispatchStrategy());
        PubSubMessages.AttachTo(PubSub);

        TabCustomizationStateManager = new TabCustomizationStateManager(FileSystem);
        DomainCustomizationStateManager = new DomainCustomizationStateManager(FileSystem, new NoopFileOpener());
        SettingsStateManager = new SettingsStateManager(FileSystem);
    }

    public MockFileSystem FileSystem { get; }

    public PubSub PubSub { get; }

    public FakeTabPaletteBrowserApi TabPaletteBrowserApi { get; } = new();
    public FakeFindTextBrowserApi FindTextBrowserApi { get; } = new();
    public FakeTabCustomizationBrowserApi TabCustomizationBrowserApi { get; } = new();
    public FakeTabsBrowserApi TabsBrowserApi { get; } = new();
    public FakeDomainCustomizationBrowserApi DomainCustomizationBrowserApi { get; } = new();

    public TabCustomizationStateManager TabCustomizationStateManager { get; }
    public DomainCustomizationStateManager DomainCustomizationStateManager { get; }
    public SettingsStateManager SettingsStateManager { get; }

    public ITabBrowser? CurrentTab { get; private set; }
    public string? CurrentTabId => CurrentTab?.Id;

    public ModifierKeys CurrentKeyboardModifiers { get; set; }

    public bool ShowTabPaletteCalled { get; private set; }
    public bool HideTabPaletteCalled { get; private set; }
    public bool FocusTabPaletteCalled { get; private set; }

    public void ShowTabPalette() => ShowTabPaletteCalled = true;
    public void HideTabPalette() => HideTabPaletteCalled = true;
    public void FocusTabPalette() => FocusTabPaletteCalled = true;

    public void SetCurrentTab(ITabBrowser? tab)
    {
        CurrentTab = tab;
    }

    public bool ActionRequiresDispatch { get; set; } = false;

    public bool DispatchCalled { get; private set; }

    public void WaitForDispatch()
    {
        WaitUntil(() => DispatchCalled);
    }

    public void WaitUntil(Func<bool> condition)
    {
        Assert.True(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5)));
    }

    public void Dispatch(Action action)
    {
        if (!ActionRequiresDispatch)
            throw new InvalidOperationException("Cannot dispatch");

        DispatchCalled = true;
        action();
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

        public TestBrowserContextBuilder WithCurrentDomainTab(out TabBrowser tab, string address, string? tabId = null)
        {
            tab = TypeConstructor.CreateTabBrowser(tabId);
            tab.SetTabAddress(address);
            _tab = tab;
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
            var feature = new ZoomFeature(null!, context.PubSub, context);
            feature.Configure();
            return feature;
        }

        public TabPaletteFeature BuildTabPaletteFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new TabPaletteFeature(null!, context.PubSub, context, context.TabPaletteBrowserApi);
            feature.Configure();
            return feature;
        }

        public TabCustomizationFeature BuildTabCustomizationFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new TabCustomizationFeature(null!, context.PubSub, context, context.TabCustomizationBrowserApi, context.TabsBrowserApi, context.TabCustomizationStateManager);
            feature.Configure();
            return feature;
        }

        public FindTextFeature BuildFindTextFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new FindTextFeature(null!, context.PubSub, context, context.FindTextBrowserApi);
            feature.Configure();
            return feature;
        }

        public DomainCustomizationFeature BuildDomainCustomizationFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new DomainCustomizationFeature(null!, context.PubSub, context, context.DomainCustomizationBrowserApi, context.DomainCustomizationStateManager);
            feature.Configure();
            return feature;
        }

        public SettingsFeature BuildSettingsFeature()
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = new SettingsFeature(null!, context.PubSub, context.SettingsStateManager);
            feature.Configure();
            return feature;
        }
    }
}