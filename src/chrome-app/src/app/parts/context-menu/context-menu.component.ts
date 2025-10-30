import { Component, OnInit, signal } from '@angular/core';
import { exposeApiToBackend } from '../interfaces/api';
import { ContextMenuParameters } from './server-models';

@Component({
  selector: 'context-menu',
  template: `@let params = parameters();
    <div
      class="px-4 py-2 bg-gray-800 text-gray-300 font-semibold border-b border-gray-700 min-w-60 rounded-sm cursor-default select-none w-min h-min max-w-200 max-h-150"
    >
      @if(params.linkUrl) { Link URL: {{ params.linkUrl }}
      } @else { No context available. }
    </div>`,
})
export default class ContextMenuComponent implements OnInit {
  protected parameters = signal<ContextMenuParameters>({});

  ngOnInit() {
    exposeApiToBackend({
      setParameters: (parameters: ContextMenuParameters) => {
        this.parameters.set(parameters);
      },
    });
  }
}
