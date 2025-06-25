import { Component, ElementRef, OnInit, viewChild } from '@angular/core';
import { ActionDialogApi } from './actionDialogApi';

@Component({
  selector: 'action-dialog',
  imports: [],
  template: `
    <div class="glass-overlay" (click)="api.dismissActionDialog()"></div>
    <div class="centered-dialog" (keydown.esc)="api.dismissActionDialog()">
      <input
        class="action-dialog"
        placeholder="Where to?"
        type="text"
        (keydown.enter)="execute(dialog.value)"
        #dialog
      />
    </div>
  `,
  styles: `
  
  input {
    width: 30rem;
    font-size: 1.1rem;
    padding: 0.7rem 1rem;
    border-radius: 0.5rem;
    border: 1px solid #ccc;
    outline: none;
    box-shadow: 0 2px 8px rgba(0,0,0,0.04);
    background: rgba(255,255,255,0.8);
  }

  .glass-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(0, 0, 0, 0.01);
    z-index: 1000;
  }

  .centered-dialog {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    z-index: 1001;
    background: rgba(255, 255, 255, 0.95);
    border-radius: 1rem;
    box-shadow: 0 8px 32px 0 rgba(31, 38, 135, 0.37);
    padding: 2rem;
    min-width: 350px;
    display: flex;
    flex-direction: column;
    align-items: center;
  }
  `,
})
export default class ActionDialogComponent implements OnInit {
  dialog = viewChild<ElementRef<HTMLInputElement>>('dialog');

  async ngOnInit() {
    await (window as any).CefSharp.BindObjectAsync('api');
    this.api = (window as any).api;

    (window as any).angularApi = {
      showDialog: () => {
        this.dialog()!.nativeElement.value = '';
        this.dialog()!.nativeElement.focus();
      },
    };

    await this.api.uiLoaded();
  }

  api!: ActionDialogApi;

  async execute(value: string) {
    await this.api.navigate(value);
    await this.api.dismissActionDialog();
  }
}
