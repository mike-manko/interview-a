import { HttpInterceptorFn } from '@angular/common/http';

export interface EnvironmentConfig {
  // Shows the "Mock API" badge while the in-memory backend answers /api/tickets.
  readonly mockBackend: boolean;
  // Registered after the app's own interceptors. Living here, not in app.config.ts, keeps the
  // mock backend and its seed data out of production bundles through fileReplacements.
  readonly backendInterceptors: readonly HttpInterceptorFn[];
}
