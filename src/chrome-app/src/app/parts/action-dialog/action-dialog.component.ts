import { Component, OnInit } from '@angular/core';
import { Api } from '../interfaces/api';

@Component({
  selector: 'action-dialog',
  imports: [],
  template: `
    <div class="glass-overlay" (click)="api.dismissActionDialog()"></div>
    <div class="centered-dialog" (keydown.esc)="api.dismissActionDialog()">
      <input
        class="action-dialog"
        autofocus="true"
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
  `,
})
export default class ActionDialogComponent implements OnInit {
  async ngOnInit() {
    console.log('ActionDialogComponent initializing');
    await (window as any).CefSharp.BindObjectAsync('api');
    this.api = (window as any).api;
    console.log('ActionDialogComponent initialized');
  }

  api!: Api;

  async execute(value: string) {
    console.log(`Navigating to: "${value}"`);
    await this.api.navigate(value);
    await this.api.dismissActionDialog();
  }
}
