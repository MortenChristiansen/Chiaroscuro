import { Component } from '@angular/core';
import TabsListComponent from './tabs-list.component';

@Component({
  selector: 'action-context',
  imports: [TabsListComponent],
  template: `
    <div class="actions-host flex flex-col h-full">
      <tabs-list class="flex-shrink-0"></tabs-list>
    </div>
  `,
})
export default class ActionContextComponent {}
