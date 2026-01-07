using BrowserHost.Features.AppState;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System.Text.Json;

namespace BrowserHost.Tests.Features.AppState;

public class AppStateFeatureTest
{
    [Fact]
    public void Starting_the_feature_restores_the_action_context_width_from_persisted_state()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildAppStateFeature();
        SeedAppState(context, actionContextWidth: 444, tabPaletteWidth: 350);

        feature.Start();

        Assert.Equal(444, context.ActionContextWidthSetTo);
    }

    [Fact]
    public void Completing_an_action_context_resize_persists_the_new_width()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildAppStateFeature();
        context.ActionContextActualWidth = 555;

        context.RaiseActionContextResizeCompleted();

        var restored = new AppStateStateManager(context.FileSystem).RestoreAppStateFromDisk();
        Assert.Equal(555, restored.ActionContextWidth);
    }

    [Fact]
    public void Completing_a_tab_palette_resize_persists_the_new_width()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildAppStateFeature();
        context.TabPaletteActualWidth = 333;

        context.RaiseTabPaletteResizeCompleted();

        var restored = new AppStateStateManager(context.FileSystem).RestoreAppStateFromDisk();
        Assert.Equal(333, restored.TabPaletteWidth);
    }

    private static void SeedAppState(TestBrowserContext context, double actionContextWidth, double tabPaletteWidth)
    {
        var state = new PersistentData<AppStateDataV1>
        {
            Version = 1,
            Data = new AppStateDataV1(ActionContextWidth: actionContextWidth, TabPaletteWidth: tabPaletteWidth)
        };

        var json = JsonSerializer.Serialize(state, BrowserHostJsonContext.Default.PersistentDataAppStateDataV1);

        var directory = context.FileSystem.Path.GetDirectoryName(AppStateStateManager.PersistedStatePath);
        if (!string.IsNullOrWhiteSpace(directory))
            context.FileSystem.Directory.CreateDirectory(directory);

        context.FileSystem.File.WriteAllText(AppStateStateManager.PersistedStatePath, json);
    }
}
