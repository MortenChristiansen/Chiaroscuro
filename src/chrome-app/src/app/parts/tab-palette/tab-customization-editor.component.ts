import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
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
    </div>
  `,
})
export class TabCustomizationEditorComponent implements OnInit {
  // provided by backend via exposed API; null means clear
  title = signal('');
  initialTitle = signal<string | null>(null);

  private api!: TabCustomizationApi;

  async ngOnInit() {
    this.api = await loadBackendApi<TabCustomizationApi>('tabCustomizationApi');

    // Expose methods for backend to push current custom title
    exposeApiToBackend({
      initCustomTitle: (currentTitle: string | null) => {
        this.initialTitle.set(currentTitle);
        this.title.set(currentTitle ?? '');
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
}
