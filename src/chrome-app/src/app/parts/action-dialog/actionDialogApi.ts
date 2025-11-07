import { Api } from '../interfaces/api';

export interface NavigationSuggestion {
  address: string;
  title: string;
  favicon: string | null;
}

export type ActionType = 'Navigate' | 'Search' | 'OpenSystemPage';

export interface ActionDialogApi extends Api {
  execute: (command: string, ctrl: boolean) => Promise<void>;
  dismissActionDialog: () => Promise<void>;
  notifyValueChanged: (value: string) => Promise<void>;
  getActionType: (command: string) => Promise<ActionType>;
}
