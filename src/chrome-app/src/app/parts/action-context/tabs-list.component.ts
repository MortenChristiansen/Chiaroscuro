import { Component, effect, OnInit, signal } from '@angular/core';
import { TabListApi } from './tabListApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import {
  CdkDragDrop,
  moveItemInArray,
  DragDropModule,
} from '@angular/cdk/drag-drop';
import { debounce } from '../../shared/utils';
import { FaviconComponent } from '../../shared/favicon.component';
import { CommonModule } from '@angular/common';
import { Tab, TabId, Folder, FolderId } from './server-models';

@Component({
  selector: 'tabs-list',
  imports: [DragDropModule, FaviconComponent, CommonModule],
  template: `
    @let ephemeralIndex = ephemeralTabStartIndex();
    <span
      class="bookmark-label text-gray-500 text-xs px-4"
      style="pointer-events: none;"
    >
      Bookmarks
    </span>

    <div
      class="flex flex-col gap-2"
      cdkDropList
      (cdkDropListDropped)="drop($event)"
    >
      @for (tab of tabs(); track tab.id; let i = $index) { @if ($index ===
      ephemeralIndex) {
      <div
        cdkDrag
        style="pointer-events: none;"
        class="w-full h-0.5 my-2 bg-gradient-to-r from-transparent via-gray-500 to-transparent opacity-60 rounded-full"
      ></div>
      } @let folder = getFolderForTabIndex(i); @if (folder &&
      isFirstTabInFolder(i)) {
      <!-- Folder Header -->
      <div
        class="folder-header group flex items-center px-4 py-1 rounded-lg select-none text-white font-sans text-sm bg-gray-700/50"
      >
        <button
          (click)="toggleFolder(folder.id)"
          class="mr-2 text-gray-400 hover:text-gray-300 transition-colors"
          [attr.aria-label]="
            isFolderOpen(folder.id) ? 'Collapse folder' : 'Expand folder'
          "
        >
          @if (isFolderOpen(folder.id)) {
          <!-- Open folder icon -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M3.75 9.776c.112-.017.227-.026.344-.026h15.812c.117 0 .232.009.344.026m-16.5 0a2.25 2.25 0 0 0-1.883 2.542l.857 6a2.25 2.25 0 0 0 2.227 1.932H19.05a2.25 2.25 0 0 0 2.227-1.932l.857-6a2.25 2.25 0 0 0-1.883-2.542m-16.5 0V6A2.25 2.25 0 0 1 6 3.75h3.879a1.125 1.125 0 0 1 .746.417l2.25 2A1.125 1.125 0 0 0 13.621 6.5H18A2.25 2.25 0 0 1 20.25 8.75v1.026"
            />
          </svg>
          } @else {
          <!-- Closed folder icon -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-8.69-6.44-2.12-2.12a1.5 1.5 0 0 0-1.061-.44H4.5A2.25 2.25 0 0 0 2.25 6v12a2.25 2.25 0 0 0 2.25 2.25h15A2.25 2.25 0 0 0 21.75 18V9a2.25 2.25 0 0 0-2.25-2.25H11.69Z"
            />
          </svg>
          }
        </button>

        @if (editingFolder() === folder.id) {
        <input
          #folderNameInput
          [value]="folder.name"
          (blur)="savefolderName(folder.id, folderNameInput.value)"
          (keydown.enter)="savefolderName(folder.id, folderNameInput.value)"
          (keydown.escape)="editingFolder.set(null)"
          class="flex-1 bg-transparent border-b border-gray-400 focus:border-white outline-none text-white"
          autoFocus
        />
        } @else {
        <span
          class="flex-1 truncate cursor-pointer"
          (click)="toggleFolder(folder.id)"
        >
          {{ folder.name }}
        </span>
        <button
          class="ml-2 opacity-0 group-hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-1 rounded"
          (click)="$event.stopPropagation(); editingFolder.set(folder.id)"
          aria-label="Edit folder name"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.287 4.287 0 0 1-1.897 1.13L6 18l.8-2.685a4.287 4.287 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10"
            />
          </svg>
        </button>
        }
      </div>
      } @if (isTabVisible(i)) {
      <!-- Tab Item -->
      <div
        class="tab group flex items-center px-4 py-2 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 hover:bg-white/10 {{
          tab.id === selectedTab()?.id ? 'bg-white/20 hover:bg-white/30' : ''
        }} {{ getFolderForTabIndex(i) ? 'ml-6' : '' }}"
        (click)="selectedTab.set(tab)"
        cdkDrag
        [cdkDragData]="tab"
      >
        <favicon [src]="tab.favicon" class="w-4 h-4 mr-2" />
        <span class="truncate flex-1">{{ tab.title ?? 'Loading...' }}</span>
        <button
          class="ml-2 opacity-0 group-hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-1 rounded"
          (click)="$event.stopPropagation(); close(tab.id)"
          aria-label="Close tab"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 20 20"
            stroke-width="2"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M6 6l8 8M6 14L14 6"
            />
          </svg>
        </button>
      </div>
      } @if ($index === tabs().length - 1 && $index +1 === ephemeralIndex) {
      <div
        class="w-full h-0.5 my-2 bg-gradient-to-r from-gray-700 via-gray-500 to-gray-700 opacity-60 rounded-full"
        cdkDrag
        style="pointer-events: none;"
      ></div>
      } }
    </div>
  `,
  styles: `
  .cdk-drag-placeholder {
    /* The destination element - currently not styled */
  }

  .cdk-drag-animating {
    transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
  }

  .cdk-drop-list-dragging .tab:not(.cdk-drag-placeholder) {
    transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
  }

  .cdk-drag-preview {
    opacity: 0; /* We don't show an element at the mouse position while dragging */
  }

  .cdk-drop-list-dragging .tab:not(.cdk-drag-placeholder) {
    background: transparent !important; /* Disable hover effect of dragged over elements */
  }

  .cdk-drop-list-dragging .tab:not(.cdk-drag-placeholder) button {
    display: none; /* Hide close button of dragged over elements */
  }
  `,
})
export default class TabsListComponent implements OnInit {
  tabs = signal<Tab[]>([]);
  folders = signal<Folder[]>([]);
  ephemeralTabStartIndex = signal<number>(0);
  tabsInitialized = signal(false);
  selectedTab = signal<Tab | null>(null);
  openFolders = signal<Set<FolderId>>(new Set());
  editingFolder = signal<FolderId | null>(null);
  private saveTabsDebounceDelay = 1000;
  private tabActivationOrderStack: TabId[] = [];

