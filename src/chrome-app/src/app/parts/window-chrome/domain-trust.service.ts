import { Injectable } from '@angular/core';

export type TrustStarScore = 1 | 2 | 3 | 4 | 5;

export interface DomainTrustRating {
  source: 'trustpilot';
  /** Raw 5-star score reported by the provider (0.0 - 5.0). */
  score: number;
  /** Rounded star score used for smiley mapping. */
  stars: TrustStarScore;
  fetchedAt: number;
}

@Injectable({ providedIn: 'root' })
export class DomainTrustService {
  private readonly trustpilotBaseUrl =
    'https://r.jina.ai/https://www.trustpilot.com/review/';

  /** Cache already fetched ratings to avoid duplicate remote calls. */
  private readonly cache = new Map<string, DomainTrustRating | null>();

  async lookup(
    domain: string,
    signal?: AbortSignal
  ): Promise<DomainTrustRating | null> {
    const normalized = this.normalizeDomain(domain);
    if (this.cache.has(normalized)) {
      return this.cache.get(normalized) ?? null;
    }

    try {
      const rating = await this.fetchTrustpilotScore(normalized, signal);
      this.cache.set(normalized, rating);
      return rating;
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw error;
      }
      // Network failure or parser issues result in "unknown" for the caller,
      // but we don't cache the failure so a later attempt can retry.
      this.cache.delete(normalized);
      return null;
    }
  }

  private normalizeDomain(domain: string): string {
    return domain
      .trim()
      .toLowerCase()
      .replace(/^www\./, '');
  }

  private async fetchTrustpilotScore(
    domain: string,
    signal?: AbortSignal
  ): Promise<DomainTrustRating | null> {
    const endpoint = `${this.trustpilotBaseUrl}${encodeURIComponent(domain)}`;
    const response = await fetch(endpoint, { signal, cache: 'no-store' });
    if (!response.ok) {
      return null;
    }

    const payload = await response.text();
    const score = this.extractTrustpilotScore(payload);
    if (score === null) {
      return null;
    }

    return {
      source: 'trustpilot',
      score,
      stars: this.roundToStars(score),
      fetchedAt: Date.now(),
    } satisfies DomainTrustRating;
  }

  private extractTrustpilotScore(page: string): number | null {
    const patterns = [
      /"trustScore"\s*:?\s*([0-9]+(?:[.,][0-9]+)?)/i,
      /"TrustScore"\s*:?\s*([0-9]+(?:[.,][0-9]+)?)/i,
      /"ratingValue"\s*:?\s*"([0-9]+(?:[.,][0-9]+)?)"/i,
      /TrustScore[^0-9]*([0-9]+(?:[.,][0-9]+)?)/i,
    ];

    for (const pattern of patterns) {
      const match = pattern.exec(page);
      if (!match) {
        continue;
      }
      const numeric = Number.parseFloat(match[1].replace(',', '.'));
      if (!Number.isNaN(numeric)) {
        return this.clampScore(numeric);
      }
    }

    return null;
  }

  private clampScore(value: number): number {
    return Math.min(5, Math.max(0, value));
  }

  private roundToStars(value: number): TrustStarScore {
    const rounded = Math.round(value);
    const constrained = Math.min(5, Math.max(1, rounded));
    return constrained as TrustStarScore;
  }
}
