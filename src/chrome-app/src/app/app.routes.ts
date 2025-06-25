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
    path: 'tabs',
    loadComponent: () => import('./parts/tabs/tabs-list.component'),
  },
];
