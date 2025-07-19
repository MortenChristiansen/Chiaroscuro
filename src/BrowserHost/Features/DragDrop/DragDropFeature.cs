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
        // Images
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".ico",
        // Documents
        ".pdf", ".txt", ".html", ".htm", ".xml", ".json", ".md",
        // Media (limited support)
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
        if (!File.Exists(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    private void HandleFileDropped(FileDroppedEvent e)
    {
        foreach (var filePath in e.FilePaths)
        {
            var fileUri = new Uri(filePath).AbsoluteUri;
            PubSub.Publish(new NavigationStartedEvent(fileUri, UseCurrentTab: false, SaveInHistory: true));
        }
    }
}