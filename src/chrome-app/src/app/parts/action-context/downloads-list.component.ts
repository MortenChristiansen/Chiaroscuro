import { Component, OnInit, signal } from '@angular/core';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { DownloadItem, FileDownloadsApi } from './fileDownloadsApi';

@Component({
  selector: 'downloads-list',
  template: `
    @if (downloads().length > 0) {
    <div class="downloads-container">
      <h3
        class="downloads-title text-white font-sans text-sm font-semibold mb-2 px-4"
      >
        Downloads
      </h3>
      <div class="downloads-list flex flex-col gap-1">
        @for (download of downloads(); track download.id) {
        <div
          class="download-item flex items-center px-4 py-2 text-gray-300 font-sans text-sm hover:bg-white/5 transition-colors duration-200"
        >
          <div class="download-info flex-1 flex items-center gap-2 min-w-0">
            <span class="file-name truncate">{{
              download.fileName ? download.fileName : 'Unnamed'
            }}</span>
            @if (!download.isCompleted && !download.isCancelled) {
            <div class="loading-indicator flex items-center gap-2">
              <div
                class="spinner w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"
              ></div>
              <span>{{ download.progress }}%</span>
            </div>
            } @else if (download.isCompleted) {
            <div class="completed-indicator">
              <svg
                class="w-4 h-4 text-green-400"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  fill-rule="evenodd"
                  d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                  clip-rule="evenodd"
                ></path>
              </svg>
            </div>
            } @else if (download.isCancelled) {
            <div class="cancelled-indicator">
              <svg
                class="w-4 h-4 text-red-400"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  fill-rule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-9a1 1 0 11-2 0V7a1 1 0 012 0v2zm-1 4a1.5 1.5 0 100-3 1.5 1.5 0 000 3z"
                  clip-rule="evenodd"
                ></path>
              </svg>
            </div>
            }
          </div>
          @if (!download.isCompleted && !download.isCancelled) {
          <button
            class="cancel-button p-1 rounded hover:bg-white/10 transition-colors duration-150 text-gray-400 hover:text-gray-300"
            (click)="cancelDownload(download.id)"
            aria-label="Cancel download"
          >
            <svg
              class="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              ></path>
            </svg>
          </button>
          }
        </div>
        }
      </div>
    </div>
    }
  `,
  styles: `
  `,
})
export default class DownloadsListComponent implements OnInit {
  downloads = signal<DownloadItem[]>([]);

  api!: FileDownloadsApi;

  async ngOnInit() {
    this.api = await loadBackendApi<FileDownloadsApi>('fileDownloadsApi');

    exposeApiToBackend({
      downloadsChanged: (downloads: DownloadItem[]) => {
        console.log('Downloads updated:', downloads);
        this.downloads.set(downloads);
      },
    });
  }

  cancelDownload(id: number) {
    console.log('Cancelling download with ID:', id, typeof id);
    this.api.cancelDownload(id);
  }
}
