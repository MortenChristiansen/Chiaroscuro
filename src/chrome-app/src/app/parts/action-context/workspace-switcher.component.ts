import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import WorkspaceEditorComponent from './workspace-editor.component';
import { WorkspaceListApi } from './workspaceListApi';
import { Workspace, WorkspaceDescription } from './server-models';

@Component({
  selector: 'workspace-switcher',
  imports: [CommonModule, WorkspaceEditorComponent],
  template: `
    <div class="flex items-center">
      <!-- Edit button -->
      <button
        (click)="openEditDialog()"
        class="p-2 rounded-full hover:bg-white/10 transition-colors duration-200 text-gray-400 hover:text-white shrink-0"
        title="Edit current workspace"
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
      <!-- Workspace icons -->
      <div class="flex-1 min-w-0 flex justify-center">
        <div
          class="flex flex-wrap items-center gap-x-1 gap-y-1 py-1 px-1 justify-center"
        >
          @for (workspace of workspaces(); track workspace.id) {
          <button
            (click)="activateWorkspace(workspace.id)"
            class="p-0.5 rounded-lg transition-all duration-200 text-md flex flex-col items-center justify-center {{
              workspace.id === activeWorkspaceId()
                ? 'opacity-100'
                : 'opacity-70 hover:opacity-100'
            }}"
            [title]="workspace.name"
            style="background: none; box-shadow: none;"
          >
            <span>{{ workspace.icon }}</span>
            @if(workspace.id === activeWorkspaceId()) {
            <span class="block w-6 h-0.5 mt-1 rounded bg-blue-500"></span>
            }
          </button>
          }
        </div>
      </div>
      <!-- Add workspace button -->
      <button
        (click)="openCreateDialog()"
        class="p-2 rounded-full hover:bg-white/10 transition-colors duration-200 text-gray-400 hover:text-white flex-shrink-0"
        title="Create new workspace"
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
            d="M12 4.5v15m7.5-7.5h-15"
          />
        </svg>
      </button>
    </div>
    @if (showEditor()) {
    <workspace-editor
      [isEdit]="isEditMode()"
      [workspaceData]="editorData()"
      [canDelete]="workspaces().length > 1"
      (save)="onSaveWorkspace($event)"
      (delete)="onDeleteWorkspace()"
      (cancel)="closeEditor()"
      animate.enter="dialog-enter"
      animate.leave="dialog-leave"
    />
    }
  `,
  styles: `
  

      .dialog-enter {
        animation: scale-opacity-enter 220ms ease-in forwards;
      }

      .dialog-leave {
        animation: scale-opacity-leave 220ms ease-out forwards;
      }
  `,
})
export default class WorkspaceSwitcherComponent implements OnInit {
  workspaces = signal<Workspace[]>([]);
  activeWorkspaceId = signal<string>('');
  showEditor = signal(false);
  isEditMode = signal(false);
  editorData = signal({ name: '', icon: '🌐', color: '#2563eb' });

  api!: WorkspaceListApi;

  async ngOnInit() {
    this.api = await loadBackendApi<WorkspaceListApi>('workspacesApi');

    exposeApiToBackend({
      setWorkspaces: (workspaces: Workspace[]) => {
        this.workspaces.set(workspaces);
        this.activeWorkspaceId.set(workspaces[0].id);
        this.api.onLoaded();
      },
      workspaceActivated: (workspaceId: string) => {
        this.activeWorkspaceId.set(workspaceId);
      },
      workspacesChanged: (workspaces: WorkspaceDescription[]) => {
        this.workspaces.update((current) =>
          workspaces.map((ws) => {
            const existing = current.find((w) => w.id === ws.id);
            if (existing) {
              return { ...existing, ...ws };
            } else
              return {
                id: ws.id,
                name: ws.name,
                icon: ws.icon,
                color: ws.color,
                tabs: [],
                ephemeralTabStartIndex: 0,
                activeTabId: null,
                folders: [],
              } as Workspace;
          })
        );
      },
    });
  }

  activateWorkspace(workspaceId: string) {
    if (workspaceId !== this.activeWorkspaceId()) {
      this.api.activateWorkspace(workspaceId);
    }
  }

  openCreateDialog() {
    this.isEditMode.set(false);
    this.editorData.set(WorkspaceEditorComponent.defaultValues);
    this.showEditor.set(true);
  }

  openEditDialog() {
    const activeWorkspace = this.workspaces().find(
      (w) => w.id === this.activeWorkspaceId()
    );
    if (activeWorkspace) {
      this.isEditMode.set(true);
      this.editorData.set({
        name: activeWorkspace.name,
        icon: activeWorkspace.icon,
        color: activeWorkspace.color,
      });
      this.showEditor.set(true);
    }
  }

  onSaveWorkspace(data: { name: string; icon: string; color: string }) {
    if (this.isEditMode()) {
      this.api.updateWorkspace(
        this.activeWorkspaceId(),
        data.name,
        data.icon,
        data.color
      );
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
}
