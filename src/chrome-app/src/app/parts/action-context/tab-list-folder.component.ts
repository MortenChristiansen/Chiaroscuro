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
import { FolderIconComponent } from './folder-icon.component';

@Component({
  selector: 'tabs-list-folder',
  animations: [
    trigger('expandCollapse', [
      state('open', style({ height: '*', opacity: 1, overflow: 'visible' })),
      state('closed', style({ height: '0px', opacity: 0, overflow: 'hidden' })),
      transition('open <=> closed', [
        animate('200ms cubic-bezier(0.4,0,0.2,1)'),
      ]),
      transition('void => open', [
        style({ height: '0px', opacity: 0, overflow: 'hidden' }),
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
      <div class="group flex items-center px-2 py-1">
        <button
          class="folder-toggle-button mr-2 text-gray-400 hover:text-gray-300 transition-colors drag-handle"
          [class.open]="isOpen()"
        >
          <folder-icon [isOpen]="isOpen()" />
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
          spellcheck="false"
        />
        } @else {
        <span
          class="flex-1 truncate cursor-pointer"
          (mousedown)="toggleOpen.emit()"
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
        #contentWrapper
        [@expandCollapse]="isOpen() ? 'open' : 'closed'"
        class="folder-content flex flex-col mt-2 ml-6 transition-all duration-200"
        (@expandCollapse.start)="onAnimationStart()"
        (@expandCollapse.done)="onAnimationDone()"
        [style.display]="contentVisible() ? '' : 'none'"
        style="will-change: height, opacity;"
      >
        <ng-content />
      </div>
    </div>
  `,
  imports: [FolderIconComponent],
})
export class TabsListFolderComponent {
  name = input.required<string>();
  isOpen = input.required<boolean>();
  isNew = input.required<boolean>();
  containsActiveTab = input.required<boolean>();
  toggleOpen = output<void>();
  folderRenamed = output<string>();

  isEditing = signal(false);
  contentVisible = signal(false);
  animating = signal(false);
  hasStartedEditingDueToNewState = false;

  folderNameInput = viewChild<ElementRef<HTMLInputElement>>('folderNameInput');
  contentWrapper = viewChild<ElementRef<HTMLDivElement>>('contentWrapper');

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

    effect(() => {
      if (this.isOpen()) this.contentVisible.set(true);
    });

    effect(() => {
      const wrapperRef = this.contentWrapper();
      if (!wrapperRef) return;

      const element = wrapperRef.nativeElement;
      const open = this.isOpen();
      const animating = this.animating();

      if (open && this.contentVisible() && !animating) {
        element.style.removeProperty('height');
        element.style.removeProperty('overflow');
      } else if (!open && !animating) {
        element.style.height = '0px';
        element.style.overflow = 'hidden';
      }
    });
  }

  onAnimationStart() {
    this.animating.set(true);
    if (!this.contentVisible()) this.contentVisible.set(true);
  }
  onAnimationDone() {
    this.animating.set(false);
    if (!this.isOpen()) this.contentVisible.set(false);
  }
}
