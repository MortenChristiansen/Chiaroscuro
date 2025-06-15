export interface Api {
  back: () => Promise<void>;
  canGoBack: () => Promise<boolean>;
  forward: () => Promise<void>;
  canGoForward: () => Promise<boolean>;
  navigate: (url: string) => Promise<void>;
  dismissActionDialog: () => Promise<void>;
}
