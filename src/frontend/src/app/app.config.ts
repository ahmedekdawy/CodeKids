import { ApplicationConfig, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
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
    provideRouter(routes, withInMemoryScrolling({ anchorScrolling: 'enabled' }))
  ]
};
