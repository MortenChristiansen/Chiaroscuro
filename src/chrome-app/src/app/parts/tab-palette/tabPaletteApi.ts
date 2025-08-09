import { Api } from '../interfaces/api';

export interface TabPaletteApi extends Api {
  find(term: string): Promise<void>;
  nextMatch(term: string): Promise<void>;
  prevMatch(term: string): Promise<void>;
  stopFinding(): Promise<void>;
}
