import { Component } from '@angular/core';
import DownloadsListComponent from './downloads-list.component';
import WorkspaceSwitcherComponent from './workspace-switcher.component';
import { TabsListComponent } from './tabs-list2.component';

@Component({
  selector: 'action-context',
  imports: [
    TabsListComponent,
    DownloadsListComponent,
    WorkspaceSwitcherComponent,
  ],
  template: `
    <div class="flex flex-col h-full">
      <tabs-list2 class="flex-shrink-0" />
      <div class="flex-1 flex flex-col justify-end">
        <downloads-list class="flex-shrink-0" />
        <workspace-switcher class="flex-shrink-0" />
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
