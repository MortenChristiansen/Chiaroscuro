using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;

namespace BrowserHost.Features.DragDrop;

public class DragDropFeature(MainWindow window, PubSub pubSub, IBrowserContext browserContext, IFileSystem fileSystem) : Feature(window, pubSub)
{
    public bool IsDragging { get; private set; }

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
        browserContext.DragDropHost.AllowDrop = true;
        browserContext.DragDropHost.DragEnter += () => IsDragging = true;
        browserContext.DragDropHost.DragLeave += () => IsDragging = false;
        browserContext.DragDropHost.FilesDropped += OnFilesDropped;

        PubSub.Handle<OpenDroppedFilesCommand>(cmd =>
        {
            OpenFileTabs(cmd.FilePaths);
            PubSub.Publish(new DroppedFilesOpenedEvent(cmd.FilePaths));
        });
    }

    private void OnFilesDropped(string[] filePaths)
    {
        IsDragging = false;
        var validFiles = filePaths.Where(IsValidFile).ToArray();

        if (validFiles.Length != 0)
            PubSub.Send(new OpenDroppedFilesCommand(validFiles));
    }

    private bool IsValidFile(string filePath)
    {
        try
        {
            if (!fileSystem.File.Exists(filePath))
                return false;

            var extension = fileSystem.Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }
        catch (Exception) when (!Debugger.IsAttached)
        {
            return false;
        }
    }

    private void OpenFileTabs(string[] filePaths)
    {
        var hasOpenedFirstTab = false;
        foreach (var filePath in filePaths)
        {
            try
            {
                var fileUri = new Uri(filePath).AbsoluteUri;
                var shouldActivateTab = !hasOpenedFirstTab;
                PubSub.Send(new StartNavigationCommand(fileUri, UseCurrentTab: false, SaveInHistory: true, ActivateTab: shouldActivateTab));
                hasOpenedFirstTab = true;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                // Log error and continue with other files
                Debug.WriteLine($"Failed to process dropped file {filePath}: {ex.Message}");
            }
        }
    }
}