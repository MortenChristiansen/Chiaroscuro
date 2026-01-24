using BrowserHost.Features;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.Folders;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.AppState;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DevTool;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Terminal;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.Tests.Fakes.WindowOperations;
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
        LocalWebAppStateManager = new LocalWebAppStateManager(FileSystem);
        LocalWebAppProcessManager = new FakeLocalWebAppProcessManager(PubSub);
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
    public FakeActionDialogBrowserApi ActionDialogBrowserApi { get; } = new();
    public FakeDownloadsBrowserApi DownloadsBrowserApi { get; } = new();
    public FakeTerminalBrowserApi TerminalBrowserApi { get; } = new();
    public FakeLocalWebAppBrowserApi LocalWebAppBrowserApi { get; } = new();

    public FakeCustomWindowChromeWindowOperations CustomWindowChromeWindowOperations { get; } = new();
    public FakeActionDialogWindowOperations ActionDialogWindowOperations { get; } = new();
    public FakeActionContextWindowOperations ActionContextWindowOperations { get; } = new();
    public FakeTabPaletteWindowOperations TabPaletteWindowOperations { get; } = new();
    public FakeDevToolWindowOperations DevToolWindowOperations { get; } = new();
    public FakeTerminalWindowOperations TerminalWindowOperations { get; } = new();

    public FakeDragDropHost FakeDragDropHost { get; } = new FakeDragDropHost();
    public IDragDropHost DragDropHost => FakeDragDropHost;

    public FakeTabsHost FakeTabsHost { get; } = new FakeTabsHost();
    public ITabsHost TabsHost => FakeTabsHost;

    public TabCustomizationStateManager TabCustomizationStateManager { get; }
    public DomainCustomizationStateManager DomainCustomizationStateManager { get; }
    public SettingsStateManager SettingsStateManager { get; }
    public WorkspaceStateManager WorkspaceStateManager { get; }
    public PinnedTabsStateManager PinnedTabsStateManager { get; }
    public LocalWebAppStateManager LocalWebAppStateManager { get; }
    public FakeLocalWebAppProcessManager LocalWebAppProcessManager { get; }

    public ITabBrowser? CurrentTab { get; private set; }
    public string? CurrentTabId => CurrentTab?.Id;

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

    public void SetCurrentTab(ITabBrowser? tab)
    {
        CurrentTab = tab;
    }

    public Queue<string> NewTabIdsToGenerate { get; } = new();
    public List<TabCreationInvocation> TabCreations { get; } = [];
    public List<FakeTabBrowser> CreatedTabs { get; } = [];

    public ITabBrowser CreateNewTab(string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        var tabId = NewTabIdsToGenerate.Count > 0 ? NewTabIdsToGenerate.Dequeue() : $"{Guid.NewGuid()}";
        return CreateExistingTab(tabId, address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser);
    }

    public ITabBrowser CreateExistingTab(string tabId, string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        TabCreations.Add(new(tabId, address, setManualAddress, favicon, isChildBrowser));
        var tab = new FakeTabBrowser(tabId)
        {
            Address = address,
            Favicon = favicon,
            Title = "",
        };
        CreatedTabs.Add(tab);
        return tab;
    }

    public record TabCreationInvocation(string TabId, string Address, bool SetManualAddress, string? Favicon, bool IsChildBrowser);

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
            BuildFeature((context) => new ZoomFeature(context.PubSub, context));

        public TabPaletteFeature BuildTabPaletteFeature() =>
            BuildFeature((context) => new TabPaletteFeature(context.PubSub, context, context.TabPaletteBrowserApi, context.TabPaletteWindowOperations));

        public TabCustomizationFeature BuildTabCustomizationFeature() =>
            BuildFeature((context) => new TabCustomizationFeature(context.PubSub, context, context.TabCustomizationBrowserApi, context.TabsBrowserApi, context.TabCustomizationStateManager));

        public FindTextFeature BuildFindTextFeature() =>
            BuildFeature((context) => new FindTextFeature(context.PubSub, context, context.FindTextBrowserApi, context.TabPaletteWindowOperations));

        public DomainCustomizationFeature BuildDomainCustomizationFeature() =>
            BuildFeature((context) => new DomainCustomizationFeature(context.PubSub, context, context.DomainCustomizationBrowserApi, context.DomainCustomizationStateManager));

        public SettingsFeature BuildSettingsFeature() =>
            BuildFeature((context) => new SettingsFeature(context.PubSub, context.SettingsStateManager));

        public DevToolFeature BuildDevToolFeature() =>
            BuildFeature((context) => new DevToolFeature(context.PubSub, context, context.DevToolWindowOperations));

        public DragDropFeature BuildDragDropFeature() =>
            BuildFeature((context) => new DragDropFeature(context.PubSub, context, context.FileSystem));

        public CustomWindowChromeFeature BuildCustomWindowChromeFeature() =>
            BuildFeature((context) => new CustomWindowChromeFeature(context.PubSub, context, context.CustomWindowChromeBrowserApi, context.CustomWindowChromeWindowOperations));

        public WorkspacesFeature BuildWorkspacesFeature() =>
            BuildFeature((context) => new WorkspacesFeature(context.PubSub, context, context.WorkspacesBrowserApi, context.TabsBrowserApi, context.WorkspaceStateManager));

        public PinnedTabsFeature BuildPinnedTabsFeature() =>
            BuildFeature((context) => new PinnedTabsFeature(context.PubSub, context, context.TabsBrowserApi, context.PinnedTabsBrowserApi, context.PinnedTabsStateManager));

        public TabsFeature BuildTabsFeature() =>
            BuildFeature((context) => new TabsFeature(context.PubSub, context, context.TabsBrowserApi));

        public ActionContextFeature BuildActionContextFeature() =>
            BuildFeature((context) => new ActionContextFeature(context.PubSub, context, context.ActionContextWindowOperations));

        public ActionDialogFeature BuildActionDialogFeature() =>
            BuildFeature((context) => new ActionDialogFeature(context.PubSub, context, context.ActionDialogBrowserApi, new NavigationHistoryStateManager(context.FileSystem), context.ActionDialogWindowOperations));

        public AppStateFeature BuildAppStateFeature() =>
            BuildFeature((context) => new AppStateFeature(context.PubSub, context.ActionContextWindowOperations, context.TabPaletteWindowOperations, new AppStateStateManager(context.FileSystem)));

        public FoldersFeature BuildFoldersFeature() =>
            BuildFeature((context) => new FoldersFeature(context.PubSub, context, context.TabsBrowserApi));

        public FileDownloadsFeature BuildFileDownloadsFeature() =>
            BuildFeature((context) => new FileDownloadsFeature(context.PubSub, context.DownloadsBrowserApi, context.FileSystem.TimeSystem));

        public TerminalFeature BuildTerminalFeature() =>
            BuildFeature((context) => new TerminalFeature(context.PubSub, context, context.TerminalBrowserApi, context.TerminalWindowOperations));

        public LocalWebAppFeature BuildLocalWebAppFeature() =>
            BuildFeature((context) => new LocalWebAppFeature(context.PubSub, context, context.LocalWebAppBrowserApi, context.LocalWebAppStateManager, context.LocalWebAppProcessManager));
    }
}