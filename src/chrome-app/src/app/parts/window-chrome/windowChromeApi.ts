import { Api } from '../interfaces/api';
import { DomainTrustRating } from './domain-trust.service';

export interface WindowsChromeApi extends Api {
  minimize: () => Promise<void>;
  maximize: () => Promise<void>;
  close: () => Promise<void>;
  back: () => Promise<void>;
  canGoBack: () => Promise<boolean>;
  forward: () => Promise<void>;
  canGoForward: () => Promise<boolean>;
  reload: () => Promise<void>;
  copyAddress: () => Promise<void>;
  isLoading: () => Promise<boolean>;
  onLoaded: () => Promise<void>;
  getIsMaximized: () => Promise<boolean>;
  getDomainTrustRating: (domain: string) => Promise<DomainTrustRating | null>;
}
