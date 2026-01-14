using BrowserHost.Utilities;

namespace BrowserHost.Tests.Infrastructure;

internal sealed class NoopFileOpener : IFileOpener
{
    public void OpenFile(string filePath) { }
}
