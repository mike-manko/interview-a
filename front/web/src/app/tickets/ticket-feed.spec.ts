import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { problemDetailsInterceptor } from '../api/problem-details-interceptor';
import { TenantContext } from '../tenancy/tenant-context';
import { anyTicket, TicketQuery } from './ticket-query';
import { autoRefreshMilliseconds, TicketFeed, ticketFeed } from './ticket-feed';
import { classifiedTicket, pendingTicket, ticketPage } from './testing-tickets';

describe('ticketFeed', () => {
  let http: HttpTestingController;
  let query: ReturnType<typeof signal<TicketQuery>>;
  let feed: TicketFeed;

  function expectTicketsRequest(): TestRequest {
    TestBed.tick();
    return http.expectOne((request) => request.url === '/api/v1/tickets');
  }

  // httpResource publishes a response asynchronously; waiting for the app to be stable makes it visible.
  async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
  }

  beforeEach(() => {
    // Only the refresh timer is faked; Angular schedules its own work with setTimeout and animation frames.
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    query = signal<TicketQuery>(anyTicket);
    feed = TestBed.runInInjectionContext(() => ticketFeed(query));
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('is loading until the first page arrives, then lists its tickets', async () => {
    expect(feed.status()).toBe('loading');

    expectTicketsRequest().flush(ticketPage([pendingTicket(), classifiedTicket()]));
    await settle();

    expect(feed.status()).toBe('ready');
    expect(feed.tickets().map((ticket) => ticket.id)).toEqual(['t-001', 't-004']);
    expect(feed.updatedAt()).toBeInstanceOf(Date);
  });

  it('reports a failed first page as an error', async () => {
    expectTicketsRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await settle();

    expect(feed.status()).toBe('error');
    expect(feed.error()).toBeDefined();
  });

  it('appends the next page with the continuation token and the same filters', async () => {
    query.set({ ...anyTicket, priorities: ['High'] });
    expectTicketsRequest().flush(ticketPage([classifiedTicket({ id: 'a' })], 'token-2'));
    await settle();
    expect(feed.canLoadMore()).toBe(true);

    feed.loadMore();
    expect(feed.loadingMore()).toBe(true);
    const next = http.expectOne((request) => request.url === '/api/v1/tickets');
    expect(next.request.params.get('continuationToken')).toBe('token-2');
    expect(next.request.params.getAll('priority')).toEqual(['High']);
    next.flush(ticketPage([classifiedTicket({ id: 'b' })]));
    await settle();

    expect(feed.tickets().map((ticket) => ticket.id)).toEqual(['a', 'b']);
    expect(feed.loadingMore()).toBe(false);
    expect(feed.canLoadMore()).toBe(false);
  });

  it('shows a ticket repeated across pages once', async () => {
    expectTicketsRequest().flush(
      ticketPage([classifiedTicket({ id: 'a' }), classifiedTicket({ id: 'b' })], 'token-2'),
    );
    await settle();

    feed.loadMore();
    http
      .expectOne((request) => request.url === '/api/v1/tickets')
      .flush(ticketPage([classifiedTicket({ id: 'b' }), classifiedTicket({ id: 'c' })]));
    await settle();

    expect(feed.tickets().map((ticket) => ticket.id)).toEqual(['a', 'b', 'c']);
  });

  it('keeps the loaded tickets and says why when the next page fails', async () => {
    expectTicketsRequest().flush(ticketPage([classifiedTicket()], 'token-2'));
    await settle();

    feed.loadMore();
    http
      .expectOne((request) => request.url === '/api/v1/tickets')
      .error(new ProgressEvent('error'));
    await settle();

    expect(feed.tickets()).toHaveLength(1);
    expect(feed.loadingMore()).toBe(false);
    expect(feed.loadMoreError()).toBe(
      'Cannot reach the server. Check your connection and try again.',
    );
    expect(feed.canLoadMore()).toBe(true);
  });

  it('starts over when the query changes, dropping the loaded pages', async () => {
    expectTicketsRequest().flush(ticketPage([classifiedTicket({ id: 'a' })], 'token-2'));
    await settle();
    feed.loadMore();
    http
      .expectOne((request) => request.url === '/api/v1/tickets')
      .flush(ticketPage([classifiedTicket({ id: 'b' })], 'token-3'));
    await settle();

    query.set({ ...anyTicket, categories: ['Billing'] });
    expect(feed.status()).toBe('loading');
    const request = expectTicketsRequest();
    expect(request.request.params.getAll('category')).toEqual(['Billing']);
    request.flush(ticketPage([classifiedTicket({ id: 'c' })]));
    await settle();

    expect(feed.tickets().map((ticket) => ticket.id)).toEqual(['c']);
    expect(feed.canLoadMore()).toBe(false);
  });

  it('drops a next page that arrives after the tenant changed', async () => {
    expectTicketsRequest().flush(ticketPage([classifiedTicket({ id: 'a' })], 'token-2'));
    await settle();
    feed.loadMore();
    const staleNext = http.expectOne((request) => request.url === '/api/v1/tickets');

    TestBed.inject(TenantContext).select('fabrikam');
    const fabrikam = expectTicketsRequest();
    expect(fabrikam.request.headers.get('X-Tenant-Id')).toBe('fabrikam');
    fabrikam.flush(ticketPage([classifiedTicket({ id: 'f' })]));
    await settle();
    staleNext.flush(ticketPage([classifiedTicket({ id: 'stale' })]));
    await settle();

    expect(feed.tickets().map((ticket) => ticket.id)).toEqual(['f']);
    expect(feed.loadingMore()).toBe(false);
  });

  it('reloads the first page on refresh and keeps the tickets meanwhile', async () => {
    expectTicketsRequest().flush(ticketPage([classifiedTicket()]));
    await settle();

    feed.refresh();
    const reload = expectTicketsRequest();
    expect(feed.refreshing()).toBe(true);
    expect(feed.status()).toBe('ready');
    expect(feed.tickets()).toHaveLength(1);
    reload.flush(ticketPage([pendingTicket(), classifiedTicket()]));
    await settle();

    expect(feed.refreshing()).toBe(false);
    expect(feed.tickets()).toHaveLength(2);
  });

  it('refreshes every 15 seconds while a loaded ticket is in triage', async () => {
    expectTicketsRequest().flush(ticketPage([pendingTicket(), classifiedTicket()]));
    await settle();
    expect(feed.autoRefreshing()).toBe(true);

    vi.advanceTimersByTime(autoRefreshMilliseconds);
    expectTicketsRequest().flush(
      ticketPage([classifiedTicket({ id: 't-001' }), classifiedTicket()]),
    );
    await settle();
    expect(feed.autoRefreshing()).toBe(false);

    vi.advanceTimersByTime(autoRefreshMilliseconds);
    TestBed.tick();
    http.expectNone((request) => request.url === '/api/v1/tickets');
  });

  it('does not refresh on its own once more pages are loaded', async () => {
    expectTicketsRequest().flush(ticketPage([pendingTicket()], 'token-2'));
    await settle();
    feed.loadMore();
    http
      .expectOne((request) => request.url === '/api/v1/tickets')
      .flush(ticketPage([classifiedTicket()]));
    await settle();

    expect(feed.autoRefreshing()).toBe(false);
    vi.advanceTimersByTime(autoRefreshMilliseconds);
    TestBed.tick();
    http.expectNone((request) => request.url === '/api/v1/tickets');
  });
});
