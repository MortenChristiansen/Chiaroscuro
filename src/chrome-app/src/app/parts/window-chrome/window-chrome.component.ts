import { Component, OnInit, signal } from '@angular/core';
import { Api } from '../interfaces/api';

@Component({
  selector: 'address-bar',
  imports: [],
  template: `
    <div class="menu">
      <div class="app-icon">🎨</div>
      <div class="address-bar">
        <button (click)="back()" [disabled]="!canGoBack()">←</button>
        <button (click)="forward()" [disabled]="!canGoForward()">→</button>
        <button (click)="reload()">↺</button>
        <span class="address">
          {{ address() }}
        </span>
      </div>
      <div class="window-controls">
        <button (click)="min()">🗕</button>
        <button (click)="max()">🗖</button>
        <button (click)="close()">✕</button>
      </div>
    </div>
  `,
  styles: `
    .menu {
      display: flex;
      align-items: center;
      gap: 1rem;
      vertical-align: middle;
    }

    .app-icon {
      /* stays left by default */
    }

    .address-bar {
      flex: 1;
      display: flex;
      gap: .5rem;
      align-items: center;
      justify-content: center;
    }

    .window-controls {
      display: flex;
      gap: 0.5rem;
      margin-left: auto;
    }

    .address {
      margin: 0 1rem;
      font-family: Calibri;
      color: #ddd;
      max-width: 400px;
      text-overflow: ellipsis;
      overflow: hidden;
      white-space: nowrap;
    }

    button {
      background:rgba(0, 0, 0, 0.01);
      border: none;
      color: #eee;
      font-size: 1rem;
      cursor: pointer;
      transition: font-weight 0.1s, color 0.1s;
      font-family: Consolas;
      padding: 0.25rem 0.5rem;
    }
    button:hover:not(:disabled) {
      font-weight: bold;
      color: #fff;
    }
    button:disabled {
      opacity: 0.5;
      cursor: default;
    }
  `,
})
export default class AddressBarComponent implements OnInit {
  async ngOnInit() {
    await (window as any).CefSharp.BindObjectAsync('api');
    this.api = (window as any).api;

    (window as any).angularApi = {
      changeAddress: async (url: string) => {
        this.address.set(url);
        this.canGoBack.set(await this.api.canGoBack());
        this.canGoForward.set(await this.api.canGoForward());
      },
    };
  }
  canGoBack = signal(false);
  canGoForward = signal(false);
  address = signal<string>('https://google.com');

  api!: Api;

  back() {
    this.api.back();
  }

  forward() {
    this.api.forward();
  }

  reload() {
    this.api.reload();
  }

  min() {
    this.api.minimize();
  }

  max() {
    this.api.maximize();
  }

  close() {
    this.api.close();
  }
}
