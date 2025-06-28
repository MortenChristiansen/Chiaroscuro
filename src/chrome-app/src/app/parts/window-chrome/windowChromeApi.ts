import { Api } from '../interfaces/api';

export interface WindowsChromeApi extends Api {
  minimize: () => Promise<void>;
  maximize: () => Promise<void>;
  close: () => Promise<void>;
  back: () => Promise<void>;
  canGoBack: () => Promise<boolean>;
  forward: () => Promise<void>;
  canGoForward: () => Promise<boolean>;
  reload: () => Promise<void>;
}
