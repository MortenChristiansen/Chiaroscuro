using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.Workspaces;

public record WorkspaceActivatedEvent(string WorkspaceId, WorkspaceDtoV1 Workspace);

public class WorkspacesApi : BrowserApi
{

}
