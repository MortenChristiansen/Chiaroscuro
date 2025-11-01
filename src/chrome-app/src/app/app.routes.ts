import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./parts/window-chrome/window-chrome.component'),
    title: 'ChromeApp',
  },
  {
    path: 'action-dialog',
    loadComponent: () =>
      import('./parts/action-dialog/action-dialog.component'),
    title: 'ChromeApp',
  },
  {
    path: 'action-context',
    loadComponent: () =>
      import('./parts/action-context/action-context.component'),
    title: 'ChromeApp',
  },
  {
    path: 'tab-palette',
    loadComponent: () => import('./parts/tab-palette/tab-palette.component'),
    title: 'ChromeApp',
  },
  {
    path: 'context-menu',
    loadComponent: () => import('./parts/context-menu/context-menu.component'),
    title: 'ChromeApp',
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./content-pages/settings/settings-page.component'),
    title: 'Settings',
    data: {
      favicon: 'FA:Settings',
    },
  },
];
