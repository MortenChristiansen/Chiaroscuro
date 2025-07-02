import { Injectable, signal } from '@angular/core';
import { DownloadItem, FileDownloadApi } from './fileDownloadApi';
import { loadBackendApi, exposeApiToBackend } from '../interfaces/api';

@Injectable({
  providedIn: 'root'
})
export class DownloadsService {
  downloads = signal<DownloadItem[]>([]);
  
  private api?: FileDownloadApi;
  private completedDownloads = new Map<string, number>(); // downloadId -> timestamp

  async initialize() {
    if (this.api) return;
    
    // Skip initialization during server-side rendering
    if (typeof window === 'undefined') {
      return;
    }
    
    try {
      this.api = await loadBackendApi<FileDownloadApi>();
      
      exposeApiToBackend({
        downloadsChanged: (downloads: DownloadItem[]) => {
          this.updateDownloads(downloads);
        }
      });

      await this.api.uiLoaded();
    } catch (error) {
      console.warn('Failed to initialize downloads API:', error);
    }
  }

  async cancelDownload(downloadId: string) {
    if (!this.api) return;
    
    try {
      await this.api.cancelDownload(downloadId);
    } catch (error) {
      console.error('Failed to cancel download:', error);
    }
  }

  private updateDownloads(newDownloads: DownloadItem[]) {
    const now = Date.now();
    
    // Track completed downloads for 10-second delay
    newDownloads.forEach(download => {
      if (download.isCompleted && !this.completedDownloads.has(download.id)) {
        this.completedDownloads.set(download.id, now);
      }
    });

    // Remove downloads that completed more than 10 seconds ago
    const filteredDownloads = newDownloads.filter(download => {
      if (!download.isCompleted) return true;
      
      const completedTime = this.completedDownloads.get(download.id);
      if (!completedTime) return true;
      
      const shouldRemove = now - completedTime > 10000; // 10 seconds
      if (shouldRemove) {
        this.completedDownloads.delete(download.id);
        return false;
      }
      
      return true;
    });

    this.downloads.set(filteredDownloads);
  }
}