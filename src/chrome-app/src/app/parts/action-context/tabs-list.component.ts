import { Component, computed, effect, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FolderIndexStateDto, TabListApi } from './tabListApi';
import { WorkspaceListApi } from './workspaceListApi';

interface WorkspacesApi {
  returnToOriginalAddress: (tabId: string) => void;
}
import {
  Folder,
  FolderId,
  Tab,
  TabCustomization,
  TabId,
} from './server-models';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabsListTabComponent } from './tabs-list-tab.component';
import { debounce } from '../../shared/utils';
import { Stack } from '../../shared/stack';
import { TabsListFolderComponent } from './tab-list-folder.component';
import { SortablejsModule } from 'nxt-sortablejs';
import { Options } from 'sortablejs';

interface FolderDto {
  id: string;
  name: string;
  isOpen: boolean;
  isNew: boolean;
  tabs: Tab[];
}

@Component({
  selector: 'tabs-list',
  imports: [
    CommonModule,
    TabsListTabComponent,
    TabsListFolderComponent,
    SortablejsModule,
  ],
  template: `
    <span
      class="bookmark-label text-gray-500 text-xs px-4"
      style="pointer-events: none;"
    >
      Bookmarks
    </span>

    <div
      id="persistent-tabs"
      class="flex flex-col gap-2"
      [nxtSortablejs]="sortablePersistedTabs"
      [config]="sortableOptions"
    >
      @for (tabOrFolder of sortablePersistedTabs; track
      getTrackingKey(tabOrFolder, $index)) { @if (!isFolder(tabOrFolder)) { @let
      tab = tabOrFolder;
      <tabs-list-tab
        [tab]="tab"
        [isActive]="tab.id == activeTabId()"
        [isPersistentTab]="true"
        [customization]="getTabCustomization(tab)"
        (selectTab)="activeTabId.set(tab.id)"
        (closeTab)="closeTab(tab.id, true)"
        (returnToOriginal)="returnToOriginal(tab.id)"
      />
      } @else { @let folder = tabOrFolder;
      <tabs-list-folder
        [name]="folder.name"
        [isOpen]="folder.isOpen"
        [isNew]="folder.isNew"
        [containsActiveTab]="containsActiveTab(folder)"
        (toggleOpen)="toggleFolder(folder.id)"
        (folderRenamed)="renameFolder(folder.id, $event)"
      >
        <div
          class="flex flex-col gap-2 tab-folder"
          [nxtSortablejs]="folder.tabs"
          [config]="sortableOptions"
        >
          @for (tab of folder.tabs; track tab.id) {
          <tabs-list-tab
            [tab]="tab"
            [isActive]="tab.id == activeTabId()"
            [isPersistentTab]="true"
            [customization]="getTabCustomization(tab)"
            (selectTab)="activeTabId.set(tab.id)"
            (closeTab)="closeTab(tab.id, true)"
            (returnToOriginal)="returnToOriginal(tab.id)"
          />
          }
        </div>
      </tabs-list-folder>
      } }
    </div>

    <div
      class="w-full h-0.5 my-2 bg-gradient-to-r from-gray-700 via-gray-500 to-gray-700 opacity-60 rounded-full"
      style="pointer-events: none;"
    ></div>

    <div
      id="ephemeral-tabs"
      class="flex flex-col gap-2"
      [nxtSortablejs]="sortableEphemeralTabs"
      [config]="sortableOptions"
    >
      @for (tab of sortableEphemeralTabs; track getTrackingKey(tab, $index)) {
      <tabs-list-tab
        [tab]="tab"
        [isActive]="tab.id == activeTabId()"
        [customization]="getTabCustomization(tab)"
        (selectTab)="activeTabId.set(tab.id)"
        (closeTab)="closeTab(tab.id, true)"
      />
      }
    </div>
  `,
  styles: ``,
})
export class TabsListComponent implements OnInit {
  sortablePersistedTabs: (Tab | FolderDto)[] = [];
  persistedTabs = signal<(Tab | FolderDto)[]>([]);
  allPersistentTabs = computed(() =>
    this.persistedTabs().flatMap((x) => (this.isFolder(x) ? x.tabs : [x]))
  );
  sortableEphemeralTabs: Tab[] = [];
  ephemeralTabs = signal<Tab[]>([]);
  activeTabId = signal<TabId | undefined>(undefined);
  tabsInitialized = signal(false);
  tabCustomizations = signal<TabCustomization[]>([]);
  private saveTabsDebounceDelay = 1000;
  private tabActivationOrderStack = new Stack<TabId>();
  private folderOpenState: Record<string, boolean> = {};

  api!: TabListApi;
  workspacesApi!: WorkspacesApi;

