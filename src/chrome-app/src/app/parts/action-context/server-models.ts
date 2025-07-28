export type WorkspaceId = string;
export type TabId = string;
export type FolderId = string;

export interface WorkspaceDescription {
  id: WorkspaceId;
  name: string;
  icon: string;
  color: string;
}

export interface Workspace extends WorkspaceDescription {
  tabs: Tab[];
  ephemeralTabStartIndex: number;
  activeTabId: TabId | null;
  folders: Folder[];
}

export interface Tab {
  id: TabId;
  title: string | null;
  favicon: string | null;
  created: Date;
}

export interface Folder {
  id: FolderId;
  name: string;
  startIndex: number;
  endIndex: number;
}
