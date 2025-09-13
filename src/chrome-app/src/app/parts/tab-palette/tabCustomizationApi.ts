import { Api } from '../interfaces/api';

export interface TabCustomizationApi extends Api {
  setCustomTitle: (title: string | null) => Promise<void>;
  setDisableFixedAddress: (disabled: boolean) => Promise<void>;
  setNotificationPermission: (permissionStatus: number) => Promise<void>;
}
