using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.DragDrop;

public record FileDroppedEvent(string[] FilePaths);

public class DragDropFeature(MainWindow window) : Feature(window)
{
    public static bool IsDragging { get; private set; }

    private static readonly string[] SupportedExtensions =
    [
        // Images - widely supported by browsers
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".ico",
        // Documents - browser-supported formats
        ".pdf", ".txt", ".html", ".htm", ".xml", ".json", ".md", ".css", ".js",
        // Media - basic browser support
        ".mp4", ".webm", ".ogg", ".mp3", ".wav"
    ];

    public override void Configure()
    {
        Window.AllowDrop = true;
        Window.DragEnter += (sender, e) => IsDragging = true;
        Window.DragLeave += (sender, e) => IsDragging = false;
        Window.Drop += OnDrop;

        PubSub.Subscribe<FileDroppedEvent>(HandleFileDropped);
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validFiles = files.Where(IsValidFile).ToArray();

            if (validFiles.Length != 0)
            {
                PubSub.Publish(new FileDroppedEvent(validFiles));
            }
        }
        e.Handled = true;
    }

    private static bool IsValidFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }
        catch (Exception) when (!Debugger.IsAttached)
        {
            return false;
        }
    }

    private void HandleFileDropped(FileDroppedEvent e)
    {
        foreach (var filePath in e.FilePaths)
        {
            try
            {
                var fileUri = new Uri(filePath).AbsoluteUri;
                PubSub.Publish(new NavigationStartedEvent(fileUri, UseCurrentTab: false, SaveInHistory: true));
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                // Log error and continue with other files
                System.Diagnostics.Debug.WriteLine($"Failed to process dropped file {filePath}: {ex.Message}");
            }
        }
    }
}