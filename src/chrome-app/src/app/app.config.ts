import {
  ApplicationConfig,
  importProvidersFrom,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { provideAnimations } from '@angular/platform-browser/animations';

import { routes } from './app.routes';
import { AppTitleStrategy } from './shared/app-title-strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    { provide: TitleStrategy, useClass: AppTitleStrategy },
    importProvidersFrom(DragDropModule),
    provideAnimations(),
  ],
};