  sortableOptions: Options = {
    handle: '.drag-handle',
    animation: 150,
    forceFallback: true,
    group: {
      name: 'tabs',
      put: (to, from, draggedElement) => {
        // Prevent folders from being dropped into other folders
        if (
          draggedElement.tagName === 'TABS-LIST-FOLDER' &&
          to.el.classList.contains('tab-folder')
        )
          return false;
        // Prevent folders from being dropped into ephemeral-tabs
        return (
          to.el.id !== 'ephemeral-tabs' ||
          draggedElement.tagName !== 'TABS-LIST-FOLDER'
        );
      },
    },
    onUpdate: (e) => {
      if (
        e.from.id === 'persistent-tabs' ||
        e.from.classList.contains('tab-folder')
      ) {
        this.persistedTabs.set(this.sortablePersistedTabs);
      }
      if (e.from.id === 'ephemeral-tabs') {
        this.ephemeralTabs.set(this.sortableEphemeralTabs);
      }
    },
    onAdd: (e) => {
      const classes: string[] = [];
      e.to.classList.forEach((cls) => classes.push(cls));

      if (
        e.to.id === 'persistent-tabs' ||
        e.to.classList.contains('tab-folder')
      ) {
        this.persistedTabs.set(this.sortablePersistedTabs);
      }
      if (e.to.id === 'ephemeral-tabs') {
        this.ephemeralTabs.set(this.sortableEphemeralTabs);
      }
    },
    onRemove: (e) => {
      if (
        e.from.id === 'persistent-tabs' ||
        e.from.classList.contains('tab-folder')
      ) {
        this.persistedTabs.set(this.sortablePersistedTabs);
      }
      if (e.from.id === 'ephemeral-tabs') {
        this.ephemeralTabs.set(this.sortableEphemeralTabs);
      }
    },
  };

