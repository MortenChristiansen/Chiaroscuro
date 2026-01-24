using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.AppState;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System.Text.Json;

namespace BrowserHost.Tests.Features.TabPalette.LocalWebApp;

public class LocalWebAppFeatureTests
{
    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_saves_config_to_state_manager()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand(tab.Id, "/path/to/app", "npm run dev"));

        var savedConfig = context.LocalWebAppStateManager.GetConfig(tab.Id);
        Assert.NotNull(savedConfig);
        Assert.Equal("/path/to/app", savedConfig.DirectoryPath);
        Assert.Equal("npm run dev", savedConfig.StartCommand);
    }

    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_publishes_LocalWebAppConfigSavedEvent()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand(tab.Id, "/path", "npm start"));

        var evt = PubSubMessages.OfType<LocalWebAppConfigSavedEvent>().Single();
        Assert.Equal(tab.Id, evt.TabId);
        Assert.Equal("/path", evt.Config.DirectoryPath);
    }

    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_updates_browser_api_with_new_config()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand(tab.Id, "/my/app", "yarn dev"));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("updateLocalWebAppConfig", $"'{tab.Id}', '/my/app', 'yarn dev'"));
    }

    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_starts_process_immediately_for_current_tab()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand(tab.Id, "/path/to/app", "npm start"));

        var startCall = context.LocalWebAppProcessManager.StartProcessCalls.Single();
        Assert.Equal(tab.Id, startCall.TabId);
        Assert.Equal("/path/to/app", startCall.Config.DirectoryPath);
    }

    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_does_not_start_process_when_directory_is_blank()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand(tab.Id, " ", "npm start"));

        Assert.Empty(context.LocalWebAppProcessManager.StartProcessCalls);
    }

    [Fact]
    public void Sending_SaveLocalWebAppConfigCommand_does_not_start_process_for_non_current_tab()
    {
        CreateFeature
            .WithCurrentTab()
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new SaveLocalWebAppConfigCommand("other-tab", "/path", "npm start"));

        Assert.Empty(context.LocalWebAppProcessManager.StartProcessCalls);
    }

    [Fact]
    public void Sending_DeleteLocalWebAppConfigCommand_stops_process_and_deletes_config()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1(tab.Id, "/path", "npm start"));
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);

        context.PubSub.Send(new DeleteLocalWebAppConfigCommand(tab.Id));

        Assert.Contains(tab.Id, context.LocalWebAppProcessManager.StopProcessCalls);
        Assert.Null(context.LocalWebAppStateManager.GetConfig(tab.Id));
    }

    [Fact]
    public void Sending_DeleteLocalWebAppConfigCommand_publishes_LocalWebAppConfigDeletedEvent()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new DeleteLocalWebAppConfigCommand(tab.Id));

        var evt = PubSubMessages.OfType<LocalWebAppConfigDeletedEvent>().Single();
        Assert.Equal(tab.Id, evt.TabId);
    }

    [Fact]
    public void Sending_DeleteLocalWebAppConfigCommand_clears_config_in_browser_api()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Send(new DeleteLocalWebAppConfigCommand(tab.Id));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("clearLocalWebAppConfig", $"'{tab.Id}'"));
    }

    [Fact]
    public void Publishing_TabActivatedEvent_starts_process_if_config_exists_and_not_running()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1("new-tab", "/app", "npm start"));

        context.PubSub.Publish(new TabActivatedEvent("new-tab", previousTab));

        var startCall = context.LocalWebAppProcessManager.StartProcessCalls.Single();
        Assert.Equal("new-tab", startCall.TabId);
    }

    [Fact]
    public void Publishing_TabActivatedEvent_does_not_start_process_if_already_running()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1("new-tab", "/app", "npm start"));
        context.LocalWebAppProcessManager.SimulateProcessRunning("new-tab");

        context.PubSub.Publish(new TabActivatedEvent("new-tab", previousTab));

        Assert.Empty(context.LocalWebAppProcessManager.StartProcessCalls);
    }

    [Fact]
    public void Publishing_TabActivatedEvent_does_not_start_process_if_no_config()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Publish(new TabActivatedEvent("new-tab", previousTab));

        Assert.Empty(context.LocalWebAppProcessManager.StartProcessCalls);
    }

    [Fact]
    public void Publishing_TabClosedEvent_stops_the_process()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);

        context.PubSub.Publish(new TabClosedEvent(tab.Id, tab));

        Assert.Contains(tab.Id, context.LocalWebAppProcessManager.StopProcessCalls);
    }

    [Fact]
    public void Publishing_a_BrowserHostClosingEvent_stops_all_local_web_app_processes()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);
        context.LocalWebAppProcessManager.SimulateProcessRunning("tab-two");

        context.PubSub.Publish(new BrowserHostClosingEvent());

        Assert.Equal(1, context.LocalWebAppProcessManager.StopAllProcessesCallCount);
        Assert.Contains(tab.Id, context.LocalWebAppProcessManager.StopProcessCalls);
        Assert.Contains("tab-two", context.LocalWebAppProcessManager.StopProcessCalls);
    }

    [Fact]
    public void Publishing_TabPaletteRequestedEvent_initializes_palette_with_current_config()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1(tab.Id, "/my/project", "npm run dev"));
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("initLocalWebAppConfig", $"'{tab.Id}', '/my/project', 'npm run dev', false, false"));
    }

    [Fact]
    public void Publishing_TabPaletteRequestedEvent_includes_running_status()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1(tab.Id, "/path", "npm start"));
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("initLocalWebAppConfig", $"'{tab.Id}', '/path', 'npm start', true, false"));
    }

    [Fact]
    public void Publishing_TabPaletteRequestedEvent_includes_error_status()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppStateManager.SaveConfig(new LocalWebAppConfigV1(tab.Id, "/path", "npm start"));
        context.LocalWebAppProcessManager.SimulateProcessError(tab.Id);
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("initLocalWebAppConfig", $"'{tab.Id}', '/path', 'npm start', false, true"));
    }

    [Fact]
    public void Publishing_TabPaletteRequestedEvent_does_nothing_when_there_is_no_current_tab()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Publish(new TabPaletteRequestedEvent());

        Assert.Empty(context.LocalWebAppBrowserApi.Invocations);
    }

    [Fact]
    public void Publishing_LocalWebAppProcessStartedEvent_updates_process_status()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new LocalWebAppProcessStartedEvent(tab.Id));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("updateLocalWebAppProcessStatus", $"'{tab.Id}', true, false"));
    }

    [Fact]
    public void Publishing_LocalWebAppProcessStoppedEvent_updates_process_status()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new LocalWebAppProcessStoppedEvent(tab.Id));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("updateLocalWebAppProcessStatus", $"'{tab.Id}', false, false"));
    }

    [Fact]
    public void Publishing_LocalWebAppProcessErrorEvent_updates_process_status_with_error_flag()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppProcessManager.SimulateProcessError(tab.Id);
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new LocalWebAppProcessErrorEvent(tab.Id));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("updateLocalWebAppProcessStatus", $"'{tab.Id}', false, true"));
    }

    [Fact]
    public void Publishing_LocalWebAppProcessErrorEvent_updates_process_status_when_running_and_error()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        context.LocalWebAppProcessManager.SimulateProcessRunning(tab.Id);
        context.LocalWebAppProcessManager.SimulateProcessError(tab.Id);
        context.LocalWebAppBrowserApi.ClearInvocations();

        context.PubSub.Publish(new LocalWebAppProcessErrorEvent(tab.Id));

        Assert.True(context.LocalWebAppBrowserApi.WasCalledWith("updateLocalWebAppProcessStatus", $"'{tab.Id}', true, true"));
    }

    [Fact]
    public void Publishing_LocalWebAppProcessStartedEvent_reloads_the_current_tab_after_a_short_delay()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();

        context.PubSub.Publish(new LocalWebAppProcessStartedEvent(tab.Id));
        context.WaitUntil(() => tab.ReloadCalled);

        Assert.True(tab.ReloadCalled);
    }

    [Fact]
    public void Starting_the_feature_restores_local_web_app_configs_from_disk()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildLocalWebAppFeature();
        SeedLocalWebAppConfigs(context, new LocalWebAppConfigV1(tab.Id, "/path", "npm start"));

        feature.Start();

        var config = context.LocalWebAppStateManager.GetConfig(tab.Id);
        Assert.NotNull(config);
        Assert.Equal("/path", config.DirectoryPath);
        Assert.Equal("npm start", config.StartCommand);
    }

    private static void SeedLocalWebAppConfigs(TestBrowserContext context, params LocalWebAppConfigV1[] configs)
    {
        var statePath = LocalWebAppStateManager.PersistedStatePath;
        var directory = context.FileSystem.Path.GetDirectoryName(statePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            context.FileSystem.Directory.CreateDirectory(directory);
        }

        var versioned = new PersistentData<LocalWebAppDataV1>
        {
            Version = 1,
            Data = new LocalWebAppDataV1(configs)
        };

        context.FileSystem.File.WriteAllText(statePath, JsonSerializer.Serialize(versioned, BrowserHostJsonContext.Default.PersistentDataLocalWebAppDataV1));
    }
}
