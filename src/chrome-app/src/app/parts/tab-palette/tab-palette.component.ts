import { Component } from '@angular/core';
import { TabTextSearchComponent } from './tab-text-search.component';
import { TabCustomizationEditorComponent } from './tab-customization-editor.component';

@Component({
  selector: 'tab-palette',
  template: `
    <div
      class="w-full h-full flex-1 min-h-0 bg-gray-800 rounded-lg shadow-lg p-4 flex flex-col gap-4 border border-gray-700"
      style="position: relative; top: 0; left: 0;right: 0; bottom: 0;"
    >
      <tab-text-search />
      <tab-customization-editor />
    </div>
  `,
  styles: ``,
  imports: [TabTextSearchComponent, TabCustomizationEditorComponent],
})
export default class TabPaletteComponent {
  // This component is currently empty and serves as a placeholder.
  // It can be extended in the future to provide additional functionality or UI elements related to tab management.
}
