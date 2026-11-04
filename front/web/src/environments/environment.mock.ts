import { mockBackendInterceptor } from '../app/mock-backend/mock-backend-interceptor';
import { EnvironmentConfig } from './environment-config';

// `npm run start:mock`: answers /api/v1/tickets from memory, for working without the .NET API.
export const environment = {
  mockBackend: true,
  backendInterceptors: [mockBackendInterceptor],
} satisfies EnvironmentConfig;
