import { Component, effect, OnInit, signal } from '@angular/core';
import { TabListApi } from './tabListApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import {
  CdkDragDrop,
  moveItemInArray,
  DragDropModule,
} from '@angular/cdk/drag-drop';

export type TabId = string;

interface Tab {
  id: TabId;
  title: string | null;
  favicon: string | null;
}

@Component({
  selector: 'tabs-list',
  imports: [DragDropModule],
  template: `
    <div
      class="flex flex-col gap-2"
      cdkDropList
      (cdkDropListDropped)="drop($event)"
    >
      @for (tab of tabs(); track tab.id) {
      <div
        class="tab group flex items-center px-4 py-2 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 hover:bg-white/10 {{
          tab.id === selectedTab()?.id ? 'bg-white/20 hover:bg-white/30' : ''
        }} cdkDrag"
        (click)="selectedTab.set(tab)"
        cdkDrag
        [cdkDragData]="tab"
      >
        @if (tab.favicon) {
        <img class="w-4 h-4 mr-2" [src]="tab.favicon" />
        } @else {
        <img class="w-4 h-4 mr-2" [src]="fallbackFavicon" />
        }
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
      }
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
  selectedTab = signal<Tab | null>(null);
  fallbackFavicon =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" rx="4" fill="%23bbb"/><text x="8" y="12" text-anchor="middle" font-size="10" fill="white" font-family="Arial">★</text></svg>';

  private tabsChangedTimeout: any = null;

  constructor() {
    effect(() => {
      const activeTab = this.selectedTab();
      if (!activeTab) return;
      this.api.activateTab(activeTab.id);
    });

    effect(() => {
      const currentTabs = this.tabs();
      const selectedTab = this.selectedTab();
      if (this.tabsChangedTimeout) {
        clearTimeout(this.tabsChangedTimeout);
      }
      this.tabsChangedTimeout = setTimeout(() => {
        this.api.tabsChanged(
          currentTabs.map((tab) => ({
            Address: tab.id,
            Title: tab.title,
            Favicon: tab.favicon,
            IsActive: tab.id === selectedTab?.id,
          }))
        );
      }, 3000);
    });
  }

  drop(event: CdkDragDrop<any>) {
    const currentTabs = [...this.tabs()];
    moveItemInArray(currentTabs, event.previousIndex, event.currentIndex);
    this.tabs.set(currentTabs);
    this.api.reorderTab(event.item.data.id, event.currentIndex);
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabListApi>();

    exposeApiToBackend({
      addTab: (tab: Tab, activate: boolean) => {
        console.log('Adding tab:', JSON.stringify(tab), 'Activate:', activate);
        this.tabs.update((currentTabs) => [...currentTabs, tab]);

        if (activate) {
          this.selectedTab.set(tab);
          console.log('Activated tab:', JSON.stringify(tab));
        }
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
      closeTab: (tabId: TabId, focusedTabId: TabId | null) => {
        this.tabs.update((currentTabs) =>
          currentTabs.filter((t) => t.id !== tabId)
        );
        const tab = this.tabs().find((t) => t.id === focusedTabId) ?? null;
        this.selectedTab.set(tab);
      },
    });

    await this.api.uiLoaded();
  }

  api!: TabListApi;

  close(tabId: TabId) {
    const currentTabIndex = this.tabs().findIndex((t) => t.id === tabId);
    this.tabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );

    if (this.selectedTab()?.id === tabId) {
      const newSelectedTab =
        this.tabs().length == 0
          ? null
          : this.tabs()[Math.max(0, currentTabIndex - 1)];
      this.selectedTab.set(newSelectedTab);
    }

    this.api.closeTab(tabId);
  }
}
