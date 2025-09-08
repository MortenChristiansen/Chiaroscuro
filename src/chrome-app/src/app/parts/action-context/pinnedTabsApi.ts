import { Api } from '../interfaces/api';
import { TabId } from './server-models';

export interface PinnedTabsApi extends Api {
  unpinTab: (id: TabId) => void;
  activateTab: (id: TabId) => void;
  returnToOriginalAddress: (id: TabId) => void;
}