  // Helper method to get the folder that contains a specific tab index
  getFolderForTabIndex(index: number): Folder | undefined {
    return this.folders().find(
      (f) => index >= f.startIndex && index <= f.endIndex
    );
  }

  // Helper method to check if a tab index is the first tab in a folder
  isFirstTabInFolder(index: number): boolean {
    const folder = this.getFolderForTabIndex(index);
    return folder ? index === folder.startIndex : false;
  }

  // Helper method to check if a tab index is the last tab in a folder
  isLastTabInFolder(index: number): boolean {
    const folder = this.getFolderForTabIndex(index);
    return folder ? index === folder.endIndex : false;
  }

  // Helper method to check if a folder is open
  isFolderOpen(folderId: FolderId): boolean {
    return this.openFolders().has(folderId);
  }

  // Toggle folder open/closed state
  toggleFolder(folderId: FolderId): void {
    const openFolders = new Set(this.openFolders());
    if (openFolders.has(folderId)) {
      openFolders.delete(folderId);
    } else {
      openFolders.add(folderId);
    }
    this.openFolders.set(openFolders);
  }

  // Check if a tab should be visible (not in a closed folder)
  isTabVisible(index: number): boolean {
    const folder = this.getFolderForTabIndex(index);
    if (!folder) return true; // Not in a folder, always visible
    return this.isFolderOpen(folder.id); // Other tabs in folder are visible only if folder is open
  }

  // Save folder name after editing
  savefolderName(folderId: FolderId, newName: string): void {
    this.editingFolder.set(null);

    // Update local folder name immediately for responsive UI
    this.folders.update((folders) =>
      folders.map((f) => (f.id === folderId ? { ...f, name: newName } : f))
    );
  }

