export interface Api {
  uiLoaded: (source: 'WindowChrome' | 'ActionDialog' | 'Tabs') => Promise<void>;

  back: () => Promise<void>;
  canGoBack: () => Promise<boolean>;
  forward: () => Promise<void>;
  canGoForward: () => Promise<boolean>;
  navigate: (url: string) => Promise<void>;
  reload: () => Promise<void>;
  dismissActionDialog: () => Promise<void>;
  minimize: () => Promise<void>;
  maximize: () => Promise<void>;
  close: () => Promise<void>;
}
