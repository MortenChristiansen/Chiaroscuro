import { Api } from '../interfaces/api';

export interface TerminalApi extends Api {
  clearTerminal: (tabId: string) => Promise<void>;
}
