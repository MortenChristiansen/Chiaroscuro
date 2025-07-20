using System;

namespace BrowserHost.Features.Tabs;

public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);

public record WorkspaceDto(string Id, string Name, string Icon, string Color, TabStateDtoV1[] Tabs, string? LastActiveTabId);

public record WorkspacesDataDtoV2(WorkspaceDto[] Workspaces, string ActiveWorkspaceId);
