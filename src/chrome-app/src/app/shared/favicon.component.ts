import { Component, input, signal, effect } from '@angular/core';

@Component({
  selector: 'app-favicon',
  template: `
    @if (showFallback()) {
      <img 
        [src]="fallbackFavicon" 
        [class]="cssClass()"
        alt="Default favicon" 
      />
    } @else {
      <img 
        [src]="src()" 
        [class]="cssClass()"
        (error)="onLoadError()"
        alt="Favicon" 
      />
    }
  `,
})
export class FaviconComponent {
  src = input<string | null>(null);
  cssClass = input<string>('w-4 h-4');
  
  showFallback = signal(false);
  
  fallbackFavicon = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" rx="4" fill="%23bbb"/><text x="8" y="12" text-anchor="middle" font-size="10" fill="white" font-family="Arial">★</text></svg>';

  constructor() {
    // Reset fallback state when src changes
    effect(() => {
      const faviconSrc = this.src();
      this.showFallback.set(!faviconSrc);
    });
  }

  onLoadError() {
    this.showFallback.set(true);
  }
}