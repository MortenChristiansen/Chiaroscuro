import { CommonModule } from '@angular/common';
import { Component, signal, OnInit, computed } from '@angular/core';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TabPaletteApi } from './tabPaletteApi';

@Component({
  selector: 'tab-text-search',
  imports: [CommonModule],
  template: `
    <div class="flex flex-col gap-2 w-full">
      <div class="flex items-center gap-2">
        <input
          #input
          type="text"
          class="flex-1 px-2 py-1 rounded border border-gray-600 bg-gray-900 text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Find in page..."
          [(value)]="searchTerm"
          (keydown.enter)="onSearchTermChange(input.value)"
        />
        <button
          class="px-2 py-1 rounded bg-gray-700 text-gray-200 hover:bg-gray-600 disabled:opacity-50"
          [disabled]="!hasMatches()"
          (click)="goToPrev()"
        >
          &#8593;
        </button>
        <button
          class="px-2 py-1 rounded bg-gray-700 text-gray-200 hover:bg-gray-600 disabled:opacity-50"
          [disabled]="!hasMatches()"
          (click)="goToNext()"
        >
          &#8595;
        </button>
        <button
          class="px-2 py-1 rounded bg-gray-600 text-gray-300 hover:bg-gray-500"
          (click)="cancelSearch()"
          [disabled]="!searchTerm()"
          title="Cancel search"
        >
          &#10006;
        </button>
      </div>
      <div class="text-xs text-gray-400 h-4">
        @if(totalMatches() > 0) { Match {{ currentMatch() }} of
        {{ totalMatches() }}
        } @else { No matches }
      </div>
    </div>
  `,
  styles: ``,
})
export class TabTextSearchComponent implements OnInit {
  searchTerm = signal('');
  totalMatches = signal(0);
  currentMatch = signal<number | null>(null);
  hasMatches = computed(() => this.totalMatches() > 0);

  private api!: TabPaletteApi;

  async ngOnInit() {
    this.api = await loadBackendApi<TabPaletteApi>('api');

    exposeApiToBackend({
      findStatusChanged: (totalMatches: number) => {
        console.log('Search status changed:', totalMatches);
        this.totalMatches.set(totalMatches);
        if (this.currentMatch() === null) this.currentMatch.set(1);
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
    this.currentMatch.update((previous) =>
      previous === this.totalMatches() ? 1 : previous! + 1
    );
    await this.api.nextMatch(this.searchTerm());
  }

  async goToPrev() {
    if (!this.hasMatches()) return;
    this.currentMatch.update((previous) =>
      previous === 1 ? this.totalMatches() : previous! - 1
    );
    await this.api.prevMatch(this.searchTerm());
  }

  async cancelSearch() {
    this.searchTerm.set('');
    this.totalMatches.set(0);
    this.currentMatch.set(null);
    await this.api.stopFinding();
  }
}
