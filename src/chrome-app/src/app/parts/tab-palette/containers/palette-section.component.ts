import { Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

@Component({
  selector: 'tab-palette-section',
  imports: [FaIconComponent],
  template: `
    <section
      class="flex flex-col gap-4 rounded-xl border border-slate-800/70 bg-slate-900/60 p-4 text-slate-100 shadow-sm backdrop-blur-sm"
    >
      <header
        class="flex flex-wrap items-start justify-between gap-3 sm:items-center"
      >
        <div class="flex flex-col gap-1">
          <h3 class="text-base font-semibold leading-5">
            <fa-icon [icon]="icon()" /> {{ title() }}
          </h3>
          <ng-content select="[section-description]" />
        </div>
        @if (pill()) {
        <span
          class="rounded-full border border-slate-700 bg-slate-900 px-2 py-1 text-xs font-semibold text-slate-300"
        >
          {{ pill() }}
        </span>
        }
      </header>

      <div class="flex flex-1 min-h-0 flex-col">
        <ng-content />
      </div>
    </section>
  `,
})
export class TabPaletteSectionComponent {
  title = input.required<string>();
  icon = input.required<IconDefinition>();
  pill = input<string>();
}
