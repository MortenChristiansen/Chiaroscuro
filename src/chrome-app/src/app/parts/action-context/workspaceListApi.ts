import { Api } from '../interfaces/api';

export interface WorkspaceStateDto {
  Id: string;
  Name: string;
  Icon: string;
  Color: string;
}

export interface WorkspaceListApi extends Api {
  activateWorkspace: (workspaceId: string) => Promise<void>;
  createWorkspace: (name: string, icon: string, color: string) => Promise<void>;
  updateWorkspace: (
    workspaceId: string,
    name: string,
    icon: string,
    color: string
  ) => Promise<void>;
  deleteWorkspace: (workspaceId: string) => Promise<void>;
}
