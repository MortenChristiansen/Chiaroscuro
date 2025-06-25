import { Component, effect, OnInit, signal } from '@angular/core';
import { TabListApi } from './tabListApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';

export type TabId = string;

interface Tab {
  id: TabId;
  title: string | null;
  favicon: string | null;
}

@Component({
  selector: 'tabs-list',
  imports: [],
  template: `
    <div class="flex flex-col gap-2">
      @for (tab of tabs(); track tab.id) {
      <div
        class="tab flex items-center px-4 py-2 rounded-lg select-none shadow-sm text-white font-sans text-base transition-colors duration-200 hover:bg-white/10 {{
          tab.id === selectedTab()?.id ? 'bg-white/20 hover:bg-white/30' : ''
        }}"
        (click)="selectedTab.set(tab)"
      >
        @if (tab.favicon) {
        <img class="w-4 h-4 mr-2" [src]="tab.favicon" />
        } @else {
        <img class="w-4 h-4 mr-2" [src]="fallbackFavicon" />
        }
        <span class="truncate">{{ tab.title ?? 'Loading...' }}</span>
      </div>
      }
    </div>
  `,
})
export default class TabsListComponent implements OnInit {
  tabs = signal<Tab[]>([]);
  selectedTab = signal<Tab | null>(null);
  fallbackFavicon =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" rx="4" fill="%23bbb"/><text x="8" y="12" text-anchor="middle" font-size="10" fill="white" font-family="Arial">★</text></svg>';

  constructor() {
    effect(() => {
      const activeTab = this.selectedTab();
      if (!activeTab) return;
      this.api.activateTab(activeTab.id);
    });
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
}
