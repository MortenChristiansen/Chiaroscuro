import {
  Component,
  effect,
  ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import {
  trigger,
  state,
  style,
  transition,
  animate,
} from '@angular/animations';

@Component({
  selector: 'tabs-list-folder',
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
      class="rounded-lg select-none text-white font-sans text-sm {{
        containsActiveTab() && !isOpen() ? ' font-bold' : ''
      }}"
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
            class="w-6 h-6"
          >
            <!-- Flap -->
            <path
              d="M3.5 9.5L5 6.5C5.2 6.1 5.6 5.9 6 5.9h4c.3 0 .6.1.8.3l1.2 1.3c.2.2.5.3.8.3h4.2c.4 0 .8.2 1 .6l1.2 2.1"
              stroke-linecap="round"
              stroke-linejoin="round"
              fill="none"
            />
            <!-- Open base -->
            <path
              d="M3.5 9.5h17c.4 0 .7.3.7.7v.2l-1.1 6.6c-.1.7-.7 1.2-1.4 1.2H5.3c-.7 0-1.3-.5-1.4-1.2l-1.1-6.6v-.2c0-.4.3-.7.7-.7Z"
              stroke-linecap="round"
              stroke-linejoin="round"
              fill="none"
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
            class="w-6 h-6"
          >
            <path
              d="M4 7.5C4 6.7 4.7 6 5.5 6h4c.3 0 .6.1.8.3l1.2 1.3c.2.2.5.3.8.3h5.2c.8 0 1.5.7 1.5 1.5v7.1c0 .8-.7 1.5-1.5 1.5h-13C4.7 18 4 17.3 4 16.5v-9Z"
              stroke-linecap="round"
              stroke-linejoin="round"
              fill="none"
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
  containsActiveTab = input.required<boolean>();
  toggleOpen = output<void>();
  folderRenamed = output<string>();

  isEditing = signal(false);
  animating = false;
  hasStartedEditingDueToNewState = false;

  folderNameInput = viewChild<ElementRef<HTMLInputElement>>('folderNameInput');

  constructor() {
    effect(() => {
      if (
        this.isNew() &&
        !this.isEditing() &&
        !this.hasStartedEditingDueToNewState
      ) {
        this.hasStartedEditingDueToNewState = true;
        this.isEditing.set(true);
      }

      if (this.folderNameInput()) {
        this.folderNameInput()!.nativeElement.focus();
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
