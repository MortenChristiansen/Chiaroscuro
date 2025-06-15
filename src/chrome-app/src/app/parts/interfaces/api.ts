export interface Api {
  back: () => void;
  canGoBack: () => boolean;
  forward: () => void;
  canGoForward: () => boolean;
  navigate: (url: string) => void;
  dismissActionDialog: () => void;
}
