import { Component } from '@angular/core';
import { DomainCssEditorComponent } from './domain-css-editor.component';

@Component({
  selector: 'domain-content',
  template: `
    <div class="flex flex-col gap-4">
      <div class="text-sm text-gray-300 font-semibold border-b border-gray-600 pb-2">
        Domain Settings
      </div>
      <domain-css-editor />
    </div>
  `,
  styles: ``,
  imports: [DomainCssEditorComponent],
})
export class DomainContentComponent {
}