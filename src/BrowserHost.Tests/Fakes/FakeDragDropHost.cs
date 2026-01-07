using BrowserHost.Features.DragDrop;

namespace BrowserHost.Tests.Fakes;

internal class FakeDragDropHost : IDragDropHost
{
    public bool AllowDrop { get; set; }

    public event Action DragEnter = delegate { };
    public event Action DragLeave = delegate { };
    public event Action<string[]> FilesDropped = delegate { };

    public void RaiseDragEnter() => DragEnter();
    public void RaiseDragLeave() => DragLeave();
    public void RaiseFilesDropped(params string[] filePaths) => FilesDropped(filePaths);
}
