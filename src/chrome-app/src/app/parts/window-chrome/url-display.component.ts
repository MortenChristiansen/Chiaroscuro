import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faFile,
  faFaceFrown,
  faFaceGrimace,
  faFaceGrin,
  faFaceMeh,
  faFaceSmile,
  faGlobe,
  faLock,
  faLockOpen,
  faSliders,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons';
import {
  DomainTrustRating,
  DomainTrustService,
  TrustStarScore,
} from './domain-trust.service';

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

type TrustLookupState =
  | { status: 'idle' }
  | { status: 'loading'; domain: string }
  | { status: 'success'; domain: string; rating: DomainTrustRating }
  | { status: 'unknown'; domain: string }
  | { status: 'error'; domain: string; message: string };

type TrustIndicatorViewModel = {
  icon: IconDefinition;
  title: string;
  color: string;
};

@Component({
  selector: 'url-display',
  imports: [CommonModule, FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block min-w-0',
  },
  template: `
    @let vm = viewModel(); @let trust = trustIndicator();
    <div class="surface">
      @if (vm === null) {
      <span class="placeholder">Press ctrl-t to start</span>
      } @else { @if (vm.kind === 'web' && trust) {
      <div class="trust-icon" [style.color]="trust.color">
        <fa-icon size="sm" [icon]="trust.icon" [title]="trust.title" />
      </div>
      }
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

      .icon,
      .trust-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        color: white;
        flex-shrink: 0;
      }

      .trust-icon {
        transition: color 150ms ease, opacity 150ms ease;
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
  private readonly domainTrustService = inject(DomainTrustService);

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

  private readonly trustState = signal<TrustLookupState>({ status: 'idle' });

  constructor() {
    effect((onCleanup) => {
      const vm = this.viewModel();
      if (vm === null || vm.kind !== 'web') {
        this.trustState.set({ status: 'idle' });
        return;
      }

      const domain = vm.domain;
      const lookupDomain = vm.domain.split(':')[0];
      const currentState = untracked(() => this.trustState());
      if (
        currentState.status !== 'idle' &&
        currentState.domain === domain &&
        (currentState.status === 'loading' ||
          currentState.status === 'success' ||
          currentState.status === 'unknown')
      ) {
        return;
      }

      const controller = new AbortController();
      this.trustState.set({ status: 'loading', domain });

      this.domainTrustService
        .lookup(lookupDomain, controller.signal)
        .then((rating) => {
          if (controller.signal.aborted) {
            return;
          }
          if (rating) {
            this.trustState.set({ status: 'success', domain, rating });
            return;
          }
          this.trustState.set({ status: 'unknown', domain });
        })
        .catch((error) => {
          if (controller.signal.aborted) {
            return;
          }
          const message =
            error instanceof Error ? error.message : 'Unknown error';
          this.trustState.set({ status: 'error', domain, message });
        });

      onCleanup(() => controller.abort());
    });
  }

  private readonly trustIconMap: Record<TrustStarScore, IconDefinition> = {
    1: faFaceGrimace,
    2: faFaceFrown,
    3: faFaceMeh,
    4: faFaceSmile,
    5: faFaceGrin,
  };

  private readonly trustColorMap: Record<TrustStarScore, string> = {
    1: '#f87171',
    2: '#fb923c',
    3: '#facc15',
    4: '#86efac',
    5: '#4ade80',
  };

  readonly trustIndicator = computed<TrustIndicatorViewModel | null>(() => {
    const vm = this.viewModel();
    if (vm === null || vm.kind !== 'web') {
      return null;
    }

    const state = this.trustState();
    if (state.status === 'success' && state.domain === vm.domain) {
      const { rating } = state;
      return {
        icon: this.trustIconMap[rating.stars],
        title: `${
          rating.source === 'trustpilot' ? 'Trustpilot rating' : 'Trust score'
        }: ${rating.score.toFixed(1)} / 5`,
        color: this.trustColorMap[rating.stars],
      } satisfies TrustIndicatorViewModel;
    }
    return null;
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
