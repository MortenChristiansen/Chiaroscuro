using BrowserHost.Features.Tabs;
using BrowserHost.Features.Workspaces;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.Folders;

public class FoldersFeature(MainWindow window) : Feature(window)
{
    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.G && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ToggleCurrentTabFolder();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void ToggleCurrentTabFolder()
    {
        var workspacesFeature = Window.GetFeature<WorkspacesFeature>();
        var currentWorkspace = workspacesFeature.CurrentWorkspace;
        var currentTab = Window.CurrentTab;
        if (currentTab == null) return;

        // Only bookmarked (persistent) tabs can be grouped
        var tabIndex = currentWorkspace.Tabs.ToList().FindIndex(t => t.TabId == currentTab.Id);
        if (tabIndex == -1 || tabIndex >= currentWorkspace.EphemeralTabStartIndex) return;

        // Check if tab is already in a folder
        var existingFolder = currentWorkspace.Folders.FirstOrDefault(f =>
            tabIndex >= f.StartIndex && tabIndex <= f.EndIndex);

        if (existingFolder != null)
        {
            RemoveTabFromFolder(currentTab.Id, existingFolder, currentWorkspace);
        }
        else
        {
            CreateFolderWithTab(currentTab.Id, currentWorkspace);
        }
    }

    private void RemoveTabFromFolder(string tabId, FolderDtoV1 folder, WorkspaceDtoV1 currentWorkspace)
    {
        var tabIndex = currentWorkspace.Tabs.ToList().FindIndex(t => t.TabId == tabId);
        if (tabIndex == -1) return;

        var updatedFolders = currentWorkspace.Folders.ToList();

        if (folder.EndIndex == folder.StartIndex)
        {
            // Only one tab in folder, remove the folder entirely
            updatedFolders.Remove(folder);
        }
        else if (tabIndex == folder.StartIndex)
        {
            // Remove first tab, adjust folder start index
            var updatedFolder = folder with { StartIndex = folder.StartIndex + 1 };
            var folderIndex = updatedFolders.FindIndex(f => f.Id == folder.Id);
            updatedFolders[folderIndex] = updatedFolder;
        }
        else if (tabIndex == folder.EndIndex)
        {
            // Remove last tab, adjust folder end index
            var updatedFolder = folder with { EndIndex = folder.EndIndex - 1 };
            var folderIndex = updatedFolders.FindIndex(f => f.Id == folder.Id);
            updatedFolders[folderIndex] = updatedFolder;
        }
        else
        {
            // Remove tab from middle, split folder (for now, just remove from end to keep it simple)
            var updatedFolder = folder with { EndIndex = tabIndex - 1 };
            var folderIndex = updatedFolders.FindIndex(f => f.Id == folder.Id);
            updatedFolders[folderIndex] = updatedFolder;
        }

        // Update workspace with new folders
        SaveFolders([.. updatedFolders], currentWorkspace);
    }

    private void CreateFolderWithTab(string tabId, WorkspaceDtoV1 currentWorkspace)
    {
        var tabIndex = currentWorkspace.Tabs.ToList().FindIndex(t => t.TabId == tabId);
        if (tabIndex == -1) return;

        var newFolder = new FolderDtoV1(
            $"{Guid.NewGuid()}",
            "New Folder",
            tabIndex,
            tabIndex
        );

        var updatedFolders = currentWorkspace.Folders.Append(newFolder).ToArray();
        SaveFolders(updatedFolders, currentWorkspace);
    }

    private void SaveFolders(FolderDtoV1[] folders, WorkspaceDtoV1 currentWorkspace)
    {
        //TODO: Is there a prettier way to do this?

        // Persist the updated workspace state
        PubSub.Publish(new TabsChangedEvent(
            [..currentWorkspace.Tabs.Select(t => new TabUiStateDto(
                t.TabId,
                t.Title ?? "",
                t.Favicon,
                t.IsActive,
                t.Created
            ))],
            currentWorkspace.EphemeralTabStartIndex,
            [.. folders.Select(f => new FolderUiStateDto(f.Id, f.Name, f.StartIndex, f.EndIndex))]
        ));

        // Notify the frontend
        var tabsFeature = Window.GetFeature<TabsFeature>();
        Window.ActionContext.SetTabs(
            [.. tabsFeature.TabBrowsers?.Select(t => new Tabs.TabDto(t.Id, t.Title, t.Favicon, DateTimeOffset.Now)) ?? []],
            Window.CurrentTab?.Id,
            currentWorkspace.EphemeralTabStartIndex,
            [.. folders.Select(f => new Tabs.FolderDto(f.Id, f.Name, f.StartIndex, f.EndIndex))],
            isFullUpdate: false
        );
    }
}
