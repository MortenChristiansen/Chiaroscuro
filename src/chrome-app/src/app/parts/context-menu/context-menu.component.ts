import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faCopy } from '@fortawesome/free-solid-svg-icons';
import { exposeApiToBackend } from '../interfaces/api';
import { ContextMenuParameters } from './server-models';

@Component({
  selector: 'context-menu',
  template: `@let params = parameters();
    <div
      class="px-4 py-2 bg-gray-800 text-gray-300 font-semibold border-b border-gray-700 min-w-60 rounded-sm cursor-default select-none w-min h-min max-w-200 max-h-150 flex items-center gap-3"
    >
      @if (params.linkUrl) {
      <div class="flex items-center gap-3 min-w-0">
        <div class="flex-1 w-max">
          <div>Link URL:</div>
          <div class="text-gray-400 font-normal text-sm break-all">
            {{ params.linkUrl }}
          </div>
        </div>
        <button
          type="button"
          (click)="copyLink(params.linkUrl)"
          class="ml-2 flex h-8 w-8 items-center justify-center rounded-full bg-gray-700 text-gray-200 hover:bg-gray-600 focus:outline-none transition active:scale-95"
          aria-label="Copy link URL"
          title="Copy link URL"
        >
          <fa-icon [icon]="copyIcon" />
        </button>
      </div>
      } @else {
      <div>No context available.</div>
      }
    </div>`,
  imports: [FaIconComponent],
})
export default class ContextMenuComponent implements OnInit {
  protected parameters = signal<ContextMenuParameters>({});
  protected readonly copyIcon = faCopy;

  ngOnInit() {
    exposeApiToBackend({
      setParameters: (parameters: ContextMenuParameters) => {
        this.parameters.set(parameters);
      },
    });
  }

  async copyLink(link?: string) {
    if (!link) {
      return;
    }

    await navigator.clipboard.writeText(link);
  }
}
