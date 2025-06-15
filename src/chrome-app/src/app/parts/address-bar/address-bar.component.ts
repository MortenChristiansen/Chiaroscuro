import {
  Component,
  ElementRef,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { Api } from '../interfaces/api';

@Component({
  selector: 'address-bar',
  imports: [],
  template: `
    <div class="menu">
      <button (click)="back()" [disabled]="!canGoBack()">Back</button>
      <button (click)="forward()" [disabled]="!canGoForward()">Forward</button>
      <input
        placeholder="Url"
        type="text"
        value="https://google.com"
        (keydown.enter)="navigate()"
        #addressBar
      />
    </div>
  `,
  styles: `
    .menu {
    display: flex;
    gap: 1rem;
  }

  input {
    width: 50rem
  }
  `,
})
export default class AddressBarComponent implements OnInit {
  async ngOnInit() {
    await (window as any).CefSharp.BindObjectAsync('api');
    this.api = (window as any).api;

    (window as any).angularApi = {
      changeAddress: async (url: string) => {
        this.addressBar()!.nativeElement.value = url;
        this.canGoBack.set(await this.api.canGoBack());
        this.canGoForward.set(await this.api.canGoForward());
      },
    };
  }
  addressBar = viewChild<ElementRef<HTMLInputElement>>('addressBar');
  canGoBack = signal(false);
  canGoForward = signal(false);

  api!: Api;

  back() {
    this.api.back();
  }

  forward() {
    this.api.forward();
  }

  navigate() {
    this.api.navigate(this.addressBar()!.nativeElement.value);
  }
}
