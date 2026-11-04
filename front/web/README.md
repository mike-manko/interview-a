# Support triage (web)

Angular 22 front end for the support-ticket triage API: the queue with category and priority filters, a ticket detail view with the AI-suggested reply, and a create form. An LLM in the .NET API's background worker classifies each ticket; the front end only reads the result.

```bash
npm install
npm start              # http://localhost:4200, real API through the dev proxy
npm run start:mock     # http://localhost:4200, in-memory mock API, no backend needed
npm test -- --watch=false
npm run lint
npx prettier --check "src/**/*.{ts,html,css}"
```

## Real API or mock

- `npm start` serves the app and forwards `/api/*` to the .NET API at `https://localhost:51053` (`proxy.conf.json`). The path is kept as is, so `/api/v1/tickets` reaches `GET /api/v1/tickets`. The API uses the self-signed .NET development certificate, so the proxy sets `secure: false`.
- `npm run start:mock` uses `src/environments/environment.mock.ts`, which registers `mockBackendInterceptor`. It answers `/api/v1/tickets` from memory, seeded from `seed-tickets.json`, and classifies new tickets with keyword rules. The header shows "Mock API · data resets on reload". Production builds never include the mock.

## DTOs come from OpenAPI

DTOs are generated, never hand-written. They live in `src/app/api/generated/`, and `src/app/tickets/ticket.ts` re-exports them as readonly types.

| Command                | Does                                                                                                            |
| ---------------------- | --------------------------------------------------------------------------------------------------------------- |
| `npm run api:pull`     | Downloads `https://localhost:51053/openapi/v1.json` (API must be running) into `openapi/support-triage.v1.json` |
| `npm run api:generate` | Runs `@hey-api/openapi-ts` on that file, with the schema patches in `openapi-ts.config.ts`                      |
| `npm run api:refresh`  | Both, in that order                                                                                             |

The generator uses only the `@hey-api/typescript` plugin, so it produces types, not an HTTP client. Requests go through `TicketsApi` (`httpResource` for reads, `HttpClient` for writes).

Why the patches exist: the API sends enums as strings, but its OpenAPI document describes them as integers. `JsonStringEnumConverter` is registered only on the MVC JSON options, while the OpenAPI generator reads the minimal-API JSON options. The patches restore the enum names (`TicketCategory`, `TicketPriority`, `TicketStatus`, `TriageFailureReason`) and fix `failedTriageAttempts`, which .NET documents as "integer or string". Backend fix: also register the converter with `builder.Services.ConfigureHttpJsonOptions(...)`; then the enum patches can go.

## Tenant switcher

Authentication is a stand-in: the API isolates data by the `X-Tenant-Id` header, and `TicketsApi` sends whichever tenant is selected in the header's tenant switcher (`TenantContext`, remembered in `localStorage`). The API has no endpoint that lists tenants, so the switcher offers the seed tenants `contoso` and `fabrikam` (`src/app/tenancy/tenant-id.ts`) and accepts any other id of 1-64 lowercase letters, digits or dashes. An unknown tenant has no tickets, which is how a demo shows isolation. Switching returns to the queue without filters, because the open ticket and the filters belong to the old tenant.
