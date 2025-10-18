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
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faCheck, faXmark } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'tab-customization-editor',
  standalone: true,
  imports: [CommonModule, IconButtonComponent, FaIconComponent],
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
          spellcheck="false"
        />
        <icon-button title="Save custom title" (click)="save(titleInput.value)">
          <fa-icon [icon]="saveTitleIcon" />
        </icon-button>
        <icon-button title="Clear custom title" (click)="clear(titleInput)">
          <fa-icon [icon]="clearTitleIcon" />
        </icon-button>
      </div>
      <div class="text-xs text-gray-500">
        @if(initialTitle() !== null && initialTitle() !== undefined) {
        Currently: "{{ initialTitle() || '(empty)' }}" } @else { No custom title
        set }
        <div class="text-xs text-gray-400 mt-4">Fixed address</div>
        <label class="inline-flex items-center gap-2 select-none pt-2">
          <input
            type="checkbox"
            class="appearance-none w-4 h-4 rounded border-gray-600 border-1 bg-gray-900 checked:bg-gray-500 checked:border-0"
            [checked]="disableFixedAddress()"
            (change)="onToggleDisableFixed($any($event.target).checked)"
          />
          <span class="text-sm text-gray-500">Disabled</span>
        </label>
      </div>
    </div>
  `,
})
export class TabCustomizationEditorComponent implements OnInit {
  title = signal('');
  initialTitle = signal<string | null>(null);
  disableFixedAddress = signal<boolean>(false);

  protected readonly saveTitleIcon = faCheck;
  protected readonly clearTitleIcon = faXmark;

  titleInput = viewChild.required<ElementRef<HTMLInputElement>>('titleInput');
  private api!: TabCustomizationApi;

  async ngOnInit() {
    this.api = await loadBackendApi<TabCustomizationApi>('tabCustomizationApi');

    exposeApiToBackend({
      initCustomSettings: (settings: {
        customTitle: string | null;
        disableFixedAddress: boolean | null;
      }) => {
        this.initialTitle.set(settings.customTitle);
        this.title.set(settings.customTitle ?? '');
        // I'm not sure why this is needed, but there are cases where the title signal does not update the input value.
        this.titleInput().nativeElement.value = settings.customTitle ?? '';

        this.disableFixedAddress.set(settings.disableFixedAddress ?? false);
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
}
