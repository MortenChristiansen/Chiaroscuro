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

@Component({
  selector: 'tab-text-search',
  imports: [CommonModule, IconButtonComponent],
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
        />
        <icon-button
          [disabled]="!hasMatches()"
          (click)="goToPrev()"
          title="Previous match"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            width="20"
            height="20"
          >
            <path
              fill-rule="evenodd"
              d="M10 5a1 1 0 01.7.3l5 5a1 1 0 01-1.4 1.4L11 8.42V15a1 1 0 11-2 0V8.42l-3.3 3.3a1 1 0 01-1.4-1.42l5-5A1 1 0 0110 5z"
              clip-rule="evenodd"
            />
          </svg>
        </icon-button>
        <icon-button
          [disabled]="!hasMatches()"
          (click)="goToNext()"
          title="Next match"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            width="20"
            height="20"
          >
            <path
              fill-rule="evenodd"
              d="M10 15a1 1 0 01-.7-.3l-5-5a1 1 0 011.4-1.4L9 11.58V5a1 1 0 112 0v6.58l3.3-3.3a1 1 0 111.4 1.42l-5 5A1 1 0 0110 15z"
              clip-rule="evenodd"
            />
          </svg>
        </icon-button>
        <icon-button
          (click)="cancelSearch(); input.focus()"
          [disabled]="totalMatches() === null"
          title="Cancel search"
        >
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
