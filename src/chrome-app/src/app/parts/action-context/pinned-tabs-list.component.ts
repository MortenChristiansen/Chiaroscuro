import { Component, OnInit, signal } from '@angular/core';
import { PinnedTabsApi } from './pinnedTabsApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { PinnedTab } from './server-models';
import { FaviconComponent } from '../../shared/favicon.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'pinned-tabs-list',
  imports: [FaviconComponent, CommonModule],
  template: `
    <div class="pinned-tabs-container py-3">
      <h3
        class="pinned-tabs-title text-white font-sans text-sm font-semibold mb-2 px-4"
      >
        Pinned Tabs
      </h3>
      <div class="pinned-tabs-list flex flex-col gap-1">
        @for (tab of pinnedTabs(); track tab.id) {
        <div
          class="pinned-tab flex items-center px-4 py-2 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 hover:bg-white/10"
          [ngClass]="{
            'bg-white/20 hover:bg-white/30': activeTabId() == tab.id,
          }"
        >
          <favicon [src]="tab.favicon" class="drag-handle w-4 h-4 mr-2" />
          <span class="truncate flex-1">{{ tab.title ?? 'Loading...' }}</span>
          <button
            class="close-button opacity-0 hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-1 rounded"
            (click)="api.unpinTab(tab.id)"
            aria-label="Unpin tab"
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
    </div>
  `,
  styles: ``,
})
export class PinnedTabsListComponent implements OnInit {
  pinnedTabs = signal<PinnedTab[]>([]);
  activeTabId = signal<string | null>(null);

  api!: PinnedTabsApi;

  async ngOnInit() {
    this.api = await loadBackendApi<PinnedTabsApi>('pinnedTabsApi');

    exposeApiToBackend({
      setPinnedTabs: (tabs: PinnedTab[], activeTabId: string | null) => {
        console.log('Pinned tabs updated:', tabs);
        this.pinnedTabs.set(tabs);
        this.activeTabId.set(activeTabId);
      },
    });
  }
}
