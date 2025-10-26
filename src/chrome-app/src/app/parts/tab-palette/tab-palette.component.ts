import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faGlobe,
  faMagnifyingGlass,
  faPalette,
  faWindowRestore,
} from '@fortawesome/free-solid-svg-icons';
import { TabPaletteSectionComponent } from './containers/palette-section.component';
import { TabTextSearchComponent } from './controls/tab-text-search.component';
import { TabCustomizationEditorComponent } from './tab-customization-editor.component';
import { DomainCssEditorComponent } from './controls/domain-css-editor.component';
import { exposeApiToBackend } from '../interfaces/api';

@Component({
  selector: 'tab-palette',
  template: `
    <section
      class="flex h-full w-full flex-col overflow-hidden bg-slate-950/40"
    >
      <header class="flex flex-col gap-2 px-4 pt-4 text-slate-100 md:px-6">
        <div
          class="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.3em] text-slate-500"
        >
          <fa-icon class="text-slate-400" [icon]="paletteIcon" />
          Tab Palette
        </div>
      </header>

      <div
        class="flex flex-col gap-4 min-h-0 overflow-y-auto px-4 py-4 md:px-6 md:py-6"
      >
        <tab-palette-section [title]="'Search'" [icon]="searchIcon">
          <tab-text-search />
        </tab-palette-section>

        <tab-palette-section [title]="'Customize Tab'" [icon]="tabIcon">
          <tab-customization-editor />
        </tab-palette-section>

        <tab-palette-section
          [title]="'Customize Domain'"
          [icon]="domainIcon"
          [pill]="currentDomain()"
        >
          <domain-css-editor />
        </tab-palette-section>
      </div>
    </section>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
  `,
  imports: [
    FaIconComponent,
    TabPaletteSectionComponent,
    TabTextSearchComponent,
    TabCustomizationEditorComponent,
    DomainCssEditorComponent,
  ],
})
export default class TabPaletteComponent implements OnInit {
  protected readonly paletteIcon = faPalette;
  protected readonly domainIcon = faGlobe;
  protected readonly tabIcon = faWindowRestore;
  protected readonly searchIcon = faMagnifyingGlass;

  currentDomain = signal<string | undefined>(undefined);

  async ngOnInit() {
    exposeApiToBackend({
      initDomainSettings: (domain: string) => {
        this.currentDomain.set(domain);
      },
    });
  }
}
