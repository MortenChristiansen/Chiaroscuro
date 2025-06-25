import { Component, OnInit, signal } from '@angular/core';
import { Api } from '../interfaces/api';

interface Tab {
  id: string;
  title: string | null;
  favicon: string | null;
}

@Component({
  selector: 'tabs-list',
  imports: [],
  template: `
    <div class="tabs">
      @for (tab of tabs(); track tab.id) {
      <div
        class="tab {{ tab.id === selectedTab()?.id ? 'active' : '' }}"
        (click)="selectedTab.set(tab)"
      >
        @if (tab.favicon) {
        <img class="favicon" [src]="tab.favicon" />
        } @else {
        <img class="favicon" [src]="fallbackFavicon" />
        }
        {{ tab.title ?? 'Loading...' }}
      </div>
      }
    </div>
  `,
  styles: `
    .tabs {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .tab {
        padding: 0.5rem 1rem;
        background: rgba(255, 255, 255, 0.1);
        border-radius: 0.5rem;
        cursor: pointer;
        transition: background 0.3s ease;
        color: #fff;
        font-family: Arial, sans-serif;
        font-size: 1rem;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        user-select: none;
    }

    .tab:hover {
        background: rgba(255, 255, 255, 0.2);
    }

    .tab.active {
        background: rgba(255, 255, 255, 0.3);
    }

    .favicon {
        width: 16px;
        height: 16px;   
        margin-right: 0.5rem;
    }
  `,
})
export default class TabsListComponent implements OnInit {
  tabs = signal<Tab[]>([]);
  selectedTab = signal<Tab | null>(null);
  fallbackFavicon =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" rx="4" fill="%23bbb"/><text x="8" y="12" text-anchor="middle" font-size="10" fill="white" font-family="Arial">★</text></svg>';

  async ngOnInit() {
    await (window as any).CefSharp.BindObjectAsync('api');
    this.api = (window as any).api;

    (window as any).angularApi = {
      addTab: (tab: Tab, activate: boolean) => {
        console.log('Adding tab:', JSON.stringify(tab), 'Activate:', activate);
        this.tabs.update((currentTabs) => [...currentTabs, tab]);

        if (activate) {
          this.selectedTab.set(tab);
          console.log('Activated tab:', JSON.stringify(tab));
        }
      },
      updateTab: (tab: Tab) => {
        this.tabs.update((currentTabs) => {
          const index = currentTabs.findIndex((t) => t.id === tab.id);
          if (index !== -1) {
            const updatedTabs = [...currentTabs];
            updatedTabs[index] = tab;
            return updatedTabs;
          }
          return currentTabs;
        });
      },
    };

    await this.api.uiLoaded('Tabs');
  }

  api!: Api;
}
