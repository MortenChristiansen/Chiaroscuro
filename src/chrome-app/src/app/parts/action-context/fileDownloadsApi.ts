import { Api } from '../interfaces/api';

export interface DownloadItem {
  id: number;
  fileName: string;
  progress: number; // 0-100
  isCompleted: boolean;
  isCancelled: boolean;
}

export interface FileDownloadsApi extends Api {
  cancelDownload: (id: number) => void;
}
