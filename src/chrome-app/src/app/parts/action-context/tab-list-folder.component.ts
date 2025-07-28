import { Component, effect, input, output, signal } from '@angular/core';
import {
  trigger,
  state,
  style,
  transition,
  animate,
} from '@angular/animations';

@Component({
  selector: 'tabs-list-folder',
  standalone: true,
  animations: [
    trigger('expandCollapse', [
      state('open', style({ height: '*', opacity: 1, overflow: 'visible' })),
      state('closed', style({ height: '0px', opacity: 0, overflow: 'hidden' })),
      transition('open <=> closed', [
        animate('200ms cubic-bezier(0.4,0,0.2,1)'),
      ]),
    ]),
  ],
  template: `
    <div
      class="rounded-lg select-none text-white font-sans text-sm bg-gray-700/50"
    >
      <div class="group flex items-center px-4 py-1">
        <button
          (click)="toggleOpen.emit()"
          class="mr-2 text-gray-400 hover:text-gray-300 transition-colors drag-handle"
        >
          @if (isOpen()) {
          <!-- Open folder icon -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M3.75 9.776c.112-.017.227-.026.344-.026h15.812c.117 0 .232.009.344.026m-16.5 0a2.25 2.25 0 0 0-1.883 2.542l.857 6a2.25 2.25 0 0 0 2.227 1.932H19.05a2.25 2.25 0 0 0 2.227-1.932l.857-6a2.25 2.25 0 0 0-1.883-2.542m-16.5 0V6A2.25 2.25 0 0 1 6 3.75h3.879a1.125 1.125 0 0 1 .746.417l2.25 2A1.125 1.125 0 0 0 13.621 6.5H18A2.25 2.25 0 0 1 20.25 8.75v1.026"
            />
          </svg>
          } @else {
          <!-- Closed folder icon -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-8.69-6.44-2.12-2.12a1.5 1.5 0 0 0-1.061-.44H4.5A2.25 2.25 0 0 0 2.25 6v12a2.25 2.25 0 0 0 2.25 2.25h15A2.25 2.25 0 0 0 21.75 18V9a2.25 2.25 0 0 0-2.25-2.25H11.69Z"
            />
          </svg>
          }
        </button>

        @if (isEditing()) {
        <input
          #folderNameInput
          [value]="name()"
          (blur)="
            isEditing.set(false); folderRenamed.emit(folderNameInput.value)
          "
          (focus)="folderNameInput.select()"
          (keydown.enter)="
            isEditing.set(false); folderRenamed.emit(folderNameInput.value)
          "
          (keydown.escape)="isEditing.set(false)"
          class="flex-1 bg-transparent border-b border-gray-400 focus:border-white outline-none text-white"
          autoFocus
        />
        } @else {
        <span
          class="flex-1 truncate cursor-pointer"
          (click)="toggleOpen.emit()"
        >
          {{ name() }}
        </span>
        <button
          class="edit-button ml-2 opacity-0 group-hover:opacity-100 transition-opacity duration-150 text-gray-400 hover:text-gray-300 p-1 rounded"
          (click)="$event.stopPropagation(); isEditing.set(true)"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.5"
            stroke="currentColor"
            class="w-4 h-4"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.287 4.287 0 0 1-1.897 1.13L6 18l.8-2.685a4.287 4.287 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10"
            />
          </svg>
        </button>
        }
      </div>
      <div
        [@expandCollapse]="isOpen() ? 'open' : 'closed'"
        class="flex flex-col mt-2 ml-6 transition-all duration-200"
        (@expandCollapse.start)="onAnimationStart()"
        (@expandCollapse.done)="onAnimationDone()"
        style="will-change: height, opacity;"
      >
        <ng-content />
      </div>
    </div>
  `,
})
export class TabsListFolderComponent {
  name = input.required<string>();
  isOpen = input.required<boolean>();
  isNew = input.required<boolean>();
  toggleOpen = output<void>();
  folderRenamed = output<string>();

  isEditing = signal(false);
  animating = false;

  constructor() {
    effect(() => {
      if (this.isNew()) {
        this.isEditing.set(true);
      }
    });
  }

  onAnimationStart() {
    this.animating = true;
  }
  onAnimationDone() {
    this.animating = false;
  }
}
