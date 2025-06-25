import { Component, ElementRef, OnInit, viewChild } from '@angular/core';
import { ActionDialogApi } from './actionDialogApi';
import { loadBackendApi, exposeApiToBackend } from '../interfaces/api';

@Component({
  selector: 'action-dialog',
  imports: [],
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
        (keydown.enter)="execute(dialog.value)"
        #dialog
      />
    </div>
  `,
})
export default class ActionDialogComponent implements OnInit {
  dialog = viewChild<ElementRef<HTMLInputElement>>('dialog');

  async ngOnInit() {
    this.api = await loadBackendApi<ActionDialogApi>();

    exposeApiToBackend({
      showDialog: () => {
        this.dialog()!.nativeElement.value = '';
        this.dialog()!.nativeElement.focus();
      },
    });

    await this.api.uiLoaded();
  }

  api!: ActionDialogApi;

  async execute(value: string) {
    await this.api.navigate(value);
    await this.api.dismissActionDialog();
  }
}
