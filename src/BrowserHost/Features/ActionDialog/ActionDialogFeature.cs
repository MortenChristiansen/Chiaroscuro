using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionDialog;

public record SearchProvider(string Name, string Key, string Pattern);
public record NavigationStartedEvent(string Address, bool UseCurrentTab, bool SaveInHistory);

public class ActionDialogFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<ActionDialogDismissedEvent>(_ => DismissDialog());
        PubSub.Subscribe<CommandExecutedEvent>(HandleCommandExecuted);
        PubSub.Subscribe<ActionDialogValueChangedEvent>(HandleValueChanged);
        PubSub.Subscribe<TabUrlLoadedSuccessfullyEvent>(e => HandlePageHistoryChange(e.TabId));
        PubSub.Subscribe<TabFaviconUrlChangedEvent>(e => HandlePageHistoryChange(e.TabId));
    }

    private static readonly SearchProvider[] _searchProviders =
    [
        new SearchProvider("Google", "g", "https://www.google.com/search?q={0}"),
        new SearchProvider("GitHub", "gh", "https://github.com/search?q={0}"),
        new SearchProvider("ChatGPT", "ai", "https://chat.openai.com/?q={0}"),
        new SearchProvider("YouTube", "y", "https://www.youtube.com/results?search_query={0}"),
    ];
    private static readonly SearchProvider _defaultSearchProvider = _searchProviders[0];

    private void HandleCommandExecuted(CommandExecutedEvent e)
    {
        if (e.Command.StartsWith('!'))
        {
            HandleSearchProviderCommand(e);
            return;
        }

        if (ContentServer.IsContentServerUrl(e.Command))
        {
            // We don't handle content server URLs
            return;
        }

        if (ContentServer.IsContentPage(e.Command, out var page))
        {
            var pageUrl = ContentServer.GetUiAddress(page.Address);
            PubSub.Publish(new NavigationStartedEvent(pageUrl, UseCurrentTab: e.Ctrl, SaveInHistory: false));
            return;
        }

        if (HandleUsingDefaultSearchProvider(e))
        {
            ExecuteProviderQuery(e, e.Command, _defaultSearchProvider);
            return;
        }

        PubSub.Publish(new NavigationStartedEvent(e.Command, UseCurrentTab: e.Ctrl, SaveInHistory: true));
    }

    private static bool HandleUsingDefaultSearchProvider(CommandExecutedEvent e) =>
        !e.Command.Contains('!') && !e.Command.Contains('.');

    private static void HandleSearchProviderCommand(CommandExecutedEvent e)
    {
        var pair = e.Command.Substring(1).Split(' ', 2);
        if (pair.Length < 2)
            return;

        var key = pair[0];
        var query = pair[1];

        var provider = _searchProviders.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
            return;

        ExecuteProviderQuery(e, query, provider);
    }

    private static void ExecuteProviderQuery(CommandExecutedEvent e, string query, SearchProvider provider)
    {
        var urlEncodedQuery = WebUtility.UrlEncode(query);
        var url = string.Format(provider.Pattern, urlEncodedQuery);
        PubSub.Publish(new NavigationStartedEvent(url, UseCurrentTab: e.Ctrl, SaveInHistory: false));
    }

    private void HandlePageHistoryChange(string tabId)
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null || currentTab.Id != tabId || string.IsNullOrEmpty(currentTab.ManualAddress))
            return;

        NavigationHistoryStateManager.SaveNavigationEntry(currentTab.ManualAddress, currentTab.Title, currentTab.Favicon);
    }

    private void HandleValueChanged(ActionDialogValueChangedEvent e)
    {
        // Get suggestions based on the current input
        var suggestions = NavigationHistoryStateManager.GetSuggestions(e.Value);

        // Send suggestions to frontend
        Window.ActionDialog.UpdateSuggestions(suggestions);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ShowDialog();
            return true;
        }
        return false;
    }

    private void ShowDialog()
    {
        if (Window.ActionDialog.Visibility == Visibility.Visible)
            return;

        ShowActionDialogControl();
        AddGlassOverlayToCurrentTab();
    }

    private void ShowActionDialogControl()
    {
        Window.ActionDialog.Opacity = 0;
        Window.ActionDialog.Visibility = Visibility.Visible;
        Window.ActionDialog.Focus();
        Window.ActionDialog.CallClientApi("showDialog");

        if (Window.ActionDialog.RenderTransform is not ScaleTransform)
        {
            var scale = new ScaleTransform(0, 0, 0.5, 0.5);
            Window.ActionDialog.RenderTransform = scale;
            Window.ActionDialog.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ((ScaleTransform)Window.ActionDialog.RenderTransform).ScaleX = 0;
            ((ScaleTransform)Window.ActionDialog.RenderTransform).ScaleY = 0;
        }

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        Window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((ScaleTransform)Window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        ((ScaleTransform)Window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    private void AddGlassOverlayToCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }

    private void DismissDialog()
    {
        if (Window.ActionDialog.Visibility == Visibility.Hidden)
            return;

        HideActionDialogControl();
        HideGlassOverlayFromCurrentTab();
    }

    private void HideActionDialogControl()
    {
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
        fadeOut.Completed += (s, e) =>
        {
            Window.ActionDialog.Visibility = Visibility.Hidden;
        };
        Window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (Window.ActionDialog.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }

    private void HideGlassOverlayFromCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }
}
