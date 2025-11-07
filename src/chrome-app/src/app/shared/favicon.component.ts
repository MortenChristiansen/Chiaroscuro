import { Component, input, signal, effect } from '@angular/core';
import { faCog } from '@fortawesome/free-solid-svg-icons';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

@Component({
  selector: 'favicon',
  template: `
    @let icon = tryGetPageFavicon(src() ?? ''); @if(icon.success) {
    <fa-icon [icon]="icon.icon" />
    } @else if (showFallback()) {
    <img [src]="fallbackFavicon" alt="Default favicon" />
    } @else {
    <img [src]="src()" (error)="onLoadError()" alt="Favicon" />
    }
  `,
  imports: [FaIconComponent],
  styles: `
      img {
        height: 100%;
        width: 100%;
      }
    `,
})
export class FaviconComponent {
  src = input<string | null>(null);

  showFallback = signal(false);
  fallbackFavicon =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" rx="4" fill="%23bbb"/><text x="8" y="12" text-anchor="middle" font-size="10" fill="white" font-family="Arial">★</text></svg>';

  private readonly faviconMap: Record<string, any> = {
    'fa:settings': faCog,
  };

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

  tryGetPageFavicon(icon: string) {
    const normalizedIcon = icon.toLowerCase();
    if (normalizedIcon in this.faviconMap) {
      return { success: true, icon: this.faviconMap[normalizedIcon] };
    }

    return { success: false };
  }
}
