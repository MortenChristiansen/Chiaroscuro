using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.DragDrop;

public record FileDroppedEvent(string[] FilePaths);

public class DragDropFeature(MainWindow window) : Feature(window)
{
    private static readonly string[] SupportedExtensions = 
    [
        // Images - widely supported by browsers
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".ico",
        // Documents - browser-supported formats
        ".pdf", ".txt", ".html", ".htm", ".xml", ".json", ".md", ".css", ".js",
        // Media - basic browser support
        ".mp4", ".webm", ".ogg", ".mp3", ".wav"
    ];

    public override void Register()
    {
        Window.AllowDrop = true;
        Window.DragEnter += OnDragEnter;
        Window.DragOver += OnDragOver;
        Window.Drop += OnDrop;

        PubSub.Subscribe<FileDroppedEvent>(HandleFileDropped);
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Any(IsValidFile))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Any(IsValidFile))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validFiles = files.Where(IsValidFile).ToArray();
            
            if (validFiles.Any())
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

            // Check if file is readable
            using var stream = File.OpenRead(filePath);
            
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }
        catch
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
            catch (Exception ex)
            {
                // Log error and continue with other files
                System.Diagnostics.Debug.WriteLine($"Failed to process dropped file {filePath}: {ex.Message}");
            }
        }
    }
}