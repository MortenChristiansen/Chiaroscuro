import { Component } from '@angular/core';
import { TabTextSearchComponent } from './tab-text-search.component';
import { TabCustomizationEditorComponent } from './tab-customization-editor.component';

@Component({
  selector: 'tab-content',
  template: `
    <div class="flex flex-col gap-4">
      <div class="text-sm text-gray-300 font-semibold border-b border-gray-600 pb-2">
        Current Tab
      </div>
      <tab-text-search />
      <tab-customization-editor />
    </div>
  `,
  styles: ``,
  imports: [TabTextSearchComponent, TabCustomizationEditorComponent],
})
export class TabContentComponent {
}