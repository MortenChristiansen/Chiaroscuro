import { Api } from '../interfaces/api';

export interface ContextMenuApi extends Api {
  dismissContextMenu: () => Promise<void>;
  downloadImage: (imageUrl: string) => Promise<void>;
  copyImage: (imageUrl: string) => Promise<void>;
}
