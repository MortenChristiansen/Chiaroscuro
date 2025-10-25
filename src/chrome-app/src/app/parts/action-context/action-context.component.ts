import { Component } from '@angular/core';
import DownloadsListComponent from './downloads-list.component';
import WorkspaceSwitcherComponent from './workspace-switcher.component';
import { TabsListComponent } from './tabs-list.component';
import { PinnedTabsListComponent } from './pinned-tabs-list.component';

@Component({
  selector: 'action-context',
  imports: [
    TabsListComponent,
    DownloadsListComponent,
    WorkspaceSwitcherComponent,
    PinnedTabsListComponent,
  ],
  template: `
    <div class="flex flex-col h-full">
      <pinned-tabs-list class="shrink-0" />
      <tabs-list class="shrink-0" />
      <div class="flex-1 flex flex-col justify-end">
        <downloads-list class="shrink-0" />
        <workspace-switcher class="shrink-0" />
      </div>
    </div>
  `,
  styles: `
    :host {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
    }
  `,
})
export default class ActionContextComponent {}
