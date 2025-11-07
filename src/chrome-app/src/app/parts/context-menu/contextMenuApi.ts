import { Api } from '../interfaces/api';

export interface ContextMenuApi extends Api {
  dismissContextMenu: () => Promise<void>;
}
