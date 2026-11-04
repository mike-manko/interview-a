import { EnvironmentConfig } from './environment-config';

// ng serve talks to the real API through proxy.conf.json. `npm run start:mock` swaps in environment.mock.ts instead.
export const environment = {
  mockBackend: false,
  backendInterceptors: [],
} satisfies EnvironmentConfig;
