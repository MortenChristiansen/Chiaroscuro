import {
  Component,
  ElementRef,
  OnInit,
  computed,
  effect,
  signal,
  viewChild,
} from '@angular/core';
import {
  ActionDialogApi,
  ActionType,
  NavigationSuggestion,
} from './actionDialogApi';
import { loadBackendApi, exposeApiToBackend } from '../interfaces/api';
import { CommonModule } from '@angular/common';
import { debounce } from '../../shared/utils';
import { FaviconComponent } from '../../shared/favicon.component';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { animate, style, transition, trigger } from '@angular/animations';
import {
  faArrowRight,
  faMagnifyingGlass,
  faMapLocationDot,
  faWindowRestore,
} from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'action-dialog',
  imports: [CommonModule, FaviconComponent, FaIconComponent],
  animations: [
    trigger('suggestionsContainer', [
      transition(':enter', [
        style({
          height: 0,
          opacity: 0,
          overflow: 'hidden',
          paddingBottom: 0,
          paddingTop: 0,
        }),
        animate(
          '250ms cubic-bezier(0.16, 1, 0.3, 1)',
          style({
            height: '*',
            opacity: 1,
            paddingBottom: '*',
            paddingTop: '*',
          })
        ),
      ]),
      transition(':leave', [
        style({ overflow: 'hidden' }),
        animate(
          '250ms cubic-bezier(0.66, 0, 0.83, 0.67)',
          style({
            height: 0,
            opacity: 0,
            paddingBottom: 0,
            paddingTop: 0,
          })
        ),
      ]),
    ]),
    trigger('suggestionItem', [
      transition(':enter', [
        style({
          height: 0,
          marginBottom: 0,
          marginTop: 0,
          opacity: 0,
          paddingBottom: 0,
          paddingTop: 0,
        }),
        animate(
          '250ms cubic-bezier(0.16, 1, 0.3, 1)',
          style({
            height: '*',
            marginBottom: '*',
            marginTop: '*',
            opacity: 1,
            paddingBottom: '*',
            paddingTop: '*',
          })
        ),
      ]),
      transition(':leave', [
        animate(
          '250ms cubic-bezier(0.66, 0, 0.83, 0.67)',
          style({
            height: 0,
            marginBottom: 0,
            marginTop: 0,
            opacity: 0,
            paddingBottom: 0,
            paddingTop: 0,
          })
        ),
      ]),
    ]),
  ],
  template: `
    <div
      class="fixed inset-0 z-1000 bg-transparent"
      (click)="api.dismissActionDialog()"
    ></div>
    <div
      class="fixed left-1/2 top-1/2 z-1001 w-120 max-w-[90vw] -translate-x-1/2 -translate-y-1/2"
      role="dialog"
      aria-modal="true"
      aria-labelledby="action-dialog-title"
    >
      <div
        class="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white/95 p-6 shadow-2xl"
      >
        <div class="relative">
          <input
            #dialog
            class="w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-base text-slate-900 shadow-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
            placeholder="Where to?"
            type="text"
            [value]="inputValue()"
            (input)="onInputChange($event)"
            (keydown)="onKeyDown($event)"
            spellcheck="false"
            role="combobox"
            autocomplete="off"
            aria-autocomplete="list"
            [attr.aria-expanded]="suggestions().length > 0"
            [attr.aria-controls]="
              suggestions().length > 0 ? 'action-dialog-options' : null
            "
            [attr.aria-activedescendant]="
              activeSuggestionIndex() >= 0
                ? 'action-dialog-option-' + activeSuggestionIndex()
                : null
            "
          />
        </div>
        @let summary = actionSummary();
        <div
          class="rounded-xl border px-4 py-3 text-sm text-slate-600 transition-colors"
          [class.border-emerald-200]="summary.accent === 'emerald'"
          [class.border-amber-200]="summary.accent === 'amber'"
          [class.border-violet-200]="summary.accent === 'violet'"
          [class.border-slate-200]="summary.accent === 'slate'"
          [class.bg-emerald-50]="summary.accent === 'emerald'"
          [class.bg-amber-50]="summary.accent === 'amber'"
          [class.bg-violet-50]="summary.accent === 'violet'"
          [class.bg-slate-50]="summary.accent === 'slate'"
        >
          <div class="flex items-center gap-3">
            <div
              class="flex h-8 w-8 items-center justify-center rounded-full bg-white text-slate-500 ring-1 ring-inset transition-colors leading-none"
              [class.text-emerald-600]="summary.accent === 'emerald'"
              [class.text-amber-600]="summary.accent === 'amber'"
              [class.text-violet-500]="summary.accent === 'violet'"
              [class.text-slate-500]="summary.accent === 'slate'"
              [class.ring-emerald-200]="summary.accent === 'emerald'"
              [class.ring-amber-200]="summary.accent === 'amber'"
              [class.ring-violet-200]="summary.accent === 'violet'"
              [class.ring-slate-200]="summary.accent === 'slate'"
            >
              @switch (summary.state) { @case ('navigate') {
              <fa-icon
                class="block h-4 w-4"
                [icon]="navigateIcon"
                aria-hidden="true"
                [fixedWidth]="true"
              />
              } @case ('system') {
              <fa-icon
                class="block h-4 w-4"
                [icon]="systemIcon"
                aria-hidden="true"
                [fixedWidth]="true"
              />
              } @case ('search') {
              <fa-icon
                class="block h-4 w-4"
                [icon]="searchIcon"
                aria-hidden="true"
                [fixedWidth]="true"
              />
              } @case ('ready') {
              <fa-icon
                class="block h-4 w-4"
                [icon]="readyIcon"
                aria-hidden="true"
                [fixedWidth]="true"
              />
              } }
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-medium text-slate-900">
                {{ summary.title }}
              </p>
              <p class="truncate text-xs text-slate-500">
                {{ summary.subtitle }}
              </p>
            </div>
            <span
              class="text-xs font-semibold uppercase tracking-wide"
              [class.text-emerald-600]="summary.accent === 'emerald'"
              [class.text-amber-600]="summary.accent === 'amber'"
              [class.text-violet-500]="summary.accent === 'violet'"
              [class.text-slate-500]="summary.accent === 'slate'"
            >
              {{ summary.badge }}
            </span>
          </div>
        </div>
        @if (suggestions().length === 0) {
        <div
          class="rounded-xl border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500"
        >
          Start typing to see suggestions.
        </div>
        } @else {
        <div
          id="action-dialog-options"
          role="listbox"
          class="max-h-72 overflow-y-auto rounded-xl border border-slate-200 bg-white p-1"
          [@suggestionsContainer]="'active'"
        >
          @for (suggestion of suggestions(); track suggestion.address; let index
          = $index) {
          <button
            type="button"
            id="action-dialog-option-{{ index }}"
            role="option"
            class="flex w-full items-center gap-3 rounded-lg border border-transparent px-3 py-2 text-left text-slate-600 transition hover:border-slate-200 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-400"
            [attr.aria-selected]="activeSuggestionIndex() === index"
            [class.bg-blue-50]="activeSuggestionIndex() === index"
            [class.border-blue-200]="activeSuggestionIndex() === index"
            [class.text-slate-900]="activeSuggestionIndex() === index"
            [class.shadow-sm]="activeSuggestionIndex() === index"
            (click)="selectSuggestion(suggestion)"
            [@suggestionItem]="'active'"
          >
            <div
              class="flex h-9 w-9 items-center justify-center bg-transparent transition"
              [class.border-blue-200]="activeSuggestionIndex() === index"
              [class.bg-blue-100]="activeSuggestionIndex() === index"
            >
              <favicon
                [src]="suggestion.favicon"
                class="h-full w-full"
              ></favicon>
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium">
                {{ suggestion.title }}
              </p>
              <p
                class="truncate text-xs text-slate-500"
                [class.text-slate-600]="activeSuggestionIndex() === index"
              >
                {{ suggestion.address }}
              </p>
            </div>
            <span
              class="text-xs font-semibold uppercase tracking-wide text-slate-400"
              [class.text-blue-500]="activeSuggestionIndex() === index"
            >
              Enter
            </span>
          </button>
          }
        </div>
        }
      </div>
    </div>
  `,
})
export default class ActionDialogComponent implements OnInit {
  private readonly suggestionDebounceDelay = 300;
  readonly dialog = viewChild.required<ElementRef<HTMLInputElement>>('dialog');
  readonly inputValue = signal('');
  readonly suggestions = signal<NavigationSuggestion[]>([]);
  readonly activeSuggestionIndex = signal<number>(-1);
  private readonly hasManualNavigation = signal(false);
  private readonly suppressInitialSelection = signal(true);
  readonly actionType = signal<ActionType | null>(null);
  readonly navigateIcon = faArrowRight;
  readonly systemIcon = faWindowRestore;
  readonly searchIcon = faMagnifyingGlass;
  readonly readyIcon = faMapLocationDot;
  readonly activeSuggestion = computed(() => {
    const items = this.suggestions();
    const index = this.activeSuggestionIndex();
    return index >= 0 && index < items.length ? items[index] : null;
  });
  readonly actionSummary = computed(() => {
    const selection = this.activeSuggestion();
    const rawValue = selection ? selection.address : this.inputValue().trim();

    if (!rawValue) {
      return {
        state: 'ready' as const,
        title: 'Ready for your next move',
        subtitle: 'Type to search or navigate to a page.',
        badge: 'Ready',
        accent: 'slate',
      };
    }

    const resolvedType: ActionType = this.actionType() ?? 'Search';
    const accentByType: Record<ActionType, 'emerald' | 'violet' | 'amber'> = {
      Navigate: 'emerald',
      OpenSystemPage: 'violet',
      Search: 'amber',
    };
    const badgeByType: Record<ActionType, string> = {
      Navigate: 'Navigate',
      OpenSystemPage: 'System',
      Search: 'Search',
    };
    const subtitleByType: Record<ActionType, string> = {
      Navigate: 'Navigates directly to this address.',
      OpenSystemPage: 'Opens a built-in workspace or system view.',
      Search: 'Searches using your default provider.',
    };

    return {
      state:
        resolvedType === 'OpenSystemPage'
          ? ('system' as const)
          : resolvedType === 'Navigate'
          ? ('navigate' as const)
          : ('search' as const),
      title: selection ? selection.title : rawValue,
      subtitle: selection ? selection.address : subtitleByType[resolvedType],
      badge: badgeByType[resolvedType],
      accent: accentByType[resolvedType],
    };
  });