  constructor() {
    effect(() => {
      this.sortablePersistedTabs = [...this.persistedTabs()];
    });

    effect(() => {
      this.sortableEphemeralTabs = [...this.ephemeralTabs()];
    });

    effect(() => {
      const activeTabId = this.activeTabId();
      if (!activeTabId) return;
      this.api.activateTab(activeTabId);
    });

    effect(() => {
      const activeTabId = this.activeTabId();
      if (!activeTabId) return;

      this.tabActivationOrderStack.push(activeTabId);
    });

    effect(() => {
      if (!this.tabsInitialized()) return;

      // Trigger effect (the actual values are retrieved in the debounced method)
      this.allPersistentTabs(), this.ephemeralTabs(), this.tabsChanged();
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabListApi>('tabsApi');
    this.workspacesApi = await loadBackendApi<WorkspacesApi>('workspacesApi');

    exposeApiToBackend({
      addTab: (tab: Tab, activate: boolean) => {
        this.ephemeralTabs.update((currentTabs) => [...currentTabs, tab]);

        if (activate) {
          this.activeTabId.set(tab.id);
        }
      },
      setTabs: (
        tabs: Tab[],
        activeTabId: TabId | undefined,
        ephemeralTabStartIndex: number,
        folders: Folder[]
      ) => {
        const persistedTabs = tabs.slice(0, ephemeralTabStartIndex);
        this.persistedTabs.set(
          this.createPersistedTabsWithFolders(persistedTabs, folders, true)
        );
        const ephemeralTabs = tabs.filter(
          (t, idx) => idx >= ephemeralTabStartIndex
        );
        this.ephemeralTabs.set(ephemeralTabs);
        this.activeTabId.set(activeTabId);
        this.tabsInitialized.set(true);
      },
      setActiveTab: (tabId: TabId) => {
        this.activeTabId.set(tabId);
      },
      updateFolders: (folders: Folder[]) => {
        this.persistedTabs.set(
          this.createPersistedTabsWithFolders(
            this.allPersistentTabs(),
            folders,
            false
          )
        );
      },
      updateTitle: (tabId: TabId, title: string | null) =>
        this.updateTab(tabId, { title }),
      updateFavicon: (tabId: TabId, favicon: string | null) =>
        this.updateTab(tabId, { favicon }),
      closeTab: (tabId: TabId, activateNext: boolean) =>
        this.closeTab(tabId, false, activateNext),
      toggleTabBookmark: (tabId: TabId) => this.toggleBookmark(tabId),
      setTabCustomizations: (customizations: TabCustomization[]) =>
        this.tabCustomizations.set(customizations),
      updateTabCustomization: (customization: TabCustomization) =>
        this.tabCustomizations.update((current) => [
          ...current.filter((c) => c.tabId != customization.tabId),
          customization,
        ]),
    });
  }

  getTabCustomization(tab: Tab) {
    return this.tabCustomizations().find((c) => c.tabId === tab.id);
  }

  private createPersistedTabsWithFolders(
    persistentTabs: Tab[],
    folders: Folder[],
    isFullUpdate: boolean
  ): (Tab | FolderDto)[] {
    const result: (Tab | FolderDto)[] = [];
    let currentFolder: FolderDto | undefined;
    persistentTabs.forEach((tab, idx) => {
      const folder = folders.find((f) => f.startIndex === idx);
      if (folder) {
        const isNewFolder =
          !isFullUpdate &&
          this.persistedTabs().every((x) => x.id !== folder.id);
        const isOpen =
          isNewFolder || (this.folderOpenState[folder.id] ?? false);
        currentFolder = {
          id: folder.id,
          name: folder.name,
          isOpen,
          tabs: [],
          isNew: isNewFolder,
        };
        result.push(currentFolder);
        if (isNewFolder) {
          this.folderOpenState[folder.id] = isOpen;
        }
      }
      if (
        currentFolder &&
        folders.find(
          (f) =>
            currentFolder!.id === f.id &&
            f.startIndex <= idx &&
            f.endIndex >= idx
        )
      ) {
        currentFolder.tabs.push(tab);
      } else {
        result.push(tab);
      }
    });

    return result;
  }

  private getFolderIndexInformation(): FolderIndexStateDto[] {
    const folders: FolderIndexStateDto[] = [];
    let startIndex = 0;

    this.persistedTabs().forEach((tabOrFolder) => {
      if (this.isFolder(tabOrFolder) && tabOrFolder.tabs.length > 0) {
        folders.push({
          Id: tabOrFolder.id,
          Name: tabOrFolder.name,
          StartIndex: startIndex,
          EndIndex: startIndex + tabOrFolder.tabs.length - 1,
        });
        startIndex += tabOrFolder.tabs.length;
      } else {
        startIndex++;
      }
    });

    return folders;
  }

  private updateTab(tabId: TabId, update: Partial<Tab>) {
    this.persistedTabs.update((currentTabs) => {
      return currentTabs.map((x) =>
        this.isFolder(x)
          ? {
              ...x,
              tabs: x.tabs.map((t) =>
                t.id === tabId ? { ...t, ...update } : t
              ),
            }
          : x.id === tabId
          ? { ...x, ...update }
          : x
      );
    });
    this.ephemeralTabs.update((currentTabs) => {
      return currentTabs.map((tab) =>
        tab.id === tabId ? { ...tab, ...update } : tab
      );
    });
  }

  private tabsChanged = debounce(() => {
    const tabs = [...this.allPersistentTabs(), ...this.ephemeralTabs()];
    const selectedTabId = this.activeTabId() ?? null;

    this.api.tabsChanged(
      tabs.map((tab) => ({
        Id: tab.id,
        Title: tab.title,
        Favicon: tab.favicon,
        IsActive: tab.id === selectedTabId,
        Created: tab.created,
      })),
      this.allPersistentTabs().length, // Ephemeral tab start index
      this.getFolderIndexInformation()
    );
  }, this.saveTabsDebounceDelay);

  closeTab(tabId: TabId, updateBackend = true, activateNext = true) {
    this.tabActivationOrderStack.remove(tabId);
    this.persistedTabs.update((currentTabs) =>
      currentTabs
        .filter((t) => t.id !== tabId)
        .map((x) =>
          this.isFolder(x)
            ? { ...x, tabs: x.tabs.filter((t) => t.id !== tabId) }
            : x
        )
    );
    this.ephemeralTabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );

    if (activateNext && this.activeTabId() === tabId) {
      const newSelectedTabId =
        this.persistedTabs().length == 0 && this.ephemeralTabs().length == 0
          ? undefined
          : this.tabActivationOrderStack.pop();

      this.activeTabId.set(newSelectedTabId);
    }

    if (updateBackend) {
      this.api.closeTab(tabId);
    }
  }

  isFolder(tab: Tab | FolderDto): tab is FolderDto {
    return 'tabs' in tab;
  }

  toggleBookmark(tabId: TabId) {
    const isBookmarked = this.persistedTabs().some((t) => t.id === tabId);
    if (isBookmarked) {
      const tab = this.allPersistentTabs().find((t) => t.id === tabId) as Tab;
      this.persistedTabs.update((tabsOrFolders) =>
        tabsOrFolders
          .filter((t) => t.id !== tabId)
          .map((x) =>
            this.isFolder(x)
              ? { ...x, tabs: x.tabs.filter((t) => t.id !== tabId) }
              : x
          )
      );
      this.ephemeralTabs.update((currentTabs) => [...currentTabs, tab]);
    } else {
      const tab = this.ephemeralTabs().find((t) => t.id === tabId)!;
      this.persistedTabs.update((currentTabs) => [...currentTabs, tab]);
      this.ephemeralTabs.update((currentTabs) =>
        currentTabs.filter((t) => t.id !== tabId)
      );
    }
  }

  toggleFolder(folderId: FolderId): void {
    this.persistedTabs.update((current) => {
      return current.map((x) => {
        if (this.isFolder(x) && x.id === folderId) {
          const newIsOpen = !x.isOpen;
          this.folderOpenState[folderId] = newIsOpen;
          return { ...x, isOpen: newIsOpen };
        }
        return x;
      });
    });
  }

  renameFolder(folderId: FolderId, newName: string): void {
    this.persistedTabs.update((current) => {
      return current.map((x) =>
        this.isFolder(x) && x.id === folderId ? { ...x, name: newName } : x
      );
    });
  }

  containsActiveTab(folder: FolderDto): boolean {
    return folder.tabs.some((t) => t.id === this.activeTabId());
  }

  returnToOriginal(tabId: TabId): void {
    this.workspacesApi.returnToOriginalAddress(tabId);
  }

  // If we just track by id, Angular can get confused when reordering items, causing layout issues
  getTrackingKey(tabOrFolder: Tab | FolderDto, index: number): string {
    return `${tabOrFolder.id}-${index}`;
  }
}
