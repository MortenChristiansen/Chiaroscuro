import { CommonModule } from '@angular/common';
import { Component, ElementRef, input, output, viewChild } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faCheck, faPen, faXmark } from '@fortawesome/free-solid-svg-icons';
import { IconButtonComponent } from '../../../shared/icon-button.component';
import { TabPaletteControlSectionComponent } from '../containers/control-section.component';

@Component({
  selector: 'tab-custom-title-editor',
  imports: [
    CommonModule,
    IconButtonComponent,
    FaIconComponent,
    TabPaletteControlSectionComponent,
  ],
  template: `
    <tab-palette-control-section
      [title]="'Custom tab title'"
      [icon]="titleIcon"
    >
      <div class="flex flex-wrap gap-2">
        <input
          #titleInput
          type="text"
          class="flex-1 min-w-[12rem] rounded-lg border border-slate-800/80 bg-slate-950/60 px-3 py-2 text-sm text-slate-100 shadow-inner outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-500/40"
          placeholder="Give this tab a sharper label"
          [value]="title() ?? ''"
          (input)="onTitleInput($event)"
          (keydown.enter)="onSave()"
          spellcheck="false"
        />
        <div
          class="flex items-center gap-1 rounded-md border border-slate-800/70 bg-slate-900/40 p-1"
        >
          <icon-button title="Save custom title" (click)="onSave()">
            <fa-icon class="text-emerald-300" [icon]="saveTitleIcon" />
          </icon-button>
          <icon-button
            title="Clear custom title"
            [disabled]="!hasCustomTitle()"
            (click)="clear.emit()"
          >
            <fa-icon class="text-rose-300" [icon]="clearTitleIcon" />
          </icon-button>
        </div>
      </div>

      <div class="text-xs text-slate-500">
        @if(initialTitle()) { Currently set to "{{ initialTitle() }}" } @else {
        No custom title saved. }
      </div>
    </tab-palette-control-section>
  `,
})
export class TabCustomTitleEditorComponent {
  title = input<string>();
  initialTitle = input<string>();
  hasCustomTitle = input(false);

  titleChange = output<string>();
  save = output<string>();
  clear = output<void>();

  protected readonly saveTitleIcon = faCheck;
  protected readonly clearTitleIcon = faXmark;
  protected readonly titleIcon = faPen;

  private readonly titleInput =
    viewChild.required<ElementRef<HTMLInputElement>>('titleInput');

  onTitleInput(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.titleChange.emit(value);
  }

  onSave() {
    const current = this.titleInput().nativeElement.value;
    this.save.emit(current);
  }

  setInputValue(value: string) {
    this.titleInput().nativeElement.value = value;
  }

  focusInput() {
    this.titleInput().nativeElement.focus();
  }
}