  private readonly notifyValueChanged = debounce((value: string) => {
    this.api.notifyValueChanged(value);
  }, this.suggestionDebounceDelay);
  private actionTypeRequestId = 0;
  private readonly updateActionType = debounce((value: string) => {
    void this.evaluateActionType(value);
  }, this.suggestionDebounceDelay);

  private async evaluateActionType(value: string) {
    const trimmed = value.trim();
    const requestId = ++this.actionTypeRequestId;

    if (!trimmed) {
      if (this.actionType() !== null) {
        this.actionType.set(null);
      }
      return;
    }

    const api = this.api;
    if (!api) {
      return;
    }

    try {
      const type = await api.getActionType(trimmed);
      if (this.actionTypeRequestId === requestId) {
        this.actionType.set(type);
      }
    } catch {
      if (this.actionTypeRequestId === requestId) {
        this.actionType.set(null);
      }
    }
  }

  constructor() {
    effect(() => {
      const items = this.suggestions();
      const manual = this.hasManualNavigation();
      const suppress = this.suppressInitialSelection();
      const currentIndex = this.activeSuggestionIndex();

      if (items.length === 0) {
        if (currentIndex !== -1) {
          this.activeSuggestionIndex.set(-1);
        }
        return;
      }

      if (suppress) {
        if (currentIndex !== -1) {
          this.activeSuggestionIndex.set(-1);
        }
        return;
      }

      if (!manual) {
        if (currentIndex !== 0) {
          this.activeSuggestionIndex.set(0);
        }
        return;
      }

      if (currentIndex >= items.length) {
        this.activeSuggestionIndex.set(items.length - 1);
      } else if (currentIndex < 0) {
        this.activeSuggestionIndex.set(0);
      }
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<ActionDialogApi>();

    exposeApiToBackend({
      showDialog: () => {
        this.inputValue.set('');
        this.suggestions.set([]);
        this.activeSuggestionIndex.set(-1);
        this.hasManualNavigation.set(false);
        this.suppressInitialSelection.set(true);
        this.actionType.set(null);
        this.actionTypeRequestId++;
        setTimeout(() => this.dialog().nativeElement.focus(), 0);
      },
      updateSuggestions: (suggestions: NavigationSuggestion[]) => {
        this.hasManualNavigation.set(false);
        this.suppressInitialSelection.set(true);
        this.suggestions.set(suggestions);
        void this.updateActionType(this.inputValue());
      },
    });
  }

  api!: ActionDialogApi;

  onInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    this.inputValue.set(value);
    this.hasManualNavigation.set(false);
    this.suppressInitialSelection.set(true);
    this.notifyValueChanged(value);
    void this.updateActionType(value);
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.moveActiveSuggestion(1);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.moveActiveSuggestion(-1);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const active = this.activeSuggestion();
      const value = active ? active.address : this.inputValue().trim();
      if (!value) {
        return;
      }
      void this.executeAction(value, event.ctrlKey || event.metaKey);
    } else if (event.key === 'Escape') {
      this.api.dismissActionDialog();
    }
  }

  private moveActiveSuggestion(direction: number) {
    const suggestions = this.suggestions();
    if (suggestions.length === 0) return;

    const currentIndex = this.activeSuggestionIndex();
    let nextIndex: number;

    if (currentIndex < 0) {
      nextIndex = direction > 0 ? 0 : suggestions.length - 1;
    } else {
      nextIndex =
        (currentIndex + direction + suggestions.length) % suggestions.length;
    }

    this.hasManualNavigation.set(true);
    this.suppressInitialSelection.set(false);
    this.activeSuggestionIndex.set(nextIndex);
    void this.evaluateActionType(suggestions[nextIndex].address);
  }

  selectSuggestion(suggestion: NavigationSuggestion) {
    this.inputValue.set(suggestion.address);
    this.hasManualNavigation.set(true);
    this.suppressInitialSelection.set(false);
    void this.evaluateActionType(suggestion.address);
    void this.executeAction(suggestion.address, false);
  }

  private async executeAction(value: string, ctrl: boolean) {
    await this.api.execute(value, ctrl);
    await this.api.dismissActionDialog();
  }
}
