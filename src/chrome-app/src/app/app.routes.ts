import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./parts/address-bar/address-bar.component'),
  },
  {
    path: 'action-dialog',
    loadComponent: () =>
      import('./parts/action-dialog/action-dialog.component'),
  },
];
