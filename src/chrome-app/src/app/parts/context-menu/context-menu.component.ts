import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faCopy, faDownload } from '@fortawesome/free-solid-svg-icons';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { ContextMenuParameters } from './server-models';
import { ContextMenuApi } from './contextMenuApi';

@Component({
  selector: 'context-menu',
  template: `@let params = parameters();
    <div
      class="px-4 py-2 bg-gray-800 text-gray-300 font-semibold border-b border-gray-700 min-w-60 rounded-sm cursor-default select-none w-min h-min max-w-200 max-h-150"
    >
      @if (params.imageSourceUrl) {
      <div class="flex flex-col gap-3">
        <div class="flex items-center gap-3 min-w-0">
          <div class="flex-1 min-w-0 max-w-96">
            <div>Image URL:</div>
            <div
              class="text-gray-400 font-normal text-sm wrap-break-word line-clamp-3"
            >
              {{ params.imageSourceUrl }}
            </div>
          </div>
          <button
            type="button"
            (click)="copyLink(params.imageSourceUrl)"
            class="ml-2 flex h-8 w-8 items-center justify-center rounded-full bg-gray-700 text-gray-200 hover:bg-gray-600 transition active:scale-95"
            title="Copy image URL"
          >
            <fa-icon [icon]="copyIcon" />
          </button>
        </div>
        <div class="flex flex-wrap gap-2 mb-2">
          <button
            type="button"
            (click)="downloadImage(params.imageSourceUrl)"
            class="flex items-center gap-2 rounded-md bg-gray-700 px-3 py-2 text-sm font-semibold text-gray-200 transition hover:bg-gray-600 active:scale-95"
            title="Download image"
          >
            <fa-icon [icon]="downloadIcon" />
            <span>Download</span>
          </button>
          <button
            type="button"
            (click)="copyImage(params.imageSourceUrl)"
            class="flex items-center gap-2 rounded-md bg-gray-700 px-3 py-2 text-sm font-semibold text-gray-200 transition hover:bg-gray-600 active:scale-95"
            title="Copy image"
          >
            <fa-icon [icon]="copyIcon" />
            <span>Copy</span>
          </button>
        </div>
      </div>
      } @else if (params.linkUrl) {
      <div class="flex items-center gap-3 min-w-0">
        <div class="flex-1 min-w-0 max-w-96">
          <div>Link URL:</div>
          <div
            class="text-gray-400 font-normal text-sm wrap-break-word line-clamp-3"
          >
            {{ params.linkUrl }}
          </div>
        </div>
        <button
          type="button"
          (click)="copyLink(params.linkUrl)"
          class="ml-2 flex h-8 w-8 items-center justify-center rounded-full bg-gray-700 text-gray-200 hover:bg-gray-600 transition active:scale-95"
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
  protected readonly downloadIcon = faDownload;

  private api!: ContextMenuApi;

  async ngOnInit() {
    this.api = await loadBackendApi<ContextMenuApi>('api');

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
    await this.api.dismissContextMenu();
  }

  async downloadImage(imageUrl?: string) {
    if (!imageUrl) {
      return;
    }

    await this.api.dismissContextMenu();
    await this.api.downloadImage(imageUrl);
  }

  async copyImage(imageUrl?: string) {
    if (!imageUrl) {
      return;
    }

    await this.api.dismissContextMenu();
    await this.api.copyImage(imageUrl);
  }
}
