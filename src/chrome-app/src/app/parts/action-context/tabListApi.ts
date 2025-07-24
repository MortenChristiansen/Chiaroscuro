import { Api } from '../interfaces/api';
import { TabId } from './server-models';

export interface TabStateDto {
  Id: TabId;
  Title: string | null;
  Favicon: string | null;
  IsActive: boolean;
  Created: Date;
}

export interface TabListApi extends Api {
  activateTab: (tabId: TabId) => Promise<void>;
  closeTab: (tabId: TabId) => Promise<void>;
  tabsChanged: (tabs: TabStateDto[], ephemeralTabStartIndex: number) => void;
  updateFolderName: (folderId: string, name: string) => Promise<void>;
}
