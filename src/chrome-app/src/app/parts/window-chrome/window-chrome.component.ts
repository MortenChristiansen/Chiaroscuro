import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faArrowRight,
  faArrowRotateRight,
  faCopy,
  faMinus,
  faSpinner,
  faWindowMaximize,
  faWindowRestore,
  faXmark,
} from '@fortawesome/free-solid-svg-icons';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { IconButtonComponent } from '../../shared/icon-button.component';
import { WindowsChromeApi } from './windowChromeApi';

@Component({
  selector: 'window-chrome',
  imports: [IconButtonComponent, CommonModule, FaIconComponent],
  template: `
    <div class="flex items-center gap-4 select-none">
      <div class="address-bar flex flex-1 items-center gap-1 justify-center">
        <icon-button [disabled]="!address() || !canGoBack()" (onClick)="back()">
          <fa-icon size="xs" [icon]="backIcon" />
        </icon-button>
        <icon-button
          [disabled]="!address() || !canGoForward()"
          (onClick)="forward()"
        >
          <fa-icon size="xs" [icon]="forwardIcon" />
        </icon-button>
        <icon-button [disabled]="!address()" (onClick)="reload()">
          <fa-icon size="xs" [icon]="reloadIcon" />
        </icon-button>
        <icon-button [disabled]="!address()" (onClick)="copyAddress()">
          <fa-icon size="xs" [icon]="copyIcon" />
        </icon-button>
        <span
          class="address mx-4 font-sans text-sm text-gray-200 max-w-[400px] truncate"
        >
          @let url = address(); @if (url === null) {
          <span class="italic text-gray-400">Press ctrl-t to start</span>
          } @else {
          <span>{{ url }}</span>
          }
        </span>
        <fa-icon
          aria-label="Loading page"
          class="loading-indicator mr-2 text-white transition-opacity duration-200 animate-spin"
          [class.opacity-100]="isLoading()"
          [class.opacity-0]="!isLoading()"
          size="xs"
          [icon]="spinnerIcon"
        />
      </div>
      <div class="window-controls flex gap-1 ml-auto">
        <icon-button (onClick)="min()">
          <fa-icon size="xs" [icon]="minimizeIcon" />
        </icon-button>
        <icon-button (onClick)="max()">
          @if (isMaximized()) {
          <fa-icon size="xs" [icon]="restoreIcon" />
          } @else {
          <fa-icon size="xs" [icon]="maximizeIcon" />
          }
        </icon-button>
        <icon-button (onClick)="close()">
          <fa-icon size="xs" [icon]="closeIcon" />
        </icon-button>
      </div>
    </div>
  `,
})
export default class WindowChromeComponent implements OnInit {
  protected readonly backIcon = faArrowLeft;
  protected readonly forwardIcon = faArrowRight;
  protected readonly reloadIcon = faArrowRotateRight;
  protected readonly copyIcon = faCopy;
  protected readonly spinnerIcon = faSpinner;
  protected readonly minimizeIcon = faMinus;
  protected readonly restoreIcon = faWindowRestore;
  protected readonly maximizeIcon = faWindowMaximize;
  protected readonly closeIcon = faXmark;

  async ngOnInit() {
    this.api = await loadBackendApi<WindowsChromeApi>();

    this.isMaximized.set(false);

    exposeApiToBackend({
      changeAddress: async (url: string | null) => {
        this.address.set(url);
        this.canGoBack.set(await this.api.canGoBack());
        this.canGoForward.set(await this.api.canGoForward());
        this.isLoading.set(await this.api.isLoading());
      },
      updateLoadingState: (isLoading: boolean) => {
        this.isLoading.set(isLoading);
      },
    });

    this.api.onLoaded();
  }
  canGoBack = signal(false);
  canGoForward = signal(false);
  address = signal<string | null>(null);
  isLoading = signal(false);
  isMaximized = signal(false);

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

  async max() {
    await this.api.maximize();
    this.isMaximized.update((value) => !value);
  }

  close() {
    this.api.close();
  }

  copyAddress() {
    this.api.copyAddress();
  }
}