  // Check if a drop position is valid (simplified for now)
  isValidDropPosition(targetIndex: number): boolean {
    // For now, prevent dropping in the middle of closed folders
    const folder = this.getFolderForTabIndex(targetIndex);
    if (!folder) return true;

    // Allow dropping at the start or end of a folder, or if folder is open
    return (
      this.isFirstTabInFolder(targetIndex) ||
      this.isLastTabInFolder(targetIndex) ||
      this.isFolderOpen(folder.id)
    );
  }

  constructor() {
    effect(() => {
      const activeTab = this.selectedTab();
      if (!activeTab) return;
      this.api.activateTab(activeTab.id);
    });

    effect(() => {
      const activeTab = this.selectedTab();
      if (!activeTab) return;

      this.tabActivationOrderStack = [
        ...this.tabActivationOrderStack.filter((id) => id !== activeTab.id),
        activeTab.id,
      ];
    });

    effect(() => {
      if (!this.tabsInitialized()) return;
      const currentTabs = this.tabs();
      const currentFolders = this.folders();
      this.tabsChanged(
        currentTabs,
        currentFolders,
        this.selectedTab()?.id ?? null
      );
    });
  }

  private tabsChanged = debounce(
    (tabs: Tab[], folders: Folder[], selectedTabId: TabId | null) => {
      this.api.tabsChanged(
        tabs.map((tab) => ({
          Id: tab.id,
          Title: tab.title,
          Favicon: tab.favicon,
          IsActive: tab.id === selectedTabId,
          Created: tab.created,
        })),
        this.ephemeralTabStartIndex(),
        folders.map((folder) => ({
          Id: folder.id,
          Name: folder.name,
          StartIndex: folder.startIndex,
          EndIndex: folder.endIndex,
        }))
      );
    },
    this.saveTabsDebounceDelay
  );

  drop(event: CdkDragDrop<any>) {
    // For now, keep the existing logic but add basic folder validation
    const currentTabs = [...this.tabs()];
    const ephemeralIndex = this.ephemeralTabStartIndex();

    // Basic validation - prevent drops in invalid positions
    if (!this.isValidDropPosition(event.currentIndex)) {
      console.log('Invalid drop position, ignoring drop');
      return;
    }

    const { adjustedCurrentIndex, adjustedPreviousIndex } =
      this.adjustDragIndices(
        event.currentIndex,
        event.previousIndex,
        ephemeralIndex
      );

    moveItemInArray(currentTabs, adjustedPreviousIndex, adjustedCurrentIndex);
    this.tabs.set(currentTabs);

    // If the item was dragged past the separator, update ephemeralTabStartIndex
    if (
      event.previousIndex < ephemeralIndex &&
      event.currentIndex >= ephemeralIndex
    ) {
      // Moved from persistent to ephemeral
      this.ephemeralTabStartIndex.set(ephemeralIndex - 1);
    } else if (
      event.previousIndex > ephemeralIndex &&
      event.currentIndex <= ephemeralIndex
    ) {
      // Moved from ephemeral to persistent
      this.ephemeralTabStartIndex.set(ephemeralIndex + 1);
    }

    // TODO: Update folder boundaries if tabs moved in/out of folders
    // For now, this is a simplified implementation
  }

