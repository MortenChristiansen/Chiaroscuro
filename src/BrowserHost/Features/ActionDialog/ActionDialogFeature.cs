using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionDialog;

public record SearchProvider(string Name, string Key, string Pattern);
public record NavigationStartedEvent(string Address, bool UseCurrentTab, bool SaveInHistory);

public partial class ActionDialogFeature(MainWindow window) : Feature(window)
{
    [GeneratedRegex(@"^!(\w+)|\s+!(\w+)$")]
    private static partial Regex SearchProviderRegex();

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
        if (TryGetSearchProvider(e.Command, out var searchProvider))
        {
            ExecuteProviderQuery(e, e.Command, searchProvider);
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

    private static bool TryGetSearchProvider(string command, [NotNullWhen(true)] out SearchProvider? provider)
    {
        var result = SearchProviderRegex().Match(command);
        if (result.Success)
        {
            var key = result.Groups[1].Value;
            if (string.IsNullOrEmpty(key))
                key = result.Groups[2].Value;
            provider = _searchProviders.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            return provider != null;
        }
        provider = null;
        return false;
    }

    private static bool HandleUsingDefaultSearchProvider(CommandExecutedEvent e) =>
        e.Command.Trim().Contains(' ') || !e.Command.Contains('.');

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
        PubSub.Publish(new ActionDialogShownEvent());

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
