using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.Tabs;

public record WorkspaceDto(string Id, string Name, string Color, TabDto[] Tabs, int EphemeralTabStartIndex, string? ActiveTabId);
public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);

public static class ActionContextBrowserExtensions
{
    public static void AddTab(this ActionContextBrowser browser, TabDto tab, bool activate = true)
    {
        browser.CallClientApi("addTab", $"{tab.ToJsonObject()}, {activate.ToJsonBoolean()}");
    }

    public static void SetWorkspaces(this ActionContextBrowser browser, WorkspaceDto[] workspaces)
    {
        browser.CallClientApi("setWorkspaces", $"{workspaces.ToJsonObject()}");
    }

    public static void UpdateTabTitle(this ActionContextBrowser browser, string tabId, string? title)
    {
        browser.CallClientApi("updateTitle", $"{tabId.ToJsonString()}, {title.ToJsonString()}");
    }

    public static void UpdateTabFavicon(this ActionContextBrowser browser, string tabId, string? favicon)
    {
        browser.CallClientApi("updateFavicon", $"{tabId.ToJsonString()}, {favicon.ToJsonString()}");
    }

    public static void CloseTab(this ActionContextBrowser browser, string tabId)
    {
        browser.CallClientApi("closeTab", tabId.ToJsonString());
    }

    public static void ToggleTabBookmark(this ActionContextBrowser browser, string tabId)
    {
        browser.CallClientApi("toggleTabBookmark", tabId.ToJsonString());
    }
}
