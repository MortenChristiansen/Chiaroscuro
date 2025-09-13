import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  signal,
  ViewChild,
  ElementRef,
  viewChild,
} from '@angular/core';
import { IconButtonComponent } from '../../shared/icon-button.component';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabCustomizationApi } from './tabCustomizationApi';

@Component({
  selector: 'tab-customization-editor',
  standalone: true,
  imports: [CommonModule, IconButtonComponent],
  template: `
    <div class="flex flex-col gap-2 w-full">
      <div class="text-xs text-gray-400">Custom tab title</div>
      <div class="flex items-center gap-2">
        <input
          #titleInput
          type="text"
          class="flex-1 px-2 py-1 rounded border border-gray-600 bg-gray-900 text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Enter a custom title (optional)"
          [(value)]="title"
          (keydown.enter)="save(titleInput.value)"
        />
        <icon-button title="Save custom title" (click)="save(titleInput.value)">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            width="20"
            height="20"
          >
            <path
              fill-rule="evenodd"
              d="M16.707 5.293a1 1 0 010 1.414l-7.5 7.5a1 1 0 01-1.414 0l-3-3a1 1 0 111.414-1.414L8.5 12.086l6.793-6.793a1 1 0 011.414 0z"
              clip-rule="evenodd"
            />
          </svg>
        </icon-button>
        <icon-button title="Clear custom title" (click)="clear(titleInput)">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            width="20"
            height="20"
          >
            <path
              fill-rule="evenodd"
              d="M10 8.586l4.95-4.95a1 1 0 111.414 1.414L11.414 10l4.95 4.95a1 1 0 01-1.414 1.414L10 11.414l-4.95 4.95a1 1 0 01-1.414-1.414L8.586 10l-4.95-4.95A1 1 0 115.05 3.636L10 8.586z"
              clip-rule="evenodd"
            />
          </svg>
        </icon-button>
      </div>
      <div class="text-xs text-gray-500 h-4">
        @if(initialTitle() !== null && initialTitle() !== undefined) {
        Currently: "{{ initialTitle() || '(empty)' }}" } @else { No custom title
        set }
      </div>
      <div class="mt-4 pt-4 border-t border-white/10">
        <label class="inline-flex items-center gap-2 select-none">
          <input
            type="checkbox"
            class="w-4 h-4 rounded border-white/10 bg-gray-800"
            [checked]="disableFixedAddress()"
            (change)="onToggleDisableFixed($any($event.target).checked)"
          />
          <span class="text-sm text-gray-300">Disable fixed address</span>
        </label>
      </div>
      <div class="mt-4 pt-4 border-t border-white/10">
        <div class="text-xs text-gray-400 mb-2">Notification Permission</div>
        <div class="flex items-center justify-between">
          <span class="text-sm" [class]="getNotificationPermissionClass()">
            {{ getNotificationPermissionText() }}
          </span>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-1 text-xs rounded bg-green-600 hover:bg-green-700 text-white"
              [disabled]="notificationPermission() === 1"
              (click)="setNotificationPermission(1)"
            >
              Allow
            </button>
            <button
              type="button"
              class="px-3 py-1 text-xs rounded bg-red-600 hover:bg-red-700 text-white"
              [disabled]="notificationPermission() === 2"
              (click)="setNotificationPermission(2)"
            >
              Deny
            </button>
            <button
              type="button"
              class="px-3 py-1 text-xs rounded bg-gray-600 hover:bg-gray-700 text-white"
              [disabled]="notificationPermission() === 0"
              (click)="setNotificationPermission(0)"
            >
              Reset
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class TabCustomizationEditorComponent implements OnInit {
  title = signal('');
  initialTitle = signal<string | null>(null);
  disableFixedAddress = signal<boolean>(false);
  notificationPermission = signal<number>(0); // 0 = NotAsked, 1 = Granted, 2 = Denied

  titleInput = viewChild.required<ElementRef<HTMLInputElement>>('titleInput');
  private api!: TabCustomizationApi;

  getNotificationPermissionText(): string {
    switch (this.notificationPermission()) {
      case 1: return 'Granted';
      case 2: return 'Denied';
      default: return 'Not Asked';
    }
  }

  getNotificationPermissionClass(): string {
    switch (this.notificationPermission()) {
      case 1: return 'text-green-400';
      case 2: return 'text-red-400';
      default: return 'text-gray-400';
    }
  }

  async ngOnInit() {
    this.api = await loadBackendApi<TabCustomizationApi>('tabCustomizationApi');

    exposeApiToBackend({
      initCustomSettings: (settings: {
        customTitle: string | null;
        disableFixedAddress: boolean | null;
        notificationPermission: number;
      }) => {
        this.initialTitle.set(settings.customTitle);
        this.title.set(settings.customTitle ?? '');
        // I'm not sure why this is needed, but there are cases where the title signal does not update the input value.
        this.titleInput().nativeElement.value = settings.customTitle ?? '';

        this.disableFixedAddress.set(settings.disableFixedAddress ?? false);
        this.notificationPermission.set(settings.notificationPermission ?? 0);
      },
    });
  }

  async save(value: string) {
    const trimmed = value.trim();
    const toSend = trimmed.length > 0 ? trimmed : null;
    await this.api.setCustomTitle(toSend);
    this.initialTitle.set(toSend);
  }

  async clear(input: HTMLInputElement) {
    this.title.set('');
    await this.api.setCustomTitle(null);
    this.initialTitle.set(null);
    input.focus();
  }

  async onToggleDisableFixed(checked: boolean) {
    this.disableFixedAddress.set(!!checked);
    await this.api.setDisableFixedAddress(!!checked);
  }

  async setNotificationPermission(permissionStatus: number) {
    this.notificationPermission.set(permissionStatus);
    await this.api.setNotificationPermission(permissionStatus);
  }
}
