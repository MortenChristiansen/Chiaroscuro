import { Component, OnInit } from '@angular/core';
import { DownloadsService } from './downloads.service';
import { DownloadItem } from './fileDownloadApi';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'downloads-list',
  template: `
    @if (downloadsService.downloads().length > 0) {
      <div class="downloads-container" @slideIn>
        <h3 class="downloads-title text-white font-sans text-sm font-semibold mb-2 px-4">Downloads</h3>
        <div class="downloads-list flex flex-col gap-1">
          @for (download of downloadsService.downloads(); track download.id) {
            <div class="download-item flex items-center px-4 py-2 text-white font-sans text-sm hover:bg-white/5 transition-colors duration-200" @itemSlideIn>
              <div class="download-info flex-1 flex items-center gap-2 min-w-0">
                <span class="file-name truncate">{{ download.fileName }}</span>
                @if (!download.isCompleted) {
                  <div class="loading-indicator">
                    <div class="spinner w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                  </div>
                } @else {
                  <div class="completed-indicator">
                    <svg class="w-4 h-4 text-green-400" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"></path>
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
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
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
    .downloads-container {
      border-top: 1px solid rgba(255, 255, 255, 0.1);
      padding-top: 8px;
      margin-top: 8px;
    }

    .downloads-title {
      color: rgba(255, 255, 255, 0.8);
    }

    .download-item {
      font-size: 13px;
    }

    .spinner {
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }
  `,
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-10px)' }))
      ])
    ]),
    trigger('itemSlideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(-10px)' }),
        animate('150ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'translateX(-10px)' }))
      ])
    ])
  ]
})
export default class DownloadsListComponent implements OnInit {
  constructor(public downloadsService: DownloadsService) {}

  async ngOnInit() {
    await this.downloadsService.initialize();
  }

  async cancelDownload(downloadId: string) {
    await this.downloadsService.cancelDownload(downloadId);
  }
}