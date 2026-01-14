using System;
using System.Diagnostics;
using System.IO;

namespace BrowserHost.Utilities;

public interface IFileOpener
{
    /// <summary>
    /// Open the specified file using the system's default application.
    /// </summary>
    /// <param name="filePath">The path to the file</param>
    void OpenFile(string filePath);
}

internal sealed class ShellFileOpener : IFileOpener
{
    public void OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' does not exist.", filePath);

        var processStartInfo = new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        };

        var _ = Process.Start(processStartInfo) ?? throw new InvalidOperationException($"Failed to start process for file '{filePath}'.");
    }
}
