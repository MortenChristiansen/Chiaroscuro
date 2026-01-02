using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.DragDrop;

public record OpenDroppedFilesCommand(string[] FilePaths) : ICommand;
public record DroppedFilesOpenedEvent(string[] FilePaths) : IEvent;

public class DragDropFeature(MainWindow window, PubSub pubSub) : Feature(window, pubSub)
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

        PubSub.Handle<OpenDroppedFilesCommand>(e =>
        {
            OpenFileTabs(e.FilePaths);
            PubSub.Publish(new DroppedFilesOpenedEvent(e.FilePaths));
        });
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validFiles = files.Where(IsValidFile).ToArray();

            if (validFiles.Length != 0)
            {
                PubSub.Send(new OpenDroppedFilesCommand(validFiles));
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

    private void OpenFileTabs(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            try
            {
                var fileUri = new Uri(filePath).AbsoluteUri;
                PubSub.Send(new StartNavigationCommand(fileUri, UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                // Log error and continue with other files
                Debug.WriteLine($"Failed to process dropped file {filePath}: {ex.Message}");
            }
        }
    }
}