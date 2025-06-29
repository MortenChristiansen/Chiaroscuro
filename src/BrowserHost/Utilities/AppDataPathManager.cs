using System.IO;

namespace BrowserHost.Utilities;

public static class AppDataPathManager
{
    /// <summary>
    /// Gets the application data folder path where state files should be stored.
    /// This is in the parent directory from where the application runs.
    /// </summary>
    public static string GetAppDataFolderPath() => 
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

    /// <summary>
    /// Gets the full path for a data file in the application data folder.
    /// </summary>
    /// <param name="fileName">The name of the file (e.g., "tabs.json")</param>
    /// <returns>The full path to the data file</returns>
    public static string GetAppDataFilePath(string fileName) => 
        Path.Combine(GetAppDataFolderPath(), fileName);
}