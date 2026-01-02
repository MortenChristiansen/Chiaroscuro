using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.Workspaces;

public record ActivateWorkspaceCommand(string WorkspaceId) : ICommand;
public record WorkspaceActivatedEvent(string WorkspaceId) : IEvent;

public record CreateWorkspaceCommand(string WorkspaceId, string Name, string Icon, string Color) : ICommand;
public record WorkspaceCreatedEvent(string WorkspaceId, string Name, string Icon, string Color) : IEvent;

public record UpdateWorkspaceCommand(string WorkspaceId, string Name, string Icon, string Color) : ICommand;
public record WorkspaceUpdatedEvent(string WorkspaceId, string Name, string Icon, string Color) : IEvent;

public record DeleteWorkspaceCommand(string WorkspaceId) : ICommand;
public record WorkspaceDeletedEvent(string WorkspaceId) : IEvent;

public record ExpireEphemeralTabsCommand(string[] TabIds) : ICommand;
public record EphemeralTabsExpiredEvent(string[] TabIds) : IEvent;
