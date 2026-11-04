import { HttpEvent, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { isTenantId } from '../tenancy/tenant-id';
import { tenantHeader, ticketsUrl } from '../tickets/tickets-api';
import {
  badRequestProblem,
  methodNotAllowedProblem,
  routeNotFoundProblem,
  tenantProblem,
  ticketNotFoundProblem,
  validationProblem,
} from './mock-problems';
import { createLatencyMs, fail, readLatencyMs, respond } from './mock-response';
import { MockTicketsDb } from './mock-tickets-db';
import { bindBoolean } from './query-binding';
import { parseTicketListQuery } from './ticket-list-query';
import { selectPage } from './ticket-page';
import { validateNewTicket } from './validate-new-ticket';

// Any origin works: only the path and query string of the request are read.
const anyOrigin = 'http://mock.invalid';

// Answers /api/v1/tickets the way openapi/support-triage.v1.json and the .NET API do, from memory. Registered last,
// so problemDetailsInterceptor sees its errors exactly as it would see the real API's.
export const mockBackendInterceptor: HttpInterceptorFn = (request, next) => {
  const url = new URL(request.urlWithParams, anyOrigin);
  const path = url.pathname;
  if (path !== ticketsUrl && !path.startsWith(`${ticketsUrl}/`)) {
    return next(request);
  }

  // Like TenantMiddleware, the tenant is checked before routing.
  const tenantId = tenantOf(request);
  if (tenantId === undefined) {
    return fail(400, tenantProblem(path), request.url, readLatencyMs);
  }

  const db = inject(MockTicketsDb);
  if (path === ticketsUrl) {
    if (request.method === 'GET') {
      return listTickets(db, tenantId, url, request.url);
    }
    if (request.method === 'POST') {
      return createTicket(db, tenantId, request);
    }
    return fail(405, methodNotAllowedProblem(path), request.url, readLatencyMs);
  }

  const ticketId = ticketIdOf(path);
  if (ticketId === undefined) {
    return fail(404, routeNotFoundProblem(path), request.url, readLatencyMs);
  }
  if (request.method === 'GET') {
    return getTicket(db, tenantId, ticketId, url, request.url);
  }
  return fail(405, methodNotAllowedProblem(path), request.url, readLatencyMs);
};

function listTickets(
  db: MockTicketsDb,
  tenantId: string,
  url: URL,
  requestUrl: string,
): Observable<HttpEvent<unknown>> {
  const parsed = parseTicketListQuery(url.searchParams);
  switch (parsed.kind) {
    case 'binding-errors':
      return fail(400, validationProblem(parsed.errors), requestUrl, readLatencyMs);
    case 'domain-errors':
      return fail(400, badRequestProblem(parsed.errors, url.pathname), requestUrl, readLatencyMs);
    case 'query':
      return respond(200, selectPage(db.list(tenantId), parsed.query), requestUrl, readLatencyMs);
    default:
      return parsed satisfies never;
  }
}

// A hidden ticket (triage gave up) is not found unless includeHidden is true, as in the API.
function getTicket(
  db: MockTicketsDb,
  tenantId: string,
  ticketId: string,
  url: URL,
  requestUrl: string,
): Observable<HttpEvent<unknown>> {
  const includeHidden = bindBoolean(url.searchParams.get('includeHidden'), false);
  if (!includeHidden.ok) {
    const errors = { includeHidden: [includeHidden.message] };
    return fail(400, validationProblem(errors), requestUrl, readLatencyMs);
  }
  const ticket = db.find(tenantId, ticketId);
  if (ticket === undefined || (ticket.isHidden && !includeHidden.value)) {
    return fail(404, ticketNotFoundProblem(ticketId, url.pathname), requestUrl, readLatencyMs);
  }
  return respond(200, ticket, requestUrl, readLatencyMs);
}

function createTicket(
  db: MockTicketsDb,
  tenantId: string,
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> {
  const validation = validateNewTicket(request.body);
  if (!validation.valid) {
    return fail(400, validationProblem(validation.errors), request.url, createLatencyMs);
  }
  return respond(202, db.create(tenantId, validation.ticket), request.url, createLatencyMs);
}

// Exactly one header with a valid tenant id, like TenantMiddleware.
function tenantOf(request: HttpRequest<unknown>): string | undefined {
  const values = request.headers.getAll(tenantHeader) ?? [];
  if (values.length !== 1 || !isTenantId(values[0])) {
    return undefined;
  }
  return values[0];
}

// The API's route takes one path segment of 1-64 characters after /api/v1/tickets/.
function ticketIdOf(path: string): string | undefined {
  const segment = path.slice(ticketsUrl.length + 1);
  if (segment === '' || segment.includes('/')) {
    return undefined;
  }
  try {
    const ticketId = decodeURIComponent(segment);
    return ticketId.length <= 64 ? ticketId : undefined;
  } catch {
    return undefined;
  }
}
