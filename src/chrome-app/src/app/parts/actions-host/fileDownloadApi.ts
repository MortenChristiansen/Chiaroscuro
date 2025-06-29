import { Api } from '../interfaces/api';

export interface DownloadItem {
  id: string;
  fileName: string;
  progress: number; // 0-100
  isCompleted: boolean;
  isCancelled: boolean;
}

export interface FileDownloadApi extends Api {
  cancelDownload: (downloadId: string) => Promise<void>;
  downloadsChanged: (downloads: DownloadItem[]) => void;
}