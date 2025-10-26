import { CommonModule } from '@angular/common';
import { IconButtonComponent } from '../../../shared/icon-button.component';
import {
  Component,
  signal,
  OnInit,
  computed,
  viewChild,
  ElementRef,
} from '@angular/core';
import { exposeApiToBackend, loadBackendApi } from '../../interfaces/api';
import { TabPaletteApi } from '../tabPaletteApi';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faChevronDown,
  faChevronUp,
  faXmark,
} from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'tab-text-search',
  imports: [CommonModule, IconButtonComponent, FaIconComponent],
  template: `
    <div class="flex flex-col gap-3 text-slate-100">
      <div
        class="flex flex-wrap items-center gap-2 text-sm font-medium text-slate-200"
      >
        @if (totalMatches() !== null) {
        <span class="ml-auto text-xs font-normal text-slate-400">
          @if (hasMatches()) {
          {{ totalMatches() }} matches } @else { No matches }
        </span>
        }
      </div>

      <div class="flex flex-wrap gap-2">
        <div class="relative flex-1 min-w-[12rem]">
          <input
            #input
            type="text"
            class="w-full rounded-lg border border-slate-800/80 bg-slate-950/60 px-3 py-2 text-sm text-slate-100 shadow-inner outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-500/40"
            placeholder="Search within the current tab"
            [(value)]="searchTerm"
            (keydown.enter)="onSearchTermChange(input.value)"
            spellcheck="false"
          />
        </div>

        <div
          class="flex items-center gap-1 rounded-md border border-slate-800/70 bg-slate-900/40 p-1"
        >
          <icon-button
            [disabled]="!hasMatches()"
            (click)="goToPrev()"
            title="Previous match"
          >
            <fa-icon class="text-slate-200" [icon]="previousMatchIcon" />
          </icon-button>
          <icon-button
            [disabled]="!hasMatches()"
            (click)="goToNext()"
            title="Next match"
          >
            <fa-icon class="text-slate-200" [icon]="nextMatchIcon" />
          </icon-button>
          <div class="h-5 w-px self-center bg-slate-800/70"></div>
          <icon-button
            (click)="cancelSearch(); input.focus()"
            [disabled]="totalMatches() === null"
            title="Clear search"
          >
            <fa-icon class="text-slate-200" [icon]="cancelSearchIcon" />
          </icon-button>
        </div>
      </div>

      <div class="min-h-[1.25rem] text-xs">
        @if (totalMatches() === null) {
        <span class="text-slate-500"
          >Start typing to look for text on this page.</span
        >
        } @else {
        <span
          [class.text-emerald-400]="hasMatches()"
          [class.text-rose-400]="!hasMatches()"
        >
          @if (hasMatches()) {
          {{ totalMatches() }} matches ready to jump to. } @else { No matches
          yet — try a different phrase. }
        </span>
        }
      </div>
    </div>
  `,
})
export class TabTextSearchComponent implements OnInit {
  searchTerm = signal('');
  totalMatches = signal<number | null>(null);
  hasMatches = computed(
    () => this.totalMatches() != null && this.totalMatches()! > 0
  );

  protected readonly previousMatchIcon = faChevronUp;
  protected readonly nextMatchIcon = faChevronDown;
  protected readonly cancelSearchIcon = faXmark;

  private searchInput =
    viewChild.required<ElementRef<HTMLInputElement>>('input');

  private api!: TabPaletteApi;

  async ngOnInit() {
    this.api = await loadBackendApi<TabPaletteApi>('findTextApi');

    exposeApiToBackend({
      findStatusChanged: (totalMatches?: number) => {
        this.totalMatches.set(totalMatches ?? null);
      },
      focusFindTextInput: () => {
        setTimeout(() => this.searchInput().nativeElement.focus());
      },
      init: () => {
        this.resetSearch();
      },
    });
  }

  async onSearchTermChange(term: string) {
    this.searchTerm.set(term);
    await this.api.find(term);
  }

  async goToNext() {
    if (!this.hasMatches()) return;
    await this.api.nextMatch(this.searchTerm());
  }

  async goToPrev() {
    if (!this.hasMatches()) return;
    await this.api.prevMatch(this.searchTerm());
  }

  async cancelSearch() {
    this.resetSearch();
    await this.api.stopFinding();
  }

  private resetSearch() {
    this.searchTerm.set('');
    this.totalMatches.set(null);
  }
}
