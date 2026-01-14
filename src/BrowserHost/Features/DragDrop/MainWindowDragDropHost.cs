using System;
using System.Windows;

namespace BrowserHost.Features.DragDrop;

public class MainWindowDragDropHost : IDragDropHost
{
    private readonly MainWindow _window;

    public MainWindowDragDropHost(MainWindow window)
    {
        _window = window;

        _window.DragEnter += (_, _) => DragEnter();
        _window.DragLeave += (_, _) => DragLeave();
        _window.Drop += OnDrop;
    }

    public bool AllowDrop
    {
        get => _window.AllowDrop;
        set => _window.AllowDrop = value;
    }

    public event Action DragEnter = delegate { };
    public event Action DragLeave = delegate { };
    public event Action<string[]> FilesDropped = delegate { };

    private void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                    FilesDropped(files);
            }
        }
        finally
        {
            e.Handled = true;
        }
    }
}
