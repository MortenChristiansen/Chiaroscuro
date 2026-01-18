using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.AppState;
using BrowserHost.Utilities;
using System;
using System.Threading.Tasks;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

public class LocalWebAppFeature : Feature
{
    private readonly IBrowserContext _browserContext;
    private readonly LocalWebAppBrowserApi _browserApi;
    private readonly LocalWebAppStateManager _stateManager;
    private readonly LocalWebAppProcessManager _processManager;

    public LocalWebAppFeature(
        PubSub pubSub,
        IBrowserContext browserContext,
        LocalWebAppBrowserApi browserApi,
        LocalWebAppStateManager stateManager,
        LocalWebAppProcessManager processManager) : base(pubSub)
    {
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
        _browserApi = browserApi ?? throw new ArgumentNullException(nameof(browserApi));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    }

    public override void Configure()
    {
        // Config commands
        PubSub.Handle<SaveLocalWebAppConfigCommand>(cmd =>
        {
            var config = new LocalWebAppConfigV1(cmd.TabId, cmd.DirectoryPath, cmd.StartCommand);
            _stateManager.SaveConfig(config);
            PubSub.Publish(new LocalWebAppConfigSavedEvent(cmd.TabId, config));
            _browserApi.UpdateConfig(config.TabId, config.DirectoryPath, config.StartCommand);

            // Start process immediately if this is the current tab and has a start command
            if (_browserContext.CurrentTab?.Id == cmd.TabId && !string.IsNullOrWhiteSpace(cmd.DirectoryPath))
            {
                _processManager.StartProcess(cmd.TabId, config);
            }
        });

        PubSub.Handle<DeleteLocalWebAppConfigCommand>(cmd =>
        {
            _processManager.StopProcess(cmd.TabId);
            _stateManager.DeleteConfig(cmd.TabId);
            PubSub.Publish(new LocalWebAppConfigDeletedEvent(cmd.TabId));
            _browserApi.ClearConfig(cmd.TabId);
        });

        PubSub.Handle<BrowseLocalWebAppDirectoryCommand>(cmd =>
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select project directory"
            };

            var config = _stateManager.GetConfig(cmd.TabId);
            if (!string.IsNullOrEmpty(config?.DirectoryPath))
            {
                dialog.InitialDirectory = config.DirectoryPath;
            }

            if (dialog.ShowDialog() == true)
            {
                _browserApi.DirectorySelected(cmd.TabId, dialog.FolderName);
            }
        });

        // Process lifecycle
        PubSub.Subscribe<TabActivatedEvent>(e => OnTabActivated(e.TabId));
        PubSub.Subscribe<TabClosedEvent>(e => OnTabClosed(e.TabId));

        PubSub.Subscribe<LocalWebAppProcessStartedEvent>(e => UpdateProcessStatus(e.TabId));
        PubSub.Subscribe<LocalWebAppProcessStoppedEvent>(e => UpdateProcessStatus(e.TabId));
        PubSub.Subscribe<LocalWebAppProcessErrorEvent>(e => UpdateProcessStatus(e.TabId));

        PubSub.Subscribe<BrowserHostClosingEvent>(_ => _processManager.StopAllProcesses());

        // Init palette with current config
        PubSub.Subscribe<TabPaletteRequestedEvent>(_ => InitializePalette());

        // Reload tab after process starts (with delay to allow server to initialize)
        PubSub.Subscribe<LocalWebAppProcessStartedEvent>(e => ReloadTabAfterDelay(e.TabId));
    }

    public override void Start()
    {
        _stateManager.RestoreFromDisk();
    }

    private void OnTabActivated(string tabId)
    {
        var config = _stateManager.GetConfig(tabId);
        if (config != null && !_processManager.IsRunning(tabId))
        {
            _processManager.StartProcess(tabId, config);
        }
    }

    private void OnTabClosed(string tabId)
    {
        _processManager.StopProcess(tabId);
    }

    private void InitializePalette()
    {
        var currentTabId = _browserContext.CurrentTab?.Id;
        if (currentTabId != null)
        {
            var config = _stateManager.GetConfig(currentTabId);
            var isRunning = _processManager.IsRunning(currentTabId);
            var hasErrors = _processManager.HasErrors(currentTabId);
            _browserApi.InitConfig(currentTabId, config?.DirectoryPath, config?.StartCommand, isRunning, hasErrors);
        }
    }

    private async void ReloadTabAfterDelay(string tabId)
    {
        // Wait for server to initialize before reloading
        await Task.Delay(2500);

        // Only reload if this is still the current tab
        if (_browserContext.CurrentTab?.Id == tabId)
        {
            _browserContext.CurrentTab.Reload();
        }
    }

    private void UpdateProcessStatus(string tabId)
    {
        var isRunning = _processManager.IsRunning(tabId);
        var hasErrors = _processManager.HasErrors(tabId);
        _browserApi.UpdateProcessStatus(tabId, isRunning, hasErrors);
    }
}
