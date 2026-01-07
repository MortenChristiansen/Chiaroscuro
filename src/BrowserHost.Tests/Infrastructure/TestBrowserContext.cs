using BrowserHost.Features;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DevTool;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Testably.Abstractions.Testing;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext : IBrowserContext
{
    private readonly Dictionary<Type, Feature> _features = [];

    public TestBrowserContext(ITabBrowser? tab)
    {
        FileSystem = new MockFileSystem();
        CurrentTab = tab;

        var dispatchStrategy = new DirectPubSubDispatchStrategy();
        PubSub = new PubSub(dispatchStrategy);
        PubSubMessages.AttachTo(PubSub);

        dispatchStrategy.OnDispatched = () =>
        {
            if (ActionRequiresDispatch)
                DispatchCalled = true;
        };

        TabCustomizationStateManager = new TabCustomizationStateManager(FileSystem);
        DomainCustomizationStateManager = new DomainCustomizationStateManager(FileSystem, new NoopFileOpener());
        SettingsStateManager = new SettingsStateManager(FileSystem);
        WorkspaceStateManager = new WorkspaceStateManager(PubSub, FileSystem);
        PinnedTabsStateManager = new PinnedTabsStateManager(FileSystem);
    }

    public Options AppOptions { get; set; } = new();

    public MockFileSystem FileSystem { get; }

    public PubSub PubSub { get; }

    public FakeTabPaletteBrowserApi TabPaletteBrowserApi { get; } = new();
    public FakeFindTextBrowserApi FindTextBrowserApi { get; } = new();
    public FakeTabCustomizationBrowserApi TabCustomizationBrowserApi { get; } = new();
    public FakeTabsBrowserApi TabsBrowserApi { get; } = new();
    public FakeDomainCustomizationBrowserApi DomainCustomizationBrowserApi { get; } = new();
    public FakeCustomWindowChromeBrowserApi CustomWindowChromeBrowserApi { get; } = new();
    public FakeWorkspacesBrowserApi WorkspacesBrowserApi { get; } = new();
    public FakePinnedTabsBrowserApi PinnedTabsBrowserApi { get; } = new();

    public FakeDragDropHost FakeDragDropHost { get; } = new FakeDragDropHost();
    public IDragDropHost DragDropHost => FakeDragDropHost;

    public TabCustomizationStateManager TabCustomizationStateManager { get; }
    public DomainCustomizationStateManager DomainCustomizationStateManager { get; }
    public SettingsStateManager SettingsStateManager { get; }
    public WorkspaceStateManager WorkspaceStateManager { get; }
    public PinnedTabsStateManager PinnedTabsStateManager { get; }

    public ITabBrowser? CurrentTab { get; private set; }
    public string? CurrentTabId => CurrentTab?.Id;

    public bool ToggleActionContextDevToolsCalled { get; private set; }
    public bool ToggleTabPaletteDevToolsCalled { get; private set; }

    public void ToggleActionContextDevTools() => ToggleActionContextDevToolsCalled = true;
    public void ToggleTabPaletteDevTools() => ToggleTabPaletteDevToolsCalled = true;

    public ModifierKeys CurrentKeyboardModifiers { get; set; }

    public Color WorkspaceColor { get; set; } = Color.FromArgb(0, 0, 0, 0);

    public TFeature GetFeature<TFeature>() where TFeature : Feature
    {
        if (_features.TryGetValue(typeof(TFeature), out var feature))
            return (TFeature)feature;

        throw new InvalidOperationException($"Feature of type {typeof(TFeature).Name} not found. Make sure to build the feature before accessing it.");
    }

    private void RegisterFeature<TFeature>(TFeature feature) where TFeature : Feature =>
        _features[typeof(TFeature)] = feature;

    public WindowState WindowState { get; set; } = WindowState.Normal;

    public string? ClipboardText { get; private set; }

    public void SetClipboardText(string text) => ClipboardText = text;

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

    public void ResetDispatchCalled() => DispatchCalled = false;

    public void WaitForDispatch() =>
        WaitUntil(() => DispatchCalled);

    public void WaitUntil(Func<bool> condition) =>
        Assert.True(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5)));

    public void Dispatch(Action action)
    {
        if (!ActionRequiresDispatch)
            throw new InvalidOperationException("Cannot dispatch");

        DispatchCalled = true;
        action();
    }

    #region Application state setup helpers

    public void PinTabs(params string[] tabIds)
    {
        foreach (var tabId in tabIds)
            PubSub.Send(new PinTabCommand(tabId));
    }

    #endregion

    public static TestBrowserContextBuilder CreateFeature =>
        new();

    public class TestBrowserContextBuilder
    {
        private ITabBrowser? _tab;
        private Action<TestBrowserContext>? _configureContext;
        private TestBrowserContext? _context;

        public TestBrowserContextBuilder WithCurrentTab(string? tabId = null)
        {
            return WithCurrentTab(out _, tabId);
        }

        public TestBrowserContextBuilder WithCurrentTab(out FakeTabBrowser tab, string? tabId = null, string? address = null, Action<FakeTabBrowser>? configureTab = null)
        {
            tab = new FakeTabBrowser(tabId);
            configureTab?.Invoke(tab);
            tab.Address = address ?? "";
            _tab = tab;
            return this;
        }

        public TestBrowserContextBuilder WithCurrentTab(out FakeTabBrowser tab, Action<FakeTabBrowser> configureTab)
        {
            tab = new FakeTabBrowser();
            configureTab(tab);
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

        /// <summary>
        /// Use any of the BuildXXXFeature methods to create and register required features.
        /// </summary>
        public TestBrowserContextBuilder IncludeRequiredFeature<TFeature>(Func<TestBrowserContextBuilder, TFeature> builder) where TFeature : Feature
        {
            _context ??= new TestBrowserContext(_tab);
            _context.RegisterFeature(builder(this));
            return this;
        }

        /// <summary>
        /// Use any of the BuildXXXFeature methods to create and register required features.
        /// </summary>
        public TestBrowserContextBuilder IncludeRequiredFeature<TFeature>(Func<TestBrowserContextBuilder, TFeature> builder, out TFeature feature) where TFeature : Feature
        {
            _context ??= new TestBrowserContext(_tab);
            feature = builder(this);
            _context.RegisterFeature(feature);
            return this;
        }

        private TFeature BuildFeature<TFeature>(Func<TestBrowserContext, TFeature> createFeature) where TFeature : Feature
        {
            var context = _context ?? new TestBrowserContext(_tab);
            _configureContext?.Invoke(context);
            var feature = createFeature(context);
            context.RegisterFeature(feature);
            feature.Configure();
            return feature;
        }

        public ZoomFeature BuildZoomFeature() =>
            BuildFeature((context) => new ZoomFeature(null!, context.PubSub, context));

        public TabPaletteFeature BuildTabPaletteFeature() =>
            BuildFeature((context) => new TabPaletteFeature(null!, context.PubSub, context, context.TabPaletteBrowserApi));

        public TabCustomizationFeature BuildTabCustomizationFeature() =>
            BuildFeature((context) => new TabCustomizationFeature(null!, context.PubSub, context, context.TabCustomizationBrowserApi, context.TabsBrowserApi, context.TabCustomizationStateManager));

        public FindTextFeature BuildFindTextFeature() =>
            BuildFeature((context) => new FindTextFeature(null!, context.PubSub, context, context.FindTextBrowserApi));

        public DomainCustomizationFeature BuildDomainCustomizationFeature() =>
            BuildFeature((context) => new DomainCustomizationFeature(null!, context.PubSub, context, context.DomainCustomizationBrowserApi, context.DomainCustomizationStateManager));

        public SettingsFeature BuildSettingsFeature() =>
            BuildFeature((context) => new SettingsFeature(null!, context.PubSub, context.SettingsStateManager));

        public DevToolFeature BuildDevToolFeature() =>
            BuildFeature((context) => new DevToolFeature(null!, context.PubSub, context));

        public DragDropFeature BuildDragDropFeature() =>
            BuildFeature((context) => new DragDropFeature(null!, context.PubSub, context, context.FileSystem));

        public CustomWindowChromeFeature BuildCustomWindowChromeFeature() =>
            BuildFeature((context) => new CustomWindowChromeFeature(null!, context.PubSub, context, context.CustomWindowChromeBrowserApi, enableWindowIntegration: false));

        public WorkspacesFeature BuildWorkspacesFeature() =>
            BuildFeature((context) => new WorkspacesFeature(null!, context.PubSub, context, context.WorkspacesBrowserApi, context.TabsBrowserApi, context.WorkspaceStateManager));

        public PinnedTabsFeature BuildPinnedTabsFeature() =>
            BuildFeature((context) => new PinnedTabsFeature(null!, context, context.PubSub, context.TabsBrowserApi, context.PinnedTabsBrowserApi, context.PinnedTabsStateManager));
    }
}