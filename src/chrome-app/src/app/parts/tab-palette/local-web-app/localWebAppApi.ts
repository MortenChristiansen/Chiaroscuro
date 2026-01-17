import { Api } from '../../interfaces/api';

export interface LocalWebAppApi extends Api {
  saveConfig: (
    tabId: string,
    directoryPath: string,
    startCommand: string | null
  ) => Promise<void>;
  deleteConfig: (tabId: string) => Promise<void>;
  browseDirectory: (tabId: string) => Promise<void>;
}
