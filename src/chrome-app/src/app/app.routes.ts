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
    path: 'action-context',
    loadComponent: () =>
      import('./parts/action-context/action-context.component'),
  },
  {
    path: 'tab-palette',
    loadComponent: () => import('./parts/tab-palette/tab-palette.component'),
  },
];
