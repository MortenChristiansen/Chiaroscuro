using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace BrowserHost.Features.ActionDialog;

public record SearchProvider(string Name, string Key, string Pattern);

public enum ActionType
{
    Navigate,
    Search,
    OpenSystemPage,
}
public partial class ActionDialogFeature(PubSub pubSub, IBrowserContext context, ActionDialogBrowserApi actionDialogApi, NavigationHistoryStateManager navigationHistoryStateManager, ActionDialogWindowOperations windowOperations) : Feature(pubSub)
{
    [GeneratedRegex(@"^!(\w+)|\s+!(\w+)$")]
    private static partial Regex SearchProviderRegex();

    public override void Configure()
    {
        PubSub.Handle<DismissActionDialogCommand>(_ =>
        {
            DismissDialog();
            PubSub.Publish(new ActionDialogDismissedEvent());
        });
        PubSub.Handle<ExecuteCommandCommand>(cmd =>
        {
            HandleCommandExecuted(new CommandExecutedEvent(cmd.Command, cmd.Ctrl));
            PubSub.Publish(new CommandExecutedEvent(cmd.Command, cmd.Ctrl));
        });
        PubSub.Handle<ChangeActionDialogValueCommand>(cmd =>
        {
            HandleValueChanged(new ActionDialogValueChangedEvent(cmd.Value));
            PubSub.Publish(new ActionDialogValueChangedEvent(cmd.Value));
        });

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
        if (TryGetSearchProvider(e.Command, out var searchProvider, out var remainderQuery))
        {
            ExecuteProviderQuery(e, remainderQuery, searchProvider);
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
            PubSub.Send(new StartNavigationCommand(pageUrl, UseCurrentTab: e.Ctrl, SaveInHistory: false, ActivateTab: true));
            return;
        }

        if (HandleUsingDefaultSearchProvider(e))
        {
            ExecuteProviderQuery(e, e.Command, _defaultSearchProvider);
            return;
        }

        PubSub.Send(new StartNavigationCommand(e.Command, UseCurrentTab: e.Ctrl, SaveInHistory: true, ActivateTab: true));
    }

    public static ActionType GetActionType(string command)
    {
        if (TryGetSearchProvider(command, out _, out _))
            return ActionType.Search;
        if (ContentServer.IsContentPage(command, out var page))
            return ActionType.OpenSystemPage;
        if (HandleUsingDefaultSearchProvider(new CommandExecutedEvent(command, false)))
            return ActionType.Search;

        return ActionType.Navigate;
    }

    private static bool TryGetSearchProvider(string command, [NotNullWhen(true)] out SearchProvider? provider, out string remainderQuery)
    {
        var result = SearchProviderRegex().Match(command);
        if (result.Success)
        {
            string key;
            if (result.Groups[1].Success)
            {
                // Search provider found at start of query
                key = result.Groups[1].Value;
                remainderQuery = command[result.Length..].Trim();
            }
            else
            {
                // Search provider found at end of query
                key = result.Groups[2].Value;
                remainderQuery = command[..result.Index].Trim();
            }

            provider = _searchProviders.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            return provider != null;
        }

        provider = null;
        remainderQuery = string.Empty;
        return false;
    }

    private static bool HandleUsingDefaultSearchProvider(CommandExecutedEvent e) =>
        e.Command.Trim().Contains(' ') || !e.Command.Contains('.');

    private void ExecuteProviderQuery(CommandExecutedEvent e, string query, SearchProvider provider)
    {
        var urlEncodedQuery = WebUtility.UrlEncode(query);
        var url = string.Format(provider.Pattern, urlEncodedQuery);
        PubSub.Send(new StartNavigationCommand(url, UseCurrentTab: e.Ctrl, SaveInHistory: false, ActivateTab: true));
    }

    private void HandlePageHistoryChange(string tabId)
    {
        var currentTab = context.CurrentTab;
        if (currentTab == null || currentTab.Id != tabId || string.IsNullOrEmpty(currentTab.ManualAddress))
            return;

        navigationHistoryStateManager.SaveNavigationEntry(currentTab.ManualAddress, currentTab.Title, currentTab.Favicon);
    }

    private void HandleValueChanged(ActionDialogValueChangedEvent e)
    {
        // Get suggestions based on the current input
        var suggestions = navigationHistoryStateManager.GetSuggestions(e.Value);

        // Send suggestions to frontend
        actionDialogApi.UpdateSuggestions(suggestions);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && context.CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            ShowDialog();
            return true;
        }
        return false;
    }

    private void ShowDialog()
    {
        if (windowOperations.ActionDialogIsVisible)
            return;

        windowOperations.ShowActionDialog();
        actionDialogApi.ShowActionDialog();
        PubSub.Publish(new ActionDialogShownEvent());
        AddGlassOverlayToCurrentTab();
    }

    private void AddGlassOverlayToCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }

    private void DismissDialog()
    {
        if (!windowOperations.ActionDialogIsVisible)
            return;

        windowOperations.HideActionDialog();
        HideGlassOverlayFromCurrentTab();
    }

    private void HideGlassOverlayFromCurrentTab()
    {
        // TODO: Implement glass overlay logic
        return;
    }
}
