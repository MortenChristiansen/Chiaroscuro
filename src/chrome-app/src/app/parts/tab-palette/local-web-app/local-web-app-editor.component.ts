import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faFolderOpen,
  faFloppyDisk,
  faPlay,
  faServer,
  faStop,
  faTrash,
  faCircleExclamation,
} from '@fortawesome/free-solid-svg-icons';
import { IconButtonComponent } from '../../../shared/icon-button.component';
import { exposeApiToBackend, loadBackendApi } from '../../interfaces/api';
import { LocalWebAppApi } from './localWebAppApi';
import { TabPaletteControlSectionComponent } from '../containers/control-section.component';

@Component({
  selector: 'local-web-app-editor',
  imports: [
    CommonModule,
    IconButtonComponent,
    FaIconComponent,
    TabPaletteControlSectionComponent,
  ],
  template: `
    <tab-palette-control-section [title]="'Local Web App'" [icon]="serverIcon">
      @if (tabId()) {
        <div
          class="rounded-lg border border-slate-800/70 bg-slate-950/40 p-3 shadow-inner"
        >
          <div class="flex flex-col gap-3">
            <!-- Directory input -->
            <div class="flex flex-col gap-1">
              <label class="text-xs text-slate-400">Project directory</label>
              <div class="flex gap-2">
                <input
                  #dirInput
                  type="text"
                  class="flex-1 rounded-md border border-slate-800/80 bg-slate-950/60 px-3 py-1.5 text-sm text-slate-100 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-500/40"
                  placeholder="/path/to/project"
                  [value]="directoryPath()"
                  (input)="onDirectoryChange(dirInput.value)"
                  spellcheck="false"
                />
                <icon-button
                  title="Browse for directory"
                  (click)="browseDirectory()"
                >
                  <fa-icon class="text-sky-300" [icon]="folderIcon" />
                </icon-button>
              </div>
            </div>

            <!-- Command input -->
            <div class="flex flex-col gap-1">
              <label class="text-xs text-slate-400">Start command</label>
              <input
                #cmdInput
                type="text"
                class="w-full rounded-md border border-slate-800/80 bg-slate-950/60 px-3 py-1.5 text-sm text-slate-100 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-500/40"
                placeholder="npm start"
                [value]="startCommand()"
                (input)="onCommandChange(cmdInput.value)"
                spellcheck="false"
              />
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-2">
              <icon-button
                title="Save configuration"
                (click)="saveConfig()"
                [disabled]="!canSave()"
              >
                <fa-icon class="text-sky-300" [icon]="saveIcon" />
              </icon-button>

              @if (hasConfig()) {
                <icon-button
                  title="Delete configuration"
                  (click)="deleteConfig()"
                >
                  <fa-icon class="text-rose-300" [icon]="deleteIcon" />
                </icon-button>
              }

              @if (hasErrors()) {
                <fa-icon
                  class="text-amber-400"
                  [icon]="errorIcon"
                  title="Process has errors - check terminal"
                />
              }
            </div>
          </div>

          <!-- Status -->
          <div class="mt-3 text-xs">
            @if (isRunning()) {
              <span class="text-emerald-400">
                <fa-icon [icon]="playIcon" class="mr-1" /> Process is running
              </span>
            } @else if (hasConfig()) {
              <span class="text-amber-300">
                <fa-icon [icon]="stopIcon" class="mr-1" /> Process will start
                when tab is activated
              </span>
            } @else {
              <span class="text-slate-500">
                Configure a local dev server to auto-start with this tab.
              </span>
            }
          </div>
        </div>
      } @else {
        <div
          class="rounded-lg border border-slate-800/70 bg-slate-950/40 p-3 text-xs text-slate-500"
        >
          Select a bookmarked or pinned tab to configure local web app settings.
        </div>
      }
    </tab-palette-control-section>
  `,
})
export class LocalWebAppEditorComponent implements OnInit {
  tabId = signal<string | null>(null);
  directoryPath = signal('');
  startCommand = signal('');
  isRunning = signal(false);
  hasErrors = signal(false);
  hasConfig = signal(false);

  protected readonly serverIcon = faServer;
  protected readonly folderIcon = faFolderOpen;
  protected readonly saveIcon = faFloppyDisk;
  protected readonly playIcon = faPlay;
  protected readonly stopIcon = faStop;
  protected readonly deleteIcon = faTrash;
  protected readonly errorIcon = faCircleExclamation;

  private api!: LocalWebAppApi;

  canSave() {
    return this.directoryPath().trim().length > 0;
  }

  async ngOnInit() {
    this.api = await loadBackendApi<LocalWebAppApi>('localWebAppApi');

    exposeApiToBackend({
      initLocalWebAppConfig: (
        tabId: string,
        directoryPath: string | null,
        startCommand: string | null,
        isRunning: boolean,
        hasErrors: boolean,
      ) => {
        this.tabId.set(tabId);
        this.directoryPath.set(directoryPath ?? '');
        this.startCommand.set(startCommand ?? '');
        this.isRunning.set(isRunning);
        this.hasErrors.set(hasErrors);
        this.hasConfig.set(!!directoryPath);
      },
      updateLocalWebAppConfig: (
        tabId: string,
        directoryPath: string,
        startCommand: string | null,
      ) => {
        if (this.tabId() === tabId) {
          this.directoryPath.set(directoryPath);
          this.startCommand.set(startCommand ?? '');
          this.hasConfig.set(!!directoryPath);
        }
      },
      clearLocalWebAppConfig: (tabId: string) => {
        if (this.tabId() === tabId) {
          this.directoryPath.set('');
          this.startCommand.set('');
          this.hasConfig.set(false);
          this.isRunning.set(false);
          this.hasErrors.set(false);
        }
      },
      localWebAppDirectorySelected: (tabId: string, directoryPath: string) => {
        if (this.tabId() === tabId) {
          this.directoryPath.set(directoryPath);
        }
      },
      updateLocalWebAppProcessStatus: (
        tabId: string,
        isRunning: boolean,
        hasErrors: boolean,
      ) => {
        if (this.tabId() === tabId) {
          this.isRunning.set(isRunning);
          this.hasErrors.set(hasErrors);
        }
      },
    });
  }

  onDirectoryChange(value: string) {
    this.directoryPath.set(value);
  }

  onCommandChange(value: string) {
    this.startCommand.set(value);
  }

  async browseDirectory() {
    const tid = this.tabId();
    if (tid) {
      await this.api.browseDirectory(tid);
    }
  }

  async saveConfig() {
    const tid = this.tabId();
    const dir = this.directoryPath().trim();
    if (tid && dir) {
      const cmd = this.startCommand().trim() || null;
      await this.api.saveConfig(tid, dir, cmd);
    }
  }

  async deleteConfig() {
    const tid = this.tabId();
    if (tid) {
      await this.api.deleteConfig(tid);
    }
  }
}
