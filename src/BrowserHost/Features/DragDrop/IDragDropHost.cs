using System;

namespace BrowserHost.Features.DragDrop;

public interface IDragDropHost
{
    bool AllowDrop { get; set; }

    event Action DragEnter;
    event Action DragLeave;
    event Action<string[]> FilesDropped;
}
