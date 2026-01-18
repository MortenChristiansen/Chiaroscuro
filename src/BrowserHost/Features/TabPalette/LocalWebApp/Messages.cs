using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

// Config commands
public record SaveLocalWebAppConfigCommand(string TabId, string DirectoryPath, string? StartCommand) : ICommand;
public record DeleteLocalWebAppConfigCommand(string TabId) : ICommand;
public record BrowseLocalWebAppDirectoryCommand(string TabId) : ICommand;

// Config events
public record LocalWebAppConfigSavedEvent(string TabId, LocalWebAppConfigV1 Config) : IEvent;
public record LocalWebAppConfigDeletedEvent(string TabId) : IEvent;

// Process events
public record LocalWebAppProcessStartedEvent(string TabId) : IEvent;
public record LocalWebAppProcessStoppedEvent(string TabId) : IEvent;
public record LocalWebAppProcessErrorEvent(string TabId) : IEvent;
