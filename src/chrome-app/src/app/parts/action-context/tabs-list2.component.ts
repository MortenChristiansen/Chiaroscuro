import { DragDropModule } from '@angular/cdk/drag-drop';
import { Component, effect, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabListApi } from './tabListApi';
import { Folder, Tab, TabId } from './server-models';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabsListTabComponent } from './tabs-list-tab.component';
import { debounce } from '../../shared/utils';

@Component({
  selector: 'tabs-list2',
  imports: [DragDropModule, CommonModule, TabsListTabComponent],
  template: `
    <span
      class="bookmark-label text-gray-500 text-xs px-4"
      style="pointer-events: none;"
    >
      Bookmarks
    </span>
    @for (tab of persistedTabs(); track tab.id) {
    <tabs-list-tab
      [tab]="tab"
      [isActive]="tab.id == activeTabId()"
      [inFolder]="false"
      (selectTab)="activeTabId.set(tab.id)"
      (closeTab)="closeTab(tab.id, true)"
    />
    }
    <div
      class="w-full h-0.5 my-2 bg-gradient-to-r from-gray-700 via-gray-500 to-gray-700 opacity-60 rounded-full"
      style="pointer-events: none;"
    ></div>
    @for (tab of ephemeralTabs(); track tab.id) {
    <tabs-list-tab
      [tab]="tab"
      [isActive]="tab.id == activeTabId()"
      [inFolder]="false"
      (selectTab)="activeTabId.set(tab.id)"
      (closeTab)="closeTab(tab.id, true)"
    />
    }
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
export class TabsListComponent implements OnInit {
  persistedTabs = signal<Tab[]>([]);
  ephemeralTabs = signal<Tab[]>([]);
  activeTabId = signal<TabId | undefined>(undefined);
  tabsInitialized = signal(false);
  private saveTabsDebounceDelay = 1000;
  private tabActivationOrderStack: TabId[] = [];

  api!: TabListApi;

  constructor() {
    effect(() => {
      const activeTabId = this.activeTabId();
      if (!activeTabId) return;
      this.api.activateTab(activeTabId);
    });

    effect(() => {
      const activeTabId = this.activeTabId();
      if (!activeTabId) return;

      this.tabActivationOrderStack = [
        ...this.tabActivationOrderStack.filter((id) => id !== activeTabId),
        activeTabId,
      ];
    });

    effect(() => {
      if (!this.tabsInitialized()) return;
      const currentTabs = [...this.persistedTabs(), ...this.ephemeralTabs()];
      this.tabsChanged(currentTabs, this.activeTabId() ?? null);
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabListApi>('tabsApi');

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
        const persistedTabs = tabs.filter(
          (t, idx) => idx < ephemeralTabStartIndex
        );
        this.persistedTabs.set(persistedTabs);
        const ephemeralTabs = tabs.filter(
          (t, idx) => idx >= ephemeralTabStartIndex
        );
        this.ephemeralTabs.set(ephemeralTabs);
        this.activeTabId.set(activeTabId);
        this.tabsInitialized.set(true);
      },
      updateTitle: (tabId: TabId, title: string | null) =>
        this.updateTab(tabId, { title }),
      updateFavicon: (tabId: TabId, favicon: string | null) =>
        this.updateTab(tabId, { favicon }),
      closeTab: (tabId: TabId) => this.closeTab(tabId, false),
      toggleTabBookmark: (tabId: TabId) => this.toggleBookmark(tabId),
    });
  }

  private updateTab(tabId: TabId, update: Partial<Tab>) {
    this.persistedTabs.update((currentTabs) => {
      return currentTabs.map((tab) =>
        tab.id === tabId ? { ...tab, ...update } : tab
      );
    });
    this.ephemeralTabs.update((currentTabs) => {
      return currentTabs.map((tab) =>
        tab.id === tabId ? { ...tab, ...update } : tab
      );
    });
  }

  private tabsChanged = debounce((tabs: Tab[], selectedTabId: TabId | null) => {
    this.api.tabsChanged(
      tabs.map((tab) => ({
        Id: tab.id,
        Title: tab.title,
        Favicon: tab.favicon,
        IsActive: tab.id === selectedTabId,
        Created: tab.created,
      })),
      this.persistedTabs().length, // Ephemeral tab start index
      [] // Handle folders
    );
  }, this.saveTabsDebounceDelay);

  closeTab(tabId: TabId, updateBackend = true) {
    this.tabActivationOrderStack = this.tabActivationOrderStack.filter(
      (id) => id !== tabId
    );
    this.persistedTabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );
    this.ephemeralTabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );

    if (this.activeTabId() === tabId) {
      const newSelectedTabId =
        this.persistedTabs().length == 0 && this.ephemeralTabs().length == 0
          ? undefined
          : this.tabActivationOrderStack[
              this.tabActivationOrderStack.length - 1
            ];

      this.activeTabId.set(newSelectedTabId);
    }

    if (updateBackend) {
      this.api.closeTab(tabId);
    }
  }

  toggleBookmark(tabId: TabId) {
    const isBookmarked = this.persistedTabs().some((t) => t.id === tabId);
    if (isBookmarked) {
      const tab = this.persistedTabs().find((t) => t.id === tabId)!;
      this.persistedTabs.update((currentTabs) =>
        currentTabs.filter((t) => t.id !== tabId)
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
}
