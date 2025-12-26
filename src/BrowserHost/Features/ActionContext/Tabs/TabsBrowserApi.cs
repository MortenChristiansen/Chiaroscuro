using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.ActionContext.Tabs;

public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);
public record FolderDto(string Id, string Name, int StartIndex, int EndIndex);
public record TabCustomizationDto(string TabId, string? CustomTitle);

public class TabsBrowserApi(BaseBrowser actionContextBrowser) : BrowserApi(actionContextBrowser)
{
    public void AddTab(TabDto tab, bool activate = true) =>
        CallClientApi("addTab", $"{tab.ToJsonObject()}, {activate.ToJsonBoolean()}");

    public void SetTabs(TabDto[] tabs, string? activeTabId, int ephemeralTabIndex, FolderDto[] folders) =>
        CallClientApi("setTabs", $"{tabs.ToJsonObject()}, {activeTabId.ToJsonString()}, {ephemeralTabIndex}, {folders.ToJsonObject()}");

    public void UpdateFolders(FolderDto[] folders) =>
        CallClientApi("updateFolders", $"{folders.ToJsonObject()}");

    public void UpdateTabTitle(string tabId, string? title) =>
        CallClientApi("updateTitle", $"{tabId.ToJsonString()}, {title.ToJsonString()}");

    public void UpdateTabFavicon(string tabId, string? favicon) =>
        CallClientApi("updateFavicon", $"{tabId.ToJsonString()}, {favicon.ToJsonString()}");

    public void CloseTab(string tabId, bool activateNext = true) =>
        CallClientApi("closeTab", $"{tabId.ToJsonString()}, {activateNext.ToJsonBoolean()}");

    public void ToggleTabBookmark(string tabId) =>
        CallClientApi("toggleTabBookmark", tabId.ToJsonString());

    public void SetActiveTab(string? tabId) =>
        CallClientApi("setActiveTab", tabId.ToJsonString());
}
