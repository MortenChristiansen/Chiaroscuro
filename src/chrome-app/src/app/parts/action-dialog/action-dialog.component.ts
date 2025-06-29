import { Component, ElementRef, OnInit, viewChild, signal, effect } from '@angular/core';
import { ActionDialogApi, NavigationSuggestion } from './actionDialogApi';
import { loadBackendApi, exposeApiToBackend } from '../interfaces/api';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'action-dialog',
  imports: [CommonModule],
  template: `
    <div
      class="fixed inset-0 bg-transparent z-[1000]"
      (click)="api.dismissActionDialog()"
    ></div>
    <div
      class="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-[1001] bg-white/95 rounded-2xl shadow-2xl p-8 min-w-[350px] flex flex-col items-center"
      (keydown.esc)="api.dismissActionDialog()"
    >
      <input
        class="w-[30rem] text-lg px-4 py-3 rounded-lg border border-gray-300 outline-none shadow-sm bg-white/80 placeholder-gray-400"
        placeholder="Where to?"
        type="text"
        (keydown)="onKeyDown($event)"
        (input)="onInputChange($event)"
        #dialog
      />
      @if (suggestions().length > 0) {
        <div class="w-[30rem] mt-2 bg-white rounded-lg border border-gray-200 shadow-lg max-h-64 overflow-y-auto">
          @for (suggestion of suggestions(); track suggestion.address) {
            <div 
              class="flex items-center px-4 py-3 hover:bg-gray-50 cursor-pointer border-b last:border-b-0"
              [class.bg-blue-50]="$index === activeSuggestionIndex()"
              (click)="selectSuggestion(suggestion)"
            >
              @if (suggestion.favicon) {
                <img [src]="suggestion.favicon" class="w-4 h-4 mr-3 flex-shrink-0" alt="">
              } @else {
                <div class="w-4 h-4 mr-3 flex-shrink-0 bg-gray-300 rounded"></div>
              }
              <div class="flex-1 min-w-0">
                <div class="font-medium text-gray-900 truncate">{{ suggestion.title }}</div>
                <div class="text-sm text-gray-500 truncate">{{ suggestion.address }}</div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export default class ActionDialogComponent implements OnInit {
  dialog = viewChild<ElementRef<HTMLInputElement>>('dialog');
  suggestions = signal<NavigationSuggestion[]>([]);
  activeSuggestionIndex = signal<number>(-1);
  private userTypedText = '';
  private debounceTimer?: any;
  private isUpdatingInput = false;

  constructor() {
    // Reset active suggestion when suggestions change
    effect(() => {
      const sigs = this.suggestions();
      if (sigs.length > 0) {
        this.activeSuggestionIndex.set(0);
        this.updateInputWithSuggestion();
      } else {
        this.activeSuggestionIndex.set(-1);
      }
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<ActionDialogApi>();

    exposeApiToBackend({
      showDialog: () => {
        this.dialog()!.nativeElement.value = '';
        this.userTypedText = '';
        this.suggestions.set([]);
        this.activeSuggestionIndex.set(-1);
        this.dialog()!.nativeElement.focus();
      },
      updateSuggestions: (suggestions: NavigationSuggestion[]) => {
        this.suggestions.set(suggestions);
      },
    });

    await this.api.uiLoaded();
  }

  api!: ActionDialogApi;

  onInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const value = input.value;
    
    // Only update userTypedText if we're not in the middle of programmatically updating the input
    if (!this.isUpdatingInput) {
      this.userTypedText = value;
    }

    // Clear debounce timer
    if (this.debounceTimer) {
      (typeof window !== 'undefined' ? window.clearTimeout : clearTimeout)(this.debounceTimer);
    }

    // Debounce the API call
    this.debounceTimer = (typeof window !== 'undefined' ? window.setTimeout : setTimeout)(() => {
      this.api.notifyValueChanged(this.userTypedText);
    }, 300);
  }

  onKeyDown(event: KeyboardEvent) {
    const input = event.target as HTMLInputElement;
    
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.moveActiveSuggestion(1);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.moveActiveSuggestion(-1);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      this.executeAction(input.value, event.ctrlKey);
    } else if (event.key === 'Escape') {
      this.api.dismissActionDialog();
    }
  }

  private moveActiveSuggestion(direction: number) {
    const suggestions = this.suggestions();
    if (suggestions.length === 0) return;

    const currentIndex = this.activeSuggestionIndex();
    let newIndex = currentIndex + direction;

    if (newIndex < 0) {
      newIndex = suggestions.length - 1;
    } else if (newIndex >= suggestions.length) {
      newIndex = 0;
    }

    this.activeSuggestionIndex.set(newIndex);
    this.updateInputWithSuggestion();
  }

  private updateInputWithSuggestion() {
    const suggestions = this.suggestions();
    const activeIndex = this.activeSuggestionIndex();
    
    if (activeIndex >= 0 && activeIndex < suggestions.length && this.userTypedText) {
      const suggestion = suggestions[activeIndex];
      const input = this.dialog()!.nativeElement;
      
      // Set flag to prevent onInputChange from updating userTypedText
      this.isUpdatingInput = true;
      
      // Set the full suggestion text
      input.value = suggestion.address;
      
      // Select the auto-completed part
      const userTextLength = this.userTypedText.length;
      input.setSelectionRange(userTextLength, suggestion.address.length);
      
      // Reset flag after a short delay to allow the input event to fire
      setTimeout(() => {
        this.isUpdatingInput = false;
      }, 0);
    }
  }

  selectSuggestion(suggestion: NavigationSuggestion) {
    this.executeAction(suggestion.address, false);
  }

  private async executeAction(value: string, useCurrentTab: boolean) {
    await this.api.navigate(value, useCurrentTab);
    await this.api.dismissActionDialog();
  }
}
