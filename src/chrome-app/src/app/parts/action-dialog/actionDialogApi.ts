import { Api } from '../interfaces/api';

export interface ActionDialogApi extends Api {
  navigate: (url: string, useCurrentTab: boolean) => Promise<void>;
  dismissActionDialog: () => Promise<void>;
}
