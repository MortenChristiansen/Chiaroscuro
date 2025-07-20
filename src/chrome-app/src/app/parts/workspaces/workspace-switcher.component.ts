import { Component, effect, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspaceListApi, WorkspaceStateDto } from './workspaceListApi';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import WorkspaceEditorComponent from './workspace-editor.component';

@Component({
  selector: 'workspace-switcher',
  imports: [CommonModule, WorkspaceEditorComponent],
  template: `
    <div class="flex items-center justify-center px-4 py-3 space-x-2">
      <!-- Edit button -->
      <button
        (click)="openEditDialog()"
        class="p-2 rounded-full hover:bg-white/10 transition-colors duration-200 text-gray-400 hover:text-white"
        title="Edit current workspace"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
          <path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.287 4.287 0 0 1-1.897 1.13L6 18l.8-2.685a4.287 4.287 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10" />
        </svg>
      </button>

      <!-- Workspace icons -->
      <div class="flex items-center space-x-1">
        @for (workspace of workspaces(); track workspace.Id) {
          <button
            (click)="activateWorkspace(workspace.Id)"
            class="p-2 rounded-lg transition-all duration-200 text-lg {{ workspace.Id === activeWorkspaceId() ? 'bg-white/20 shadow-lg transform scale-110' : 'hover:bg-white/10' }}"
            [title]="workspace.Name"
          >
            {{ workspace.Icon }}
          </button>
        }
      </div>

      <!-- Add workspace button -->
      <button
        (click)="openCreateDialog()"
        class="p-2 rounded-full hover:bg-white/10 transition-colors duration-200 text-gray-400 hover:text-white"
        title="Create new workspace"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
      </button>
    </div>

    @if (showEditor()) {
      <workspace-editor
        [isEdit]="isEditMode()"
        [workspaceData]="editorData()"
        (save)="onSaveWorkspace($event)"
        (delete)="onDeleteWorkspace()"
        (cancel)="closeEditor()"
      />
    }
  `,
  styles: ``
})
export default class WorkspaceSwitcherComponent implements OnInit {
  workspaces = signal<WorkspaceStateDto[]>([]);
  activeWorkspaceId = signal<string>('');
  showEditor = signal(false);
  isEditMode = signal(false);
  editorData = signal({ name: '', icon: '🌐', color: '#2563eb' });
  private previousWorkspaceIndex = -1;

  api!: WorkspaceListApi;

  constructor() {
    effect(() => {
      const activeWorkspace = this.workspaces().find(w => w.Id === this.activeWorkspaceId());
      if (activeWorkspace) {
        // Update background color with animation
        this.updateBackgroundColor(activeWorkspace.Color);
      }
    });
  }

  async ngOnInit() {
    this.api = await loadBackendApi<WorkspaceListApi>('workspacesApi');

    exposeApiToBackend({
      workspacesChanged: (workspaces: WorkspaceStateDto[], activeWorkspaceId: string) => {
        const currentIndex = this.workspaces().findIndex(w => w.Id === activeWorkspaceId);
        const newIndex = workspaces.findIndex(w => w.Id === activeWorkspaceId);
        
        // Determine slide direction for tab animation
        let slideDirection = '';
        if (this.previousWorkspaceIndex !== -1 && this.previousWorkspaceIndex !== newIndex) {
          slideDirection = this.previousWorkspaceIndex < newIndex ? 'slide-left' : 'slide-right';
        }
        
        this.workspaces.set(workspaces);
        this.activeWorkspaceId.set(activeWorkspaceId);
        this.previousWorkspaceIndex = newIndex;
        
        // Notify tab list about workspace change with animation direction
        if (slideDirection && (window as any).angularApi && (window as any).angularApi.setTabsWithAnimation) {
          // The actual tab data will be set by the backend when the workspace is activated
          // This is just to ensure the animation direction is communicated
        }
      }
    });
  }

  activateWorkspace(workspaceId: string) {
    if (workspaceId !== this.activeWorkspaceId()) {
      this.api.activateWorkspace(workspaceId);
    }
  }

  openCreateDialog() {
    this.isEditMode.set(false);
    this.editorData.set({ name: '', icon: '🌐', color: '#2563eb' });
    this.showEditor.set(true);
  }

  openEditDialog() {
    const activeWorkspace = this.workspaces().find(w => w.Id === this.activeWorkspaceId());
    if (activeWorkspace) {
      this.isEditMode.set(true);
      this.editorData.set({
        name: activeWorkspace.Name,
        icon: activeWorkspace.Icon,
        color: activeWorkspace.Color
      });
      this.showEditor.set(true);
    }
  }

  onSaveWorkspace(data: { name: string; icon: string; color: string }) {
    if (this.isEditMode()) {
      this.api.updateWorkspace(this.activeWorkspaceId(), data.name, data.icon, data.color);
    } else {
      this.api.createWorkspace(data.name, data.icon, data.color);
    }
    this.closeEditor();
  }

  onDeleteWorkspace() {
    this.api.deleteWorkspace(this.activeWorkspaceId());
    this.closeEditor();
  }

  closeEditor() {
    this.showEditor.set(false);
  }

  private updateBackgroundColor(color: string) {
    if (typeof document !== 'undefined') {
      document.documentElement.style.setProperty('--workspace-color', color);
      
      // Animate background color change
      document.body.style.transition = 'background-color 300ms cubic-bezier(0.4, 0, 0.2, 1)';
      document.body.style.backgroundColor = color;
      
      // Remove transition after animation completes to avoid interfering with other changes
      setTimeout(() => {
        document.body.style.transition = '';
      }, 300);
    }
  }
}