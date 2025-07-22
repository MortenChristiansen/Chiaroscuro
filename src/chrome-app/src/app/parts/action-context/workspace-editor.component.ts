import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface WorkspaceFormData {
  name: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'workspace-editor',
  imports: [CommonModule, FormsModule],
  template: `
    <div
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
    >
      <div class="bg-gray-800 rounded-lg p-6 w-96 max-w-full mx-4">
        <h2 class="text-xl font-semibold text-white mb-4">
          {{ isEdit() ? 'Edit Workspace' : 'Create New Workspace' }}
        </h2>

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
            />
          </div>
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-300 mb-2">
              Icon
            </label>
            <div
              class="grid grid-cols-8 gap-2 max-h-32 overflow-y-auto border border-gray-600 rounded-md p-2 bg-gray-700"
            >
              @for (icon of availableIcons; track icon) {
              <button
                type="button"
                (click)="formData.icon = icon"
                class="p-2 rounded hover:bg-gray-600 text-lg {{
                  formData.icon === icon ? 'bg-blue-600' : ''
                }}"
              >
                {{ icon }}
              </button>
              }
            </div>
          </div>
          <div class="mb-6">
            <label class="block text-sm font-medium text-gray-300 mb-2">
              Background Color
            </label>
            <div class="grid grid-cols-8 gap-2">
              @for (color of availableColors; track color) {
              <button
                type="button"
                (click)="formData.color = color"
                class="w-8 h-8 rounded border-2 {{
                  formData.color === color ? 'border-white' : 'border-gray-600'
                }}"
                [style.background-color]="color"
              ></button>
              }
            </div>
          </div>
          <div class="flex justify-between">
            <div>
              @if (isEdit()) {
              <button
                [disabled]="!canDelete()"
                type="button"
                (click)="onDelete()"
                class="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50"
              >
                Delete
              </button>
              }
            </div>
            <div class="space-x-2">
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
          </div>
        </form>
      </div>
    </div>
  `,
  styles: ``,
})
export default class WorkspaceEditorComponent {
  isEdit = input(false);
  workspaceData = input<WorkspaceFormData>({
    name: '',
    icon: '🌐',
    color: '#2563eb',
  });
  canDelete = input<boolean>(true);
  save = output<WorkspaceFormData>();
  delete = output<void>();
  cancel = output<void>();

  formData: WorkspaceFormData = { ...this.workspaceData() };

  availableIcons = [
    '🌐',
    '📁',
    '💼',
    '🏠',
    '⚡',
    '🎯',
    '🚀',
    '💻',
    '📊',
    '🎨',
    '🔧',
    '📝',
    '🎵',
    '🎮',
    '🏆',
    '💡',
    '🔬',
    '📚',
    '☕',
    '🌟',
    '🎬',
    '📸',
    '🛠️',
    '🎪',
    '🌈',
    '🔥',
    '❄️',
    '🌙',
    '☀️',
    '⭐',
    '🌍',
    '🎳',
  ];

  availableColors = [
    '#ef4444',
    '#f97316',
    '#f59e0b',
    '#eab308',
    '#84cc16',
    '#22c55e',
    '#10b981',
    '#14b8a6',
    '#06b6d4',
    '#0ea5e9',
    '#3b82f6',
    '#6366f1',
    '#8b5cf6',
    '#a855f7',
    '#d946ef',
    '#ec4899',
    '#f43f5e',
    '#64748b',
    '#6b7280',
    '#374151',
  ];

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
