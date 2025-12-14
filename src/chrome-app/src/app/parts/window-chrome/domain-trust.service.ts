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

type CacheEntry = {
  rating: DomainTrustRating | null;
  expiresAt: number;
};

const CACHE_TTL_MS = 7 * 24 * 60 * 60 * 1000; // one week
const STORAGE_PREFIX = 'domain-trust-rating:';

@Injectable({ providedIn: 'root' })
export class DomainTrustService {
  private readonly trustpilotBaseUrl =
    'https://r.jina.ai/https://www.trustpilot.com/review/';

  private readonly memoryCache = new Map<string, CacheEntry>();

  async lookup(
    domain: string,
    signal?: AbortSignal
  ): Promise<DomainTrustRating | null> {
    const normalized = this.normalizeDomain(domain);
    if (normalized === null) {
      return null;
    }

    const cached = this.readCache(normalized);
    if (cached !== undefined) {
      return cached;
    }

    try {
      const rating = await this.fetchTrustpilotScore(normalized, signal);
      this.writeCache(normalized, rating);
      return rating;
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw error;
      }
      // Network failure or parser issues result in "unknown" for the caller,
      // but we don't cache the failure so a later attempt can retry.
      this.evictCache(normalized);
      return null;
    }
  }

  normalizeDomain(domain: string): string | null {
    const trimmed = domain.trim();
    if (trimmed.length === 0) {
      return null;
    }

    try {
      const parsed = new URL(`http://${trimmed}`);
      const hostname = parsed.hostname.toLowerCase();
      if (this.isLocalhost(hostname)) {
        return null;
      }
      if (parsed.port.length > 0) {
        return null;
      }
      return this.stripWww(hostname);
    } catch {
      const simple = trimmed.toLowerCase();
      if (this.isLocalhost(simple)) {
        return null;
      }
      if (simple.includes(':')) {
        return null;
      }
      return this.stripWww(simple);
    }
  }

  private stripWww(hostname: string): string {
    return hostname.replace(/^www\./, '');
  }

  private isLocalhost(hostname: string): boolean {
    return (
      hostname === 'localhost' ||
      hostname === '127.0.0.1' ||
      hostname === '::1' ||
      hostname === '0.0.0.0'
    );
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
    const normalized = page.replace(/\s+/g, ' ');

    const titleMatch =
      /is rated "[^"]+" with\s+([0-9]+(?:[.,][0-9]+)?)\s*\/\s*5/i.exec(
        normalized
      );
    if (titleMatch) {
      const score = this.parseScore(titleMatch[1]);
      if (score !== null) {
        return this.clampScore(score);
      }
    }

    const aggregateMatch =
      /"@type"\s*:\s*"AggregateRating"[\s\S]*?"ratingValue"\s*:\s*"?([0-9]+(?:[.,][0-9]+)?)/i.exec(
        page
      );
    if (aggregateMatch) {
      const score = this.parseScore(aggregateMatch[1]);
      if (score !== null) {
        return this.clampScore(score);
      }
    }

    const trustScoreMatch = /TrustScore[^0-9]*([0-5](?:[.,][0-9]+)?)/i.exec(
      page
    );
    if (trustScoreMatch) {
      const score = this.parseScore(trustScoreMatch[1]);
      if (score !== null) {
        return this.clampScore(score);
      }
    }

    return null;
  }

  private parseScore(raw: string): number | null {
    const numeric = Number.parseFloat(raw.replace(',', '.'));
    // Are rating of 0 indicates that there are no reviews.
    if (Number.isNaN(numeric) || numeric <= 0) {
      return null;
    }
    return numeric;
  }

  private clampScore(value: number): number {
    return Math.min(5, Math.max(0, value));
  }

  private roundToStars(value: number): TrustStarScore {
    const rounded = Math.round(value);
    const constrained = Math.min(5, Math.max(1, rounded));
    return constrained as TrustStarScore;
  }

  private readCache(domain: string): DomainTrustRating | null | undefined {
    const now = Date.now();
    const inMemory = this.memoryCache.get(domain);
    if (inMemory) {
      if (inMemory.expiresAt > now) {
        return inMemory.rating;
      }
      this.memoryCache.delete(domain);
    }

    const storage = this.getStorage();
    if (!storage) {
      return undefined;
    }

    const raw = storage.getItem(this.storageKey(domain));
    if (!raw) {
      return undefined;
    }

    try {
      const parsed = JSON.parse(raw) as CacheEntry;
      if (parsed.expiresAt <= now) {
        storage.removeItem(this.storageKey(domain));
        return undefined;
      }

      this.memoryCache.set(domain, parsed);
      return parsed.rating;
    } catch {
      storage.removeItem(this.storageKey(domain));
      return undefined;
    }
  }

  private writeCache(domain: string, rating: DomainTrustRating | null): void {
    const entry: CacheEntry = {
      rating,
      expiresAt: Date.now() + CACHE_TTL_MS,
    };
    this.memoryCache.set(domain, entry);

    const storage = this.getStorage();
    if (!storage) {
      return;
    }

    try {
      storage.setItem(this.storageKey(domain), JSON.stringify(entry));
    } catch {
      // Ignore storage quota or availability errors.
    }
  }

  private evictCache(domain: string): void {
    this.memoryCache.delete(domain);
    const storage = this.getStorage();
    storage?.removeItem(this.storageKey(domain));
  }

  private getStorage(): Storage | null {
    try {
      if (typeof window === 'undefined' || !window.localStorage) {
        return null;
      }
      return window.localStorage;
    } catch {
      return null;
    }
  }

  private storageKey(domain: string): string {
    return `${STORAGE_PREFIX}${domain}`;
  }
}
