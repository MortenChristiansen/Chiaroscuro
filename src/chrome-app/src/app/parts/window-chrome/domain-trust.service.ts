import { Injectable } from '@angular/core';
import { Api, loadBackendApi } from '../interfaces/api';

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
  private readonly backendApi = loadBackendApi<DomainTrustBackendApi>();

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
      console.warn(
        `DomainTrustService: Failed to fetch trust rating for ${normalized}:`,
        error
      );
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
    this.throwIfAborted(signal);
    const api = await this.backendApi;
    this.throwIfAborted(signal);

    const rating = await api.getDomainTrustRating(domain);
    this.throwIfAborted(signal);

    if (!rating) {
      return null;
    }

    return rating;
  }

  private throwIfAborted(signal?: AbortSignal): void {
    if (!signal?.aborted) {
      return;
    }

    // Match fetch() abort behavior so existing callers keep working.
    throw new DOMException('Aborted', 'AbortError');
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

interface DomainTrustBackendApi extends Api {
  getDomainTrustRating: (domain: string) => Promise<DomainTrustRating | null>;
}
