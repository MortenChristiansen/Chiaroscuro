import { Component } from '@angular/core';
import TabsListComponent from './tabs-list.component';
import DownloadsListComponent from './downloads-list.component';

@Component({
  selector: 'action-context',
  imports: [TabsListComponent, DownloadsListComponent],
  template: `
    <div class="actions-host flex flex-col h-full">
      <tabs-list class="flex-shrink-0"></tabs-list>
      <downloads-list class="flex-shrink-0 mt-auto"></downloads-list>
    </div>
  `,
})
export default class ActionContextComponent {}
