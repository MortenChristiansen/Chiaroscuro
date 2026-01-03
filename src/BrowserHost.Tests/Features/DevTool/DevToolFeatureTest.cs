using BrowserHost.Features.ActionContext.Tabs;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.DevTool;

public class DevToolFeatureTest
{
    [Fact]
    public void Publishing_a_TabClosedEvent_closes_the_tab_dev_tools()
    {
        CreateFeature
            .WithCurrentTab(out var tab)
            .CaptureContext(out var context)
            .BuildDevToolFeature();

        context.PubSub.Publish(new TabClosedEvent(tab.Id, tab));

        Assert.True(tab.CloseDevToolsCalled);
    }

    [Fact]
    public void Publishing_a_TabActivatedEvent_closes_dev_tools_for_the_previous_tab()
    {
        CreateFeature
            .WithCurrentTab(out var previousTab)
            .CaptureContext(out var context)
            .BuildDevToolFeature();

        context.PubSub.Publish(new TabActivatedEvent("tab-2", previousTab));

        Assert.True(previousTab.CloseDevToolsCalled);
    }

    [Fact]
    public void Pressing_F12_shows_dev_tools_for_the_current_tab_when_they_are_closed()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .BuildDevToolFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F12));

        Assert.True(handled);
        Assert.True(tab.ShowDevToolsCalled);
    }

    [Fact]
    public void Pressing_F12_closes_dev_tools_for_the_current_tab_when_they_are_open()
    {
        var feature = CreateFeature
            .WithCurrentTab(out var tab)
            .BuildDevToolFeature();
        tab.ShowDevTools();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F12));

        Assert.True(handled);
        Assert.True(tab.CloseDevToolsCalled);
    }

    [Fact]
    public void Pressing_F12_is_handled_even_when_there_is_no_current_tab()
    {
        var feature = CreateFeature
            .BuildDevToolFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.F12));

        Assert.True(handled);
    }
}
