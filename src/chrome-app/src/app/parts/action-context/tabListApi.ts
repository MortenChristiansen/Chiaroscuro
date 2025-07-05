import { Api } from '../interfaces/api';
import { TabId } from './tabs-list.component';

export interface TabStateDto {
  Id: TabId;
  Title: string | null;
  Favicon: string | null;
  IsActive: boolean;
}

export interface TabListApi extends Api {
  activateTab: (tabId: TabId) => Promise<void>;
  closeTab: (tabId: TabId) => Promise<void>;
  reorderTab: (tabId: TabId, newIndex: number) => Promise<void>;
  tabsChanged: (tabs: TabStateDto[]) => void;
}
