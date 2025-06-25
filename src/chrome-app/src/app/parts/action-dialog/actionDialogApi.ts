import { Api } from '../interfaces/api';

export interface ActionDialogApi extends Api {
  navigate: (url: string) => Promise<void>;
  dismissActionDialog: () => Promise<void>;
}
