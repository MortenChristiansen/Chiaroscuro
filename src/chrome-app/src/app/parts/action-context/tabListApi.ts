import { Api } from '../interfaces/api';
import { FolderId, TabId } from './server-models';

export interface TabStateDto {
  Id: TabId;
  Title: string | null;
  Favicon: string | null;
  IsActive: boolean;
  Created: Date;
}

export interface FolderIndexStateDto {
  Id: FolderId;
  Name: string;
  StartIndex: number;
  EndIndex: number;
}

export interface TabListApi extends Api {
  activateTab: (tabId: TabId) => Promise<void>;
  closeTab: (tabId: TabId) => Promise<void>;
  tabsChanged: (
    tabs: TabStateDto[],
    ephemeralTabStartIndex: number,
    folders: FolderIndexStateDto[]
  ) => void;
}
