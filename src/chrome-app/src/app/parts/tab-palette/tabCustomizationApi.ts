import { Api } from '../interfaces/api';

export interface TabCustomizationApi extends Api {
  setCustomTitle: (title: string | null) => Promise<void>;
}