  private adjustDragIndices(
    currentIndex: number,
    previousIndex: number,
    ephemeralIndex: number
  ) {
    let adjustedCurrentIndex =
      currentIndex - (currentIndex > ephemeralIndex ? 1 : 0);
    const adjustedPreviousIndex =
      previousIndex - (previousIndex > ephemeralIndex ? 1 : 0);

    // I cannot explain the nature of this condition but it solves an edge case when dragging a persistent tab to the top of the ephemeral tabs
    if (
      currentIndex == ephemeralIndex &&
      adjustedCurrentIndex == ephemeralIndex &&
      currentIndex > previousIndex
    )
      adjustedCurrentIndex--;

    return { adjustedCurrentIndex, adjustedPreviousIndex };
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabListApi>('tabsApi');

    exposeApiToBackend({
      addTab: (tab: Tab, activate: boolean) => {
        this.tabs.update((currentTabs) => [...currentTabs, tab]);

        if (activate) {
          this.selectedTab.set(tab);
        }
      },
      setTabs: (
        tabs: Tab[],
        activeTabId: TabId,
        ephemeralTabStartIndex: number,
        folders: Folder[]
      ) => {
        this.tabs.set(tabs);

        // Update folders and auto-open any new folders
        const currentFolderIds = new Set(this.folders().map((f) => f.id));
        const newFolders = folders;
        const newFolderIds = newFolders
          .filter((f) => !currentFolderIds.has(f.id))
          .map((f) => f.id);

        this.folders.set(folders);

        // Auto-open newly created folders
        if (newFolderIds.length > 0) {
          const currentOpen = new Set(this.openFolders());
          newFolderIds.forEach((id) => currentOpen.add(id));
          this.openFolders.set(currentOpen);
        }

        this.ephemeralTabStartIndex.set(ephemeralTabStartIndex);

        const activeTab = tabs.find((t) => t.id === activeTabId);
        if (activeTab) {
          this.selectedTab.set(activeTab);
        }
        this.tabsInitialized.set(true);
        this.tabActivationOrderStack = [
          ...tabs.filter((t) => t.id !== activeTabId).map((t) => t.id),
          activeTabId,
        ];
      },
      updateTitle: (tabId: TabId, title: string | null) => {
        this.tabs.update((currentTabs) => {
          const updatedTabs = currentTabs.map((tab, i) =>
            currentTabs[i].id === tabId ? { ...tab, title } : tab
          );
          return updatedTabs;
        });
      },
      updateFavicon: (tabId: TabId, favicon: string | null) => {
        this.tabs.update((currentTabs) => {
          const updatedTabs = currentTabs.map((tab, i) =>
            currentTabs[i].id === tabId ? { ...tab, favicon } : tab
          );
          return updatedTabs;
        });
      },
      closeTab: (tabId: TabId) => this.close(tabId, false),
      toggleTabBookmark: (tabId: TabId) => this.toggleBookmark(tabId),
    });
  }

  api!: TabListApi;

  close(tabId: TabId, updateBackend = true) {
    this.tabActivationOrderStack = this.tabActivationOrderStack.filter(
      (id) => id !== tabId
    );
    this.tabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );

    if (this.selectedTab()?.id === tabId) {
      const newSelectedTabId =
        this.tabs().length == 0
          ? null
          : this.tabActivationOrderStack[
              this.tabActivationOrderStack.length - 1
            ];
      const newSelectedTab =
        this.tabs().find((t) => t.id === newSelectedTabId) || null;
      this.selectedTab.set(newSelectedTab);
    }

    if (updateBackend) {
      this.api.closeTab(tabId);
    }
  }

  toggleBookmark(tabId: TabId) {
    const currentTabs = [...this.tabs()];
    const ephemeralIndex = this.ephemeralTabStartIndex();
    const tabIndex = currentTabs.findIndex((t) => t.id === tabId);

    if (tabIndex === -1) return; // Tab not found

    const tab = currentTabs[tabIndex];
    const isCurrentlyEphemeral = tabIndex >= ephemeralIndex;

    // Remove tab from current position
    currentTabs.splice(tabIndex, 1);

    if (isCurrentlyEphemeral) {
      // Moving from ephemeral to persistent (bookmark the tab)
      // Insert at the end of persistent tabs (which is now at ephemeralIndex after removal)
      currentTabs.splice(ephemeralIndex, 0, tab);
      // Update ephemeralIndex as persistent section grew by 1
      this.ephemeralTabStartIndex.set(ephemeralIndex + 1);
    } else {
      // Moving from persistent to ephemeral (unbookmark the tab)
      // Insert at the end of all tabs (end of ephemeral section)
      currentTabs.push(tab);
      // Update ephemeralIndex to account for one less persistent tab
      this.ephemeralTabStartIndex.set(ephemeralIndex - 1);
    }

    this.tabs.set(currentTabs);
  }
}
