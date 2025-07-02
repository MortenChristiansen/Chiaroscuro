import { Component } from '@angular/core';
import TabsListComponent from './tabs-list.component';
import DownloadsListComponent from './downloads-list.component';

@Component({
  selector: 'action-context',
  imports: [TabsListComponent, DownloadsListComponent],
  template: `
    <div class="flex flex-col h-full">
      <tabs-list class="flex-shrink-0"></tabs-list>
      <div class="flex-1 flex flex-col justify-end">
        <downloads-list class="flex-shrink-0"></downloads-list>
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
