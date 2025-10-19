import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, viewChild, computed } from '@angular/core';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabCustomizationApi } from './tabCustomizationApi';
import { TabCustomTitleEditorComponent } from './controls/tab-custom-title-editor.component';
import { TabFixedAddressEditorComponent } from './controls/tab-fixed-address-editor.component';

@Component({
  selector: 'tab-customization-editor',
  standalone: true,
  imports: [
    CommonModule,
    TabCustomTitleEditorComponent,
    TabFixedAddressEditorComponent,
  ],
  template: `
    <div class="flex flex-col gap-5 text-slate-100">
      <tab-custom-title-editor
        [title]="title()"
        [initialTitle]="initialTitle()"
        [hasCustomTitle]="hasCustomTitle()"
        (titleChange)="title.set($event)"
        (save)="save($event)"
        (clear)="clear()"
      />

      <tab-fixed-address-editor
        [disableFixedAddress]="disableFixedAddress()"
        (disableChange)="onToggleDisableFixed($event)"
      />
    </div>
  `,
})
export class TabCustomizationEditorComponent implements OnInit {
  title = signal('');
  initialTitle = signal<string | undefined>(undefined);
  disableFixedAddress = signal<boolean>(false);
  hasCustomTitle = computed(() => !!this.initialTitle());

  private readonly titleEditor = viewChild.required(
    TabCustomTitleEditorComponent
  );
  private api!: TabCustomizationApi;

  async ngOnInit() {
    this.api = await loadBackendApi<TabCustomizationApi>('tabCustomizationApi');

    exposeApiToBackend({
      initCustomSettings: (settings: {
        customTitle?: string;
        disableFixedAddress?: boolean;
      }) => {
        this.initialTitle.set(settings.customTitle);
        this.title.set(settings.customTitle ?? '');
        // I'm not sure why this is needed, but there are cases where the title signal does not update the input value.
        this.titleEditor().setInputValue(settings.customTitle ?? '');

        this.disableFixedAddress.set(settings.disableFixedAddress ?? false);
      },
    });
  }

  async save(value: string) {
    const trimmed = value.trim();
    const toSend = trimmed.length > 0 ? trimmed : null;
    await this.api.setCustomTitle(toSend);
    this.initialTitle.set(toSend ?? undefined);
  }

  async clear() {
    this.title.set('');
    await this.api.setCustomTitle(null);
    this.initialTitle.set(undefined);
    this.titleEditor().setInputValue('');
    this.titleEditor().focusInput();
  }

  async onToggleDisableFixed(checked: boolean) {
    this.disableFixedAddress.set(!!checked);
    await this.api.setDisableFixedAddress(!!checked);
  }
}
