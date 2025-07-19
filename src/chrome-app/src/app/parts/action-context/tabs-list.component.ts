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

export type TabId = string;

interface Tab {
  id: TabId;
  title: string | null;
  favicon: string | null;
  created: Date;
}

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
      @for (tab of tabs(); track tab.id) { @if ($index === ephemeralIndex) {
      <div
        cdkDrag
        style="pointer-events: none;"
        class="w-full h-0.5 my-2 bg-gradient-to-r from-transparent via-gray-500 to-transparent opacity-60 rounded-full"
      ></div>
      }

      <div
        class="tab group flex items-center px-4 py-2 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 hover:bg-white/10 {{
          tab.id === selectedTab()?.id ? 'bg-white/20 hover:bg-white/30' : ''
        }} cdkDrag"
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

      @if ($index === tabs().length - 1 && $index +1 === ephemeralIndex) {
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
  ephemeralTabStartIndex = signal<number>(0);
  tabsInitialized = signal(false);
  selectedTab = signal<Tab | null>(null);
  private saveTabsDebounceDelay = 1000;
  private tabActivationOrderStack: TabId[] = [];

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
      this.tabsChanged(currentTabs, this.selectedTab()?.id ?? null);
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
      this.ephemeralTabStartIndex()
    );
  }, this.saveTabsDebounceDelay);

  drop(event: CdkDragDrop<any>) {
    const currentTabs = [...this.tabs()];
    const ephemeralIndex = this.ephemeralTabStartIndex();

    const { adjustedCurrentIndex, adjustedPreviousIndex } = this.adjustDragIndices(
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
        ephemeralTabStartIndex: number
      ) => {
        this.tabs.set(tabs);
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
}
