using BrowserHost.Utilities;

namespace BrowserHost.Features.DragDrop;

public record OpenDroppedFilesCommand(string[] FilePaths) : ICommand;
public record DroppedFilesOpenedEvent(string[] FilePaths) : IEvent;
