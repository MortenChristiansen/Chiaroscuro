using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.DragDrop;

namespace BrowserHost.Tests.Features.DragDrop;

public class DragDropFeatureTests
{
    [Fact]
    public void Configuring_the_feature_enables_drag_and_drop_on_the_host()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();

        Assert.True(context.DragDropHost.AllowDrop);
    }

    [Fact]
    public void Dragging_files_into_the_window_sets_IsDragging_to_true()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        context.FakeDragDropHost.RaiseDragLeave();

        context.FakeDragDropHost.RaiseDragEnter();

        Assert.True(feature.IsDragging);
    }

    [Fact]
    public void Dragging_files_out_of_the_window_sets_IsDragging_to_false()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        context.FakeDragDropHost.RaiseDragEnter();

        context.FakeDragDropHost.RaiseDragLeave();

        Assert.False(feature.IsDragging);
    }

    [Fact]
    public void Dropping_supported_existing_files_starts_navigation_for_each_valid_file_and_publishes_an_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        var navigations = new List<StartNavigationCommand>();
        context.PubSub.Handle<StartNavigationCommand>(cmd => navigations.Add(cmd));
        context.FileSystem.Directory.CreateDirectory("C:\\dropped");
        var image = "C:\\dropped\\image.png";
        var document = "C:\\dropped\\doc.pdf";
        context.FileSystem.File.WriteAllText(image, "");
        context.FileSystem.File.WriteAllText(document, "");
        var unsupported = "C:\\dropped\\archive.zip";
        context.FileSystem.File.WriteAllText(unsupported, "");
        var missing = "C:\\dropped\\missing.txt";

        context.FakeDragDropHost.RaiseFilesDropped(image, unsupported, missing, document);

        Assert.Equal(2, navigations.Count);
        Assert.Equal(new Uri(image).AbsoluteUri, navigations[0].Address);
        Assert.Equal(new Uri(document).AbsoluteUri, navigations[1].Address);
        Assert.All(navigations, n =>
        {
            Assert.False(n.UseCurrentTab);
            Assert.True(n.SaveInHistory);
        });
        var opened = Assert.Single(PubSubMessages.OfType<DroppedFilesOpenedEvent>());
        Assert.Equal([image, document], opened.FilePaths);
    }

    [Fact]
    public void Dropping_multiple_files_will_activate_the_first_of_them()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        var navigations = new List<StartNavigationCommand>();
        context.PubSub.Handle<StartNavigationCommand>(cmd => navigations.Add(cmd));
        context.FileSystem.Directory.CreateDirectory("C:\\dropped");
        var file1 = "C:\\dropped\\image1.png";
        var file2 = "C:\\dropped\\image2.png";
        context.FileSystem.File.WriteAllText(file1, "");
        context.FileSystem.File.WriteAllText(file2, "");

        context.FakeDragDropHost.RaiseFilesDropped(file1, file2);

        Assert.Equal(2, navigations.Count);
        Assert.True(navigations[0].ActivateTab);
        Assert.False(navigations[1].ActivateTab);
    }

    [Fact]
    public void Dropping_files_that_are_not_supported_does_not_start_navigation_or_publish_an_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        var navigations = new List<StartNavigationCommand>();
        context.PubSub.Handle<StartNavigationCommand>(cmd => navigations.Add(cmd));
        context.FileSystem.Directory.CreateDirectory("C:\\dropped");
        var unsupported = "C:\\dropped\\archive.zip";
        context.FileSystem.File.WriteAllText(unsupported, "");

        context.FakeDragDropHost.RaiseFilesDropped(unsupported);

        Assert.Empty(navigations);
        Assert.Empty(PubSubMessages.OfType<DroppedFilesOpenedEvent>());
    }

    [Fact]
    public void Sending_an_OpenDroppedFilesCommand_starts_navigation_for_each_file_and_publishes_a_matching_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildDragDropFeature();
        var navigations = new List<StartNavigationCommand>();
        context.PubSub.Handle<StartNavigationCommand>(cmd => navigations.Add(cmd));
        var files = new[]
        {
            "C:\\dropped\\a.txt",
            "C:\\dropped\\b.md",
        };

        context.PubSub.Send(new OpenDroppedFilesCommand(files));

        Assert.Equal(2, navigations.Count);
        Assert.Equal(new Uri(files[0]).AbsoluteUri, navigations[0].Address);
        Assert.Equal(new Uri(files[1]).AbsoluteUri, navigations[1].Address);
        var opened = Assert.Single(PubSubMessages.OfType<DroppedFilesOpenedEvent>());
        Assert.Equal(files, opened.FilePaths);
    }
}
