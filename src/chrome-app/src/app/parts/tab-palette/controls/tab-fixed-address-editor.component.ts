import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { faLocationDot } from '@fortawesome/free-solid-svg-icons';
import { TabPaletteControlSectionComponent } from '../containers/control-section.component';

@Component({
  selector: 'tab-fixed-address-editor',
  imports: [CommonModule, TabPaletteControlSectionComponent],
  template: `
    <tab-palette-control-section
      [title]="'Fixed address'"
      [icon]="fixedAddressIcon"
    >
      <p class="mt-1 text-xs text-slate-500">
        Control whether this tab keeps its fixed workspace address.
      </p>
      <label
        class="inline-flex select-none items-center gap-2 text-sm text-slate-300"
      >
        <input
          type="checkbox"
          class="h-4 w-4 appearance-none rounded border border-slate-700 bg-slate-950/70 transition checked:border-transparent checked:bg-slate-500"
          [checked]="disableFixedAddress()"
          (change)="onToggle($any($event.target).checked)"
        />
        <span>Disable</span>
      </label>
    </tab-palette-control-section>
  `,
})
export class TabFixedAddressEditorComponent {
  disableFixedAddress = input(false);
  disableChange = output<boolean>();

  protected readonly fixedAddressIcon = faLocationDot;

  onToggle(checked: boolean) {
    this.disableChange.emit(!!checked);
  }
}
