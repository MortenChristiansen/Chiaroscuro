import { Component } from '@angular/core';
import { TabContentComponent } from './tab-content.component';
import { DomainContentComponent } from './domain-content.component';

@Component({
  selector: 'tab-palette',
  template: `
    <div
      class="w-full h-full flex-1 min-h-0 p-4 flex flex-col gap-6"
      style="position: relative; top: 0; left: 0;right: 0; bottom: 0;"
    >
      <tab-content />
      <div class="border-t border-gray-600"></div>
      <domain-content />
    </div>
  `,
  styles: ``,
  imports: [TabContentComponent, DomainContentComponent],
})
export default class TabPaletteComponent {
  // This component serves as the main container for tab and domain-specific functionality.
}
