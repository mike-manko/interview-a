import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { environment } from '../environments/environment';
import { problemDetailsInterceptor } from './api/problem-details-interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // A mock backend goes last, so problemDetailsInterceptor handles its errors like the real API's.
    provideHttpClient(
      withInterceptors([problemDetailsInterceptor, ...environment.backendInterceptors]),
    ),
  ],
};
