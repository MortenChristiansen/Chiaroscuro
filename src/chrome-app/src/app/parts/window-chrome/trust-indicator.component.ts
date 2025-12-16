import {
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
  faFaceFrown,
  faFaceGrimace,
  faFaceGrin,
  faFaceMeh,
  faFaceSmile,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons';
import {
  DomainTrustRating,
  DomainTrustService,
  TrustStarScore,
} from './domain-trust.service';

type TrustLookupState =
  | { status: 'idle' }
  | { status: 'loading'; domain: string }
  | { status: 'success'; domain: string; rating: DomainTrustRating }
  | { status: 'unknown'; domain: string }
  | { status: 'error'; domain: string; message: string };

interface TrustIconViewModel {
  icon: IconDefinition;
  title: string;
  color: string;
}

@Component({
  selector: 'trust-indicator',
  imports: [FaIconComponent],
  host: {
    class: 'inline-flex items-center',
  },
  template: `
    @let vm = viewModel(); @if (vm) {
    <div class="trust-icon" [style.color]="vm.color">
      <fa-icon size="sm" [icon]="vm.icon" [title]="vm.title" />
    </div>
    }
  `,
  styles: [
    `
      :host {
        display: inline-flex;
      }

      .trust-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        color: white;
        flex-shrink: 0;
        transition: color 150ms ease, opacity 150ms ease;
      }
    `,
  ],
})
export default class TrustIndicatorComponent {
  readonly domain = input<string | null>(null);
  private readonly domainTrustService = inject(DomainTrustService);
  private readonly trustState = signal<TrustLookupState>({ status: 'idle' });

  constructor() {
    effect((onCleanup) => {
      const normalizedDomain = this.domainTrustService.normalizeDomain(
        this.domain() ?? ''
      );
      if (!normalizedDomain) {
        this.trustState.set({ status: 'idle' });
        return;
      }

      const currentState = untracked(() => this.trustState());
      if (
        currentState.status !== 'idle' &&
        currentState.domain === normalizedDomain &&
        (currentState.status === 'loading' ||
          currentState.status === 'success' ||
          currentState.status === 'unknown')
      ) {
        return;
      }

      const controller = new AbortController();
      this.trustState.set({ status: 'loading', domain: normalizedDomain });

      this.domainTrustService
        .lookup(normalizedDomain, controller.signal)
        .then((rating) => {
          if (controller.signal.aborted) {
            return;
          }
          if (rating) {
            this.trustState.set({
              status: 'success',
              domain: normalizedDomain,
              rating,
            });
            return;
          }
          this.trustState.set({ status: 'unknown', domain: normalizedDomain });
        })
        .catch((error) => {
          if (controller.signal.aborted) {
            return;
          }
          const message =
            error instanceof Error ? error.message : 'Unknown error';
          this.trustState.set({
            status: 'error',
            domain: normalizedDomain,
            message,
          });
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

  readonly viewModel = computed<TrustIconViewModel | null>(() => {
    const normalizedDomain = this.domainTrustService.normalizeDomain(
      this.domain() ?? ''
    );
    if (!normalizedDomain) {
      return null;
    }

    const state = this.trustState();
    if (state.status !== 'success' || state.domain !== normalizedDomain) {
      return null;
    }

    const { rating } = state;
    return {
      icon: this.trustIconMap[rating.stars],
      title: `Trustpilot rating: ${rating.score.toFixed(1)} / 5`,
      color: this.trustColorMap[rating.stars],
    } satisfies TrustIconViewModel;
  });
}
