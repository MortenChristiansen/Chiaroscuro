import { Component, input, output } from '@angular/core';
import { Tab } from './server-models';
import { CommonModule } from '@angular/common';
import { FaviconComponent } from '../../shared/favicon.component';

@Component({
  selector: 'tabs-list-tab',
  standalone: true,
  template: `
    <div
      class="tab drag-handle group flex items-center px-4 py-2 rounded-lg select-none text-white font-sans text-base transition-colors duration-200 hover:bg-white/10"
      [ngClass]="{
        'bg-white/20 hover:bg-white/30': isActive(),
      }"
      (click)="onSelectTab()"
    >
      <favicon [src]="tab().favicon" class="w-4 h-4 mr-2" />
      <span class="truncate flex-1">{{ tab().title ?? 'Loading...' }}</span>
      <button
        class="close-button not-odd:ml-2 opacity-0 group-hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-1 rounded"
        (click)="onCloseTab($event)"
        aria-label="Close tab"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 20 20"
          stroke-width="2"
          stroke="currentColor"
          class="w-4 h-4"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M6 6l8 8M6 14L14 6"
          />
        </svg>
      </button>
    </div>
  `,
  imports: [CommonModule, FaviconComponent],
})
export class TabsListTabComponent {
  tab = input.required<Tab>();
  isActive = input.required<boolean>();
  selectTab = output<void>();
  closeTab = output<void>();

  onSelectTab() {
    this.selectTab.emit();
  }

  onCloseTab(event: Event) {
    event.stopPropagation();
    this.closeTab.emit();
  }
}
