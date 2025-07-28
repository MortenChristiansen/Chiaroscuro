using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.Tabs;

public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);
public record FolderDto(string Id, string Name, int StartIndex, int EndIndex);

public static class ActionContextBrowserExtensions
{
    public static void AddTab(this ActionContextBrowser browser, TabDto tab, bool activate = true)
    {
        browser.CallClientApi("addTab", $"{tab.ToJsonObject()}, {activate.ToJsonBoolean()}");
    }

    public static void SetTabs(this ActionContextBrowser browser, TabDto[] tabs, string? activeTabId, int ephemeralTabIndex, FolderDto[] folders)
    {
        browser.CallClientApi("setTabs", $"{tabs.ToJsonObject()}, {activeTabId.ToJsonString()}, {ephemeralTabIndex}, {folders.ToJsonObject()}");
    }

    public static void UpdateFolders(this ActionContextBrowser browser, FolderDto[] folders)
    {
        browser.CallClientApi("updateFolders", $"{folders.ToJsonObject()}");
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
