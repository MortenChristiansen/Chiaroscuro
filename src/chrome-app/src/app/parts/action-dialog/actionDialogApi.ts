import { Api } from '../interfaces/api';

export interface NavigationSuggestion {
  address: string;
  title: string;
  favicon: string | null;
}

export interface ActionDialogApi extends Api {
  navigate: (url: string, useCurrentTab: boolean) => Promise<void>;
  dismissActionDialog: () => Promise<void>;
  notifyValueChanged: (value: string) => Promise<void>;
}
