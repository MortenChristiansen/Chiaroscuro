import { CommonModule } from '@angular/common';
import { IconButtonComponent } from '../../shared/icon-button.component';
import {
  Component,
  signal,
  OnInit,
  computed,
  viewChild,
  ElementRef,
} from '@angular/core';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabPaletteApi } from './tabPaletteApi';
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
    <div class="flex flex-col gap-2 w-full">
      <div class="text-xs text-gray-400">Find in page</div>
      <div class="flex items-center gap-2">
        <input
          #input
          type="text"
          class="flex-1 px-2 py-1 rounded border border-gray-600 bg-gray-900 text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Search..."
          [(value)]="searchTerm"
          (keydown.enter)="onSearchTermChange(input.value)"
          spellcheck="false"
        />
        <icon-button
          [disabled]="!hasMatches()"
          (click)="goToPrev()"
          title="Previous match"
        >
          <fa-icon [icon]="previousMatchIcon" />
        </icon-button>
        <icon-button
          [disabled]="!hasMatches()"
          (click)="goToNext()"
          title="Next match"
        >
          <fa-icon [icon]="nextMatchIcon" />
        </icon-button>
        <icon-button
          (click)="cancelSearch(); input.focus()"
          [disabled]="totalMatches() === null"
          title="Cancel search"
        >
          <fa-icon [icon]="cancelSearchIcon" />
        </icon-button>
      </div>
      @if(totalMatches() !== null) {
      <div class="text-xs text-gray-400 h-4">
        @if(totalMatches()! > 0) {
        {{ totalMatches() }} matches } @else { No matches }
      </div>
      }
    </div>
  `,
  standalone: true,
  styles: ``,
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
        console.log('Search status changed:', totalMatches);
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
    console.log('Searching for:', term);
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
