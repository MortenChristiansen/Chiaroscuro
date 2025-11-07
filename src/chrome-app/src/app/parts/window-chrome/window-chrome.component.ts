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
import UrlDisplayComponent from './url-display.component';
import { WindowsChromeApi } from './windowChromeApi';

@Component({
  selector: 'window-chrome',
  imports: [
    IconButtonComponent,
    UrlDisplayComponent,
    CommonModule,
    FaIconComponent,
  ],
  template: `
    <div
      class="grid w-full grid-cols-[auto_auto_auto] items-center gap-2 select-none"
    >
      <div class="flex items-center gap-1">
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
        <fa-icon
          aria-label="Loading page"
          class="loading-indicator ml-2 text-white transition-opacity duration-200 animate-spin"
          [class.opacity-100]="isLoading()"
          [class.opacity-0]="!isLoading()"
          size="xs"
          [icon]="spinnerIcon"
        />
      </div>
      <div class="min-w-0 justify-self-center max-w-[60%]">
        <url-display [url]="address()" />
      </div>
      <div class="window-controls flex gap-1 justify-self-end">
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

    this.isMaximized.set(await this.api.getIsMaximized());

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
      updateWindowState: (isMaximized: boolean) => {
        this.isMaximized.set(isMaximized);
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
  }

  close() {
    this.api.close();
  }

  copyAddress() {
    this.api.copyAddress();
  }
}
