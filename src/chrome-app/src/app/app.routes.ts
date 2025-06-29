import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./parts/window-chrome/window-chrome.component'),
  },
  {
    path: 'action-dialog',
    loadComponent: () =>
      import('./parts/action-dialog/action-dialog.component'),
  },
  {
    path: 'actions-host',
    loadComponent: () => import('./parts/actions-host/actions-host.component'),
  },
];
