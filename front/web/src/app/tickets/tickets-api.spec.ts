import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { TenantContext } from '../tenancy/tenant-context';
import { anyTicket, TicketQuery } from './ticket-query';
import { classifiedTicket, pendingTicket, ticketPage } from './testing-tickets';
import { TicketsApi } from './tickets-api';

describe('TicketsApi', () => {
  let api: TicketsApi;
  let http: HttpTestingController;
  let tenant: TenantContext;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(TicketsApi);
    http = TestBed.inject(HttpTestingController);
    tenant = TestBed.inject(TenantContext);
  });

  afterEach(() => http.verify());

  it('loads the first page for the selected tenant and query', async () => {
    const query = signal<TicketQuery>({ ...anyTicket, categories: ['Billing'] });
    const page = TestBed.runInInjectionContext(() => api.firstPage(query));
    TestBed.tick();

    const request = http.expectOne(
      (candidate) => candidate.url === '/api/v1/tickets' && candidate.method === 'GET',
    );
    expect(request.request.headers.get('X-Tenant-Id')).toBe('contoso');
    expect(request.request.params.getAll('category')).toEqual(['Billing']);
    request.flush(ticketPage([classifiedTicket()], 'next'));
    await TestBed.inject(ApplicationRef).whenStable();

    expect(page.value()).toEqual(ticketPage([classifiedTicket()], 'next'));
  });

  it('re-requests the first page with the new header when the tenant changes', () => {
    TestBed.runInInjectionContext(() => api.firstPage(() => anyTicket));
    TestBed.tick();
    http.expectOne((candidate) => candidate.url === '/api/v1/tickets').flush(ticketPage([]));

    tenant.select('fabrikam');
    TestBed.tick();

    const request = http.expectOne((candidate) => candidate.url === '/api/v1/tickets');
    expect(request.request.headers.get('X-Tenant-Id')).toBe('fabrikam');
    request.flush(ticketPage([]));
  });

  it('loads the next page with the continuation token and the same filters', async () => {
    const next = firstValueFrom(api.nextPage({ ...anyTicket, priorities: ['High'] }, 'token-2'));
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/tickets');
    expect(request.request.params.get('continuationToken')).toBe('token-2');
    expect(request.request.params.getAll('priority')).toEqual(['High']);
    request.flush(ticketPage([pendingTicket()]));

    expect(await next).toEqual(ticketPage([pendingTicket()]));
  });

  it('loads one ticket by id, hidden ones included', async () => {
    const ticket = TestBed.runInInjectionContext(() => api.ticket(() => 't 1'));
    TestBed.tick();
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/tickets/t%201');
    expect(request.request.params.get('includeHidden')).toBe('true');
    expect(request.request.headers.get('X-Tenant-Id')).toBe('contoso');
    request.flush(classifiedTicket());
    await TestBed.inject(ApplicationRef).whenStable();

    expect(ticket.value()).toEqual(classifiedTicket());
  });

  it('stays idle while there is no ticket id', () => {
    TestBed.runInInjectionContext(() => api.ticket(() => undefined));
    TestBed.tick();

    http.expectNone((candidate) => candidate.url.startsWith('/api/v1/tickets'));
  });

  it('creates a ticket for the selected tenant', async () => {
    const created = firstValueFrom(
      api.createTicket({ customerEmail: 'a@example.com', subject: 'S', body: 'B' }),
    );
    const request = http.expectOne({ method: 'POST', url: '/api/v1/tickets' });
    expect(request.request.headers.get('X-Tenant-Id')).toBe('contoso');
    expect(request.request.body).toEqual({
      customerEmail: 'a@example.com',
      subject: 'S',
      body: 'B',
    });
    request.flush({ id: 'new-1', status: 'Pending' }, { status: 202, statusText: 'Accepted' });

    expect(await created).toEqual({ id: 'new-1', status: 'Pending' });
  });
});
