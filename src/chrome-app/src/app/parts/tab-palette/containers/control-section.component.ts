import { Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

@Component({
  selector: 'tab-palette-control-section',
  imports: [FaIconComponent],
  template: `
    <section class="flex flex-col gap-2 mt-4">
      <div
        class="flex flex-wrap items-center gap-2 text-sm font-medium text-slate-200"
      >
        <span
          class="flex h-8 w-8 items-center justify-center rounded-md bg-slate-800/80 text-slate-400"
        >
          <fa-icon [icon]="icon()" />
        </span>
        <span>{{ title() }}</span>
      </div>

      <ng-content />
    </section>
  `,
})
export class TabPaletteControlSectionComponent {
  title = input.required<string>();
  icon = input.required<IconDefinition>();
}
