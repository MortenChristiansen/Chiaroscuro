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
    @for (tab of tabs(); track tab.id) {
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
  tabs = signal<Tab[]>([]);
  activeTabId = signal<TabId | undefined>(undefined);
  ephemeralTabStartIndex = signal<number>(0); // Can this be made into a computed property?
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
      const currentTabs = this.tabs();
      this.tabsChanged(currentTabs, this.activeTabId() ?? null);
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabListApi>('tabsApi');

    exposeApiToBackend({
      addTab: (tab: Tab, activate: boolean) => {
        this.tabs.update((currentTabs) => [...currentTabs, tab]);

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
        this.tabs.set(tabs);
        this.activeTabId.set(activeTabId);
        this.ephemeralTabStartIndex.set(ephemeralTabStartIndex);
        this.tabsInitialized.set(true);
      },
      updateTitle: (tabId: TabId, title: string | null) =>
        this.updateTab(tabId, { title }),
      updateFavicon: (tabId: TabId, favicon: string | null) =>
        this.updateTab(tabId, { favicon }),
      closeTab: (tabId: TabId) => this.closeTab(tabId, false),
      toggleTabBookmark: (tabId: TabId) => {},
    });
  }

  private updateTab(tabId: TabId, update: Partial<Tab>) {
    this.tabs.update((currentTabs) => {
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
      this.ephemeralTabStartIndex(),
      [] // Handle folders
    );
  }, this.saveTabsDebounceDelay);

  closeTab(tabId: TabId, updateBackend = true) {
    this.tabActivationOrderStack = this.tabActivationOrderStack.filter(
      (id) => id !== tabId
    );
    this.tabs.update((currentTabs) =>
      currentTabs.filter((t) => t.id !== tabId)
    );

    if (this.activeTabId() === tabId) {
      const newSelectedTabId =
        this.tabs().length == 0
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
}
