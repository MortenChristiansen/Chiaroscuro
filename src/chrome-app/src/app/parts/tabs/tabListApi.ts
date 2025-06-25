import { Api } from '../interfaces/api';
import { TabId } from './tabs-list.component';

export interface TabListApi extends Api {
  activateTab: (tabId: TabId) => Promise<void>;
}
