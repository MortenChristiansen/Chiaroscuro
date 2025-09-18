import { Component, OnInit, signal } from '@angular/core';
import { PinnedTabsApi } from './pinnedTabsApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { PinnedTab, TabCustomization, TabId } from './server-models';
import { FaviconComponent } from '../../shared/favicon.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'pinned-tabs-list',
  imports: [FaviconComponent, CommonModule],
  template: `
    <div class="pinned-tabs-container">
      <div class="pinned-tabs-list flex flex-row gap-2 w-full">
        @for (tab of pinnedTabs(); track tab.id) {
        <div
          class="pinned-tab flex items-center justify-center h-8 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 bg-white/5 hover:bg-white/10 relative flex-1 min-w-0"
          [ngClass]="{
            'bg-white/15 hover:bg-white/30': activeTabId() == tab.id,
          }"
          (click)="api.activateTab(tab.id)"
          [attr.title]="
            getTabCustomization(tab)?.customTitle ?? tab.title ?? 'Loading...'
          "
        >
          <favicon [src]="tab.favicon" class="w-5 h-5" />
          <button
            class="close-button absolute top-0 right-0 opacity-0 hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-0.5 rounded bg-gray-900/80"
            (click)="$event.stopPropagation(); api.unpinTab(tab.id)"
            aria-label="Unpin tab"
            tabindex="-1"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 20 20"
              stroke-width="2"
              stroke="currentColor"
              class="w-3 h-3"
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
    </div>
  `,
  styles: ``,
})
export class PinnedTabsListComponent implements OnInit {
  pinnedTabs = signal<PinnedTab[]>([]);
  activeTabId = signal<string | null>(null);
  tabCustomizations = signal<TabCustomization[]>([]);

  api!: PinnedTabsApi;

  async ngOnInit() {
    this.api = await loadBackendApi<PinnedTabsApi>('pinnedTabsApi');

    exposeApiToBackend({
      setPinnedTabs: (tabs: PinnedTab[], activeTabId: string | null) => {
        this.pinnedTabs.set(tabs);
        this.activeTabId.set(activeTabId);
      },
      updateTitle: (tabId: TabId, title: string | null) =>
        this.updateTab(tabId, { title }),
      updateFavicon: (tabId: TabId, favicon: string | null) =>
        this.updateTab(tabId, { favicon }),
      setTabCustomizations: (customizations: TabCustomization[]) =>
        this.tabCustomizations.set(customizations),
      updateTabCustomization: (customization: TabCustomization) =>
        this.tabCustomizations.update((current) => [
          ...current.filter((c) => c.tabId != customization.tabId),
          customization,
        ]),
    });
  }

  getTabCustomization(tab: PinnedTab) {
    return this.tabCustomizations().find((c) => c.tabId === tab.id);
  }

  private updateTab(tabId: TabId, updates: Partial<PinnedTab>) {
    this.pinnedTabs.update((tabs) =>
      tabs.map((tab) => (tab.id === tabId ? { ...tab, ...updates } : tab))
    );
  }
}
