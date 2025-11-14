import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faFile,
  faGlobe,
  faLock,
  faLockOpen,
  faSliders,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons';

type UrlViewModel =
  | {
      kind: 'web';
      icon: IconDefinition;
      iconTitle: string;
      domain: string;
      path: string | null;
      pathTitle: string | null;
    }
  | {
      kind: 'file' | 'other';
      icon: IconDefinition;
      iconTitle: string;
      display: string;
    };

@Component({
  selector: 'url-display',
  imports: [CommonModule, FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block min-w-0',
  },
  template: `
    @let vm = viewModel();
    <div class="surface">
      @if (vm === null) {
      <span class="placeholder">Press ctrl-t to start</span>
      } @else {
      <div class="icon">
        <fa-icon size="sm" [icon]="vm.icon" [title]="vm.iconTitle" />
      </div>
      @if (vm.kind === 'web') {
      <span class="domain" title="{{ vm.domain }}">{{ vm.domain }}</span>
      @if (vm.path) {
      <span class="path" title="{{ vm.pathTitle }}">{{ vm.path }}</span>
      } } @else {
      <span class="rest" title="{{ vm.display }}">{{ vm.display }}</span>
      } }
    </div>
  `,
  styles: [
    `
      .surface {
        display: flex;
        align-items: center;
        width: 100%;
        min-width: 0;
        gap: 0.25rem;
      }

      .icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        color: white;
        flex-shrink: 0;
      }

      .domain {
        color: white;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        flex: 0 1 auto;
        min-width: 0;
      }

      .path {
        color: rgba(206, 212, 235, 0.75);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        flex: 1 20 0%;
        min-width: 0;
      }

      .rest {
        color: rgba(226, 232, 240, 0.95);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        flex: 1;
        min-width: 0;
      }

      .placeholder {
        color: rgba(148, 163, 184, 0.85);
        font-style: italic;
      }
    `,
  ],
})
export default class UrlDisplayComponent {
  readonly url = input<string | null>(null);

  readonly viewModel = computed<UrlViewModel | null>(() => {
    const rawUrl = this.url();
    if (rawUrl === null) {
      return null;
    }

    const normalizedUrl = rawUrl.trim().toLowerCase();
    if (normalizedUrl.length === 0) {
      return null;
    }

    if (
      normalizedUrl.startsWith('http://') ||
      normalizedUrl.startsWith('https://')
    ) {
      try {
        const parsed = new URL(normalizedUrl);
        const path = parsed.pathname === '/' ? null : parsed.pathname;
        const isSecure = normalizedUrl.startsWith('https://');
        const iconTitle = isSecure
          ? 'Secure connection (HTTPS)'
          : 'Insecure connection (HTTP)';

        return {
          kind: 'web',
          icon: isSecure ? faLock : faLockOpen,
          domain: parsed.host,
          path,
          pathTitle: rawUrl,
          iconTitle,
        } satisfies UrlViewModel;
      } catch {
        return {
          kind: 'other',
          icon: faGlobe,
          display: normalizedUrl,
          iconTitle: 'Invalid web address',
        } satisfies UrlViewModel;
      }
    }

    if (normalizedUrl.startsWith('file://')) {
      const display = this.parseFileUrl(normalizedUrl);
      return {
        kind: 'file',
        icon: faFile,
        display,
        iconTitle: 'Local file',
      } satisfies UrlViewModel;
    }

    if (/^\/[a-z0-9-]+/i.test(normalizedUrl)) {
      return {
        kind: 'other',
        icon: faSliders,
        display: normalizedUrl,
        iconTitle: 'System page',
      } satisfies UrlViewModel;
    }

    return {
      kind: 'other',
      icon: faGlobe,
      display: normalizedUrl,
      iconTitle: 'Unknown protocol',
    } satisfies UrlViewModel;
  });

  private parseFileUrl(value: string): string {
    try {
      const parsed = new URL(value);
      const raw = `${parsed.pathname}${parsed.search}${parsed.hash}`;
      const decoded = decodeURIComponent(raw);
      return decoded.startsWith('/') ? decoded.slice(1) : decoded;
    } catch {
      return value.replace(/^file:\/\//i, '');
    }
  }
}
