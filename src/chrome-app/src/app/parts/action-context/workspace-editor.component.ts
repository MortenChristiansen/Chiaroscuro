import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { workspaceColors, workspaceIcons } from './data';

interface WorkspaceFormData {
  name: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'workspace-editor',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fixed inset-0 flex items-center justify-center z-50">
      <div
        class="border rounded-xl border-slate-800/70 bg-slate-900/90 p-3 xxs:p-6 w-96 max-w-full mx-4 relative"
      >
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-xl font-semibold text-white">
            {{ isEdit() ? 'Edit Workspace' : 'Create New Workspace' }}
          </h2>
          @if (isEdit()){
          <a
            href="#"
            (click)="onDelete(); $event.preventDefault()"
            [class.opacity-50]="!canDelete()"
            [attr.aria-disabled]="!canDelete()"
            class="text-red-400 hover:text-red-600 font-medium underline cursor-pointer transition-opacity duration-150"
            [style.pointer-events]="canDelete() ? 'auto' : 'none'"
          >
            Delete
          </a>
          }
        </div>
        <form (ngSubmit)="onSubmit()" #form="ngForm">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-300 mb-2">
              Name
            </label>
            <input
              type="text"
              [(ngModel)]="formData.name"
              name="name"
              required
              class="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-md text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Workspace name"
              spellcheck="false"
            />
          </div>
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-300 mb-2">
              Icon
            </label>
            <div
              class="grid grid-cols-4 xxs:grid-cols-6 xs:grid-cols-8 gap-1 max-h-32 overflow-y-auto overflow-x-hidden border border-gray-600 rounded-md p-1 bg-gray-700"
            >
              @for (icon of availableIcons; track icon) {
              <button
                type="button"
                (click)="formData.icon = icon"
                class="w-8 h-8 flex items-center justify-center text-lg rounded hover:bg-gray-600 {{
                  formData.icon === icon ? 'bg-blue-600' : ''
                }}"
              >
                <span class="text-center">{{ icon }}</span>
              </button>
              }
            </div>
          </div>
          <div class="mb-6">
            <label class="block text-sm font-medium text-gray-300 mb-2">
              Background Color
            </label>
            <div class="grid grid-cols-4 xxs:grid-cols-6 xs:grid-cols-8 gap-1">
              @for (color of availableColors; track color) {
              <button
                type="button"
                (click)="formData.color = color"
                class="w-full aspect-square rounded border-2 {{
                  formData.color === color ? 'border-white' : 'border-gray-600'
                }}"
                [style.background-color]="color"
              ></button>
              }
            </div>
          </div>
          <div class="flex justify-end space-x-2 mt-6">
            <button
              type="button"
              (click)="onCancel()"
              class="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-gray-500"
            >
              Cancel
            </button>
            <button
              type="submit"
              [disabled]="!form.valid"
              class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ isEdit() ? 'Update' : 'Create' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export default class WorkspaceEditorComponent {
  public static readonly defaultValues: WorkspaceFormData = {
    name: '',
    icon: '🌐',
    color: workspaceColors[0],
  };

  isEdit = input(false);
  workspaceData = input<WorkspaceFormData>(
    WorkspaceEditorComponent.defaultValues
  );
  canDelete = input<boolean>(true);
  save = output<WorkspaceFormData>();
  delete = output<void>();
  cancel = output<void>();

  formData: WorkspaceFormData = { ...this.workspaceData() };

  availableIcons = workspaceIcons;
  availableColors = workspaceColors;

  ngOnInit() {
    this.formData = { ...this.workspaceData() };
  }

  onSubmit() {
    if (this.formData.name.trim()) {
      this.save.emit(this.formData);
    }
  }

  onDelete() {
    if (
      confirm(
        'Are you sure you want to delete this workspace? This action cannot be undone.'
      )
    ) {
      this.delete.emit();
    }
  }

  onCancel() {
    this.cancel.emit();
  }
}
