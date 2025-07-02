import { Component, OnInit } from '@angular/core';
import { loadBackendApi } from '../interfaces/api';
import { FileDownloadsApi } from './fileDownloadsApi';

@Component({
  selector: 'downloads-list',
  template: ` FILE DOWNLOADS `,
  styles: `
  `,
})
export default class DownloadsListComponent implements OnInit {
  api!: FileDownloadsApi;

  async ngOnInit() {
    this.api = await loadBackendApi<FileDownloadsApi>('fileDownloadsApi');
  }
}
