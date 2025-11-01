import { Component, input, output } from '@angular/core';

@Component({
  selector: 'icon-button',
  template: `
    <button
      #button
      [disabled]="disabled()"
      class="icon-btn"
      (click)="onClick.emit($event)"
      (mousedown)="button.classList.add('pressed')"
      (mouseup)="button.classList.remove('pressed')"
      (mouseleave)="button.classList.remove('pressed')"
    >
      <ng-content></ng-content>
    </button>
  `,
  styles: `
    .icon-btn {
      background: transparent;
      border: none;
      color: #e5e7eb;
      font-family: Consolas;
      border-radius: 0.375rem;
      font-weight: 400;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.15rem 0.35rem;
      cursor: pointer;
      transition: background 0.15s, color 0.15s;
    }
    .icon-btn:hover:not(:disabled) {
      background: rgba(255,255,255,0.08);
      color: #fff;
    }
    .icon-btn:disabled {
      opacity: 0.5;
      cursor: default;
    }
    .pressed {
      background: rgba(255,255,255,0.18) !important;
      color: #fff;
    }
  `,
})
export class IconButtonComponent {
  disabled = input(false);
  onClick = output<Event>();
}
