import { Component, OnInit, signal } from '@angular/core';
import { Api, exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { WindowsChromeApi } from './windowChromeApi';

@Component({
  selector: 'window-chrome',
  imports: [],
  template: `
    <div class="flex items-center gap-4 align-middle px-2 py-1 select-none">
      <div class="address-bar flex flex-1 items-center gap-2 justify-center">
        <button
          (click)="back()"
          [disabled]="!canGoBack()"
          class="icon-btn"
          aria-label="Back"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            class="w-5 h-5 rotate-180"
          >
            <path
              fill-rule="evenodd"
              d="M12.293 15.707a1 1 0 010-1.414L15.586 11H4a1 1 0 110-2h11.586l-3.293-3.293a1 1 0 111.414-1.414l5 5a1 1 0 010 1.414l-5 5a1 1 0 01-1.414 0z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
        <button
          (click)="forward()"
          [disabled]="!canGoForward()"
          class="icon-btn"
          aria-label="Forward"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            class="w-5 h-5"
          >
            <path
              fill-rule="evenodd"
              d="M12.293 15.707a1 1 0 010-1.414L15.586 11H4a1 1 0 110-2h11.586l-3.293-3.293a1 1 0 111.414-1.414l5 5a1 1 0 010 1.414l-5 5a1 1 0 01-1.414 0z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
        <button (click)="reload()" class="icon-btn" aria-label="Reload">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            class="w-5 h-5"
          >
            <path
              d="M4 10a6 6 0 1 1 2.2 4.6"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <polyline
              points="5 17 5 13 9 13"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </button>
        <button
          (click)="copyAddress()"
          class="icon-btn"
          aria-label="Copy address"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 20 20"
            stroke-width="2"
            stroke="currentColor"
            class="w-5 h-5"
          >
            <rect x="7" y="7" width="9" height="9" rx="2" />
            <rect
              x="4"
              y="4"
              width="9"
              height="9"
              rx="2"
              fill="none"
              stroke-dasharray="2 2"
            />
          </svg>
        </button>
        <span
          class="address mx-4 font-sans text-base text-gray-200 max-w-[400px] truncate"
        >
          @let url = address(); @if (url === null) {
          <span class="italic text-gray-400">Press ctrl-t to start</span>
          } @else {
          <span>{{ url }}</span>
          }
        </span>
      </div>
      <div class="window-controls flex gap-2 ml-auto">
        <button (click)="min()" class="icon-btn" aria-label="Minimize">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="2"
            stroke="currentColor"
            class="w-5 h-5"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M6 18L18 18"
            />
          </svg>
        </button>
        <button (click)="max()" class="icon-btn" aria-label="Maximize">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="2"
            stroke="currentColor"
            class="w-5 h-5"
          >
            <rect x="6" y="6" width="12" height="12" rx="2" />
          </svg>
        </button>
        <button
          (click)="close()"
          class="icon-btn text-red-400 hover:text-red-600"
          aria-label="Close"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="2"
            stroke="currentColor"
            class="w-5 h-5"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M6 6l12 12M6 18L18 6"
            />
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: `
    .icon-btn {
      background: transparent;
      border: none;
      color: #e5e7eb;
      font-family: Consolas;
      border-radius: 0.375rem;
      font-weight: 400;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.25rem .5rem;
      cursor: pointer;
      transition: background 0.15s, color 0.15s;
    }
    .icon-btn:hover:not(:disabled) {
      background: rgba(255,255,255,0.08);
      color: #fff;
    }
    .icon-btn:disabled {
      opacity: 0.5;
      cursor: default;
    }
  `,
})
export default class WindowChromeComponent implements OnInit {
  async ngOnInit() {
    this.api = await loadBackendApi<WindowsChromeApi>();

    exposeApiToBackend({
      changeAddress: async (url: string | null) => {
        this.address.set(url);
        this.canGoBack.set(await this.api.canGoBack());
        this.canGoForward.set(await this.api.canGoForward());
      },
    });

    await this.api.uiLoaded();
  }
  canGoBack = signal(false);
  canGoForward = signal(false);
  address = signal<string | null>(null);

  api!: WindowsChromeApi;

  back() {
    this.api.back();
  }

  forward() {
    this.api.forward();
  }

  reload() {
    this.api.reload();
  }

  min() {
    this.api.minimize();
  }

  max() {
    this.api.maximize();
  }

  close() {
    this.api.close();
  }

  copyAddress() {
    this.api.copyAddress();
  }
}
