import { Api } from '../interfaces/api';

export interface DomainCustomizationApi extends Api {
  setCssEnabled: (enabled: boolean) => Promise<void>;
  editCss: () => Promise<void>;
  removeCss: () => Promise<void>;
}