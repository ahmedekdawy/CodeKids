import { ApplicationConfig, isDevMode, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideServiceWorker } from '@angular/service-worker';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { apiBusyInterceptor } from './api-busy.interceptor';
import { loadDefaultTenant } from './tenant';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAppInitializer(loadDefaultTenant),
    provideHttpClient(withInterceptors([authInterceptor, apiBusyInterceptor])),
    provideRouter(routes, withInMemoryScrolling({ anchorScrolling: 'enabled' })),
    // Production builds only; registering once the app is idle keeps it off the first page load.
    // app-sw.js is Angular's worker plus the app icon's unread badge for pushes that arrive while closed.
    provideServiceWorker('app-sw.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
