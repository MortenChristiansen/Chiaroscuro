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
      favicon:
        "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><defs><linearGradient id='g' x1='0' y1='0' x2='0' y2='1'><stop offset='0%' stop-color='%23f8fafc'/><stop offset='100%' stop-color='%23e2e8f0'/></linearGradient></defs><circle cx='32' cy='32' r='30' fill='url(%23g)' stroke='%2394a3b8' stroke-width='2'/><g transform='translate(32,32)'><g fill='%23565f7a'><circle r='8' fill='%238ea2b8'/><g stroke='%23565f7a' stroke-width='4' stroke-linecap='round'><line x1='0' y1='-18' x2='0' y2='-26'/><line x1='0' y1='18' x2='0' y2='26'/><line x1='18' y1='0' x2='26' y2='0'/><line x1='-18' y1='0' x2='-26' y2='0'/><line x1='12.7' y1='12.7' x2='18.4' y2='18.4'/><line x1='-12.7' y1='-12.7' x2='-18.4' y2='-18.4'/><line x1='12.7' y1='-12.7' x2='18.4' y2='-18.4'/><line x1='-12.7' y1='12.7' x2='-18.4' y2='18.4'/></g></g></g></svg>",
    },
  },
];
