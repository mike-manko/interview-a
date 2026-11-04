import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom } from 'rxjs';
import { problemDetailsInterceptor } from '../api/problem-details-interceptor';
import { expectAccessible } from '../../testing/expect-accessible';
import { Ticket } from './ticket';
import {
  classifiedTicket,
  hiddenTicket,
  pendingTicket,
  retryingTicket,
  ticketPage,
} from './testing-tickets';
import { TicketsPage } from './tickets-page';

// The real detail page loads its own ticket; the queue only needs an outlet target.
@Component({ template: `<p>Ticket detail</p>` })
class TicketDetailStub {}

const cardDeclined = classifiedTicket({
  id: 't-010',
  subject: 'Card declined',
  priority: 'Critical',
  category: 'Billing',
});
const parcelLost = classifiedTicket({
  id: 't-003',
  subject: 'Parcel lost',
  priority: 'High',
  category: 'Shipping',
});
// Newest first, as the API returns them.
const queue: readonly Ticket[] = [
  cardDeclined,
  pendingTicket(),
  retryingTicket(),
  classifiedTicket({ id: 't-004', priority: 'Low' }),
  parcelLost,
];

describe('TicketsPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;
  let page: HTMLElement;

  function expectTicketsRequest(): TestRequest {
    TestBed.tick();
    return http.expectOne((request) => request.url === '/api/v1/tickets');
  }

  async function openQueue(url = '/tickets', tickets: readonly Ticket[] = queue): Promise<void> {
    harness = await RouterTestingHarness.create(url);
    expectTicketsRequest().flush(ticketPage(tickets));
    await harness.fixture.whenStable();
    page = harness.routeNativeElement as HTMLElement;
  }

  // The page navigates to change its view; whenStable would wait for a ticket request it is about to make.
  async function navigatedBy(action: () => void): Promise<void> {
    const navigated = firstValueFrom(
      router.events.pipe(filter((event) => event instanceof NavigationEnd)),
    );
    action();
    await navigated;
    TestBed.tick();
  }

  function subjects(): string[] {
    return [...page.querySelectorAll('tbody a')].map((link) => link.textContent?.trim() ?? '');
  }

  function groupNames(): string[] {
    return [...page.querySelectorAll('th[scope="rowgroup"] .font-semibold')].map(
      (name) => name.textContent?.trim() ?? '',
    );
  }

  function button(name: string): HTMLButtonElement {
    const match = [...page.querySelectorAll('button')].find(
      (candidate) => candidate.textContent?.trim() === name,
    );
    if (match === undefined) {
      throw new Error(`button ${name} missing`);
    }
    return match;
  }

  function text(): string {
    return page.textContent?.replace(/\s+/g, ' ') ?? '';
  }

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            {
              path: 'tickets',
              component: TicketsPage,
              children: [{ path: ':id', component: TicketDetailStub }],
            },
          ],
          withComponentInputBinding(),
        ),
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => http.verify());

  it('shows skeleton rows while the first page loads', async () => {
    harness = await RouterTestingHarness.create('/tickets');
    page = harness.routeNativeElement as HTMLElement;
    const request = expectTicketsRequest();
    harness.fixture.detectChanges();

    expect(page.querySelector('[role="status"]')?.textContent).toContain('Loading tickets');
    expect(page.querySelector('table')).toBeNull();
    await expectAccessible(page);

    request.flush(ticketPage(queue));
    await harness.fixture.whenStable();
    expect(page.querySelector('[role="status"]')).toBeNull();
  });

  it('groups the queue by priority, most urgent first, newest first within a group', async () => {
    await openQueue();

    expect(page.querySelector('h1')?.textContent?.trim()).toBe('Ticket queue');
    expect(groupNames()).toEqual(['Critical', 'High', 'Low', 'Not classified yet']);
    expect(subjects()).toEqual([
      'Card declined',
      'Parcel lost',
      'Change email address',
      'Charged twice for October',
      'App crashes on login',
    ]);
    expect(text()).toContain('Showing 5 tickets');
    expect(text()).toMatch(/Updated \d{2}:\d{2}:\d{2}/);
    expect(text()).toContain('Select a ticket to read it and its suggested reply.');
    await expectAccessible(page);
  });

  it('sends the category, priority and hidden filters from the URL and ignores invalid values', async () => {
    harness = await RouterTestingHarness.create(
      '/tickets?category=Billing&category=Nope&category=Account&priority=High&priority=Urgent&hidden=1',
    );
    const request = expectTicketsRequest();

    expect(request.request.params.getAll('category')).toEqual(['Billing', 'Account']);
    expect(request.request.params.getAll('priority')).toEqual(['High']);
    expect(request.request.params.get('includeHidden')).toBe('true');
    request.flush(ticketPage([hiddenTicket()]));
    await harness.fixture.whenStable();
    page = harness.routeNativeElement as HTMLElement;

    const billing = button('Billing');
    expect(billing.getAttribute('aria-pressed')).toBe('true');
    expect(page.querySelector<HTMLInputElement>('input[type="checkbox"]')?.checked).toBe(true);
    expect(text()).toContain(
      'Tickets without a category or priority yet are hidden while these filters are on.',
    );
  });

  it('pressing a category chip puts it in the URL and asks the API for it', async () => {
    await openQueue();

    await navigatedBy(() => button('Billing').click());

    expect(router.url).toBe('/tickets?category=Billing');
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/tickets');
    expect(request.request.params.getAll('category')).toEqual(['Billing']);
    request.flush(ticketPage([cardDeclined]));
    await harness.fixture.whenStable();

    expect(subjects()).toEqual(['Card declined']);
    expect(button('Billing').getAttribute('aria-pressed')).toBe('true');
    await expectAccessible(page);
  });

  it('searches the loaded tickets without asking the API again', async () => {
    await openQueue();

    const search = page.querySelector<HTMLInputElement>('#ticket-search');
    await navigatedBy(() => {
      if (search === null) {
        throw new Error('search missing');
      }
      search.value = 'MARK.T';
      search.dispatchEvent(new Event('input'));
    });
    await harness.fixture.whenStable();

    expect(router.url).toBe('/tickets?q=MARK.T');
    http.expectNone((request) => request.url === '/api/v1/tickets');
    expect(subjects()).toEqual(['App crashes on login']);
    expect(text()).toContain('Showing 1 of 5 tickets loaded');
    expect(page.querySelector('[aria-live="polite"]')?.textContent).toContain(
      'Showing 1 of 5 tickets loaded',
    );
  });

  it('counts the triage states and shows only the picked one', async () => {
    await openQueue();

    const labels = [...page.querySelectorAll('fieldset label')].map((label) =>
      label.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(labels).toEqual(['All 5', 'Needs attention 1', 'In triage 1', 'Triaged 3']);

    await navigatedBy(() =>
      page
        .querySelector<HTMLInputElement>('input[name="triage-facet"][value="in-triage"]')
        ?.click(),
    );
    await harness.fixture.whenStable();

    expect(router.url).toBe('/tickets?state=in-triage');
    expect(subjects()).toEqual(['Charged twice for October']);
  });

  it('groups by the option picked in Group by', async () => {
    await openQueue();

    const select = page.querySelector<HTMLSelectElement>('#ticket-grouping');
    await navigatedBy(() => {
      if (select === null) {
        throw new Error('select missing');
      }
      select.value = 'triage';
      select.dispatchEvent(new Event('change'));
    });
    await harness.fixture.whenStable();

    expect(router.url).toBe('/tickets?group=triage');
    expect(groupNames()).toEqual(['Needs attention', 'In triage', 'Triaged']);
  });

  it('explains that triage keeps failing and offers the tickets that need attention', async () => {
    await openQueue('/tickets', [
      retryingTicket({
        failureReason: 'ProviderConfiguration',
        failureMessage: 'The AI provider rejected the API key.',
      }),
      retryingTicket({ id: 't-006' }),
      classifiedTicket(),
    ]);

    expect(text()).toContain('Triage is failing for 2 tickets.');
    expect(text()).toContain('Latest reason: The AI provider rejected the API key.');
    expect(text()).toContain('Refreshes every 15 seconds while tickets are in triage.');

    await navigatedBy(() => button('Show tickets that need attention').click());
    await harness.fixture.whenStable();
    expect(router.url).toBe('/tickets?state=attention');
    expect(subjects()).toEqual(['App crashes on login', 'App crashes on login']);
  });

  it('says what failed and tries again', async () => {
    harness = await RouterTestingHarness.create('/tickets');
    expectTicketsRequest().error(new ProgressEvent('error'));
    await harness.fixture.whenStable();
    page = harness.routeNativeElement as HTMLElement;

    const alert = page.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('The ticket queue could not be loaded.');
    expect(alert?.textContent).toContain('Cannot reach the server.');
    await expectAccessible(page);

    button('Try again').click();
    expectTicketsRequest().flush(ticketPage(queue));
    await harness.fixture.whenStable();
    expect(subjects()).toHaveLength(5);
  });

  it('teaches what to do when the tenant has no tickets', async () => {
    await openQueue('/tickets', []);

    expect(text()).toContain('No tickets in contoso yet.');
    const create = [...page.querySelectorAll('a')].find(
      (link) => link.textContent?.trim() === 'Create a ticket',
    );
    expect(create?.getAttribute('href')).toBe('/tickets/new');
    await expectAccessible(page);
  });

  it('says when no ticket matches and clears the filters', async () => {
    await openQueue('/tickets?q=nothing-like-this&group=category');

    expect(text()).toContain('No tickets match these filters.');
    expect(page.querySelector('table')).toBeNull();

    const clear = [...page.querySelectorAll('button')].filter(
      (candidate) => candidate.textContent?.trim() === 'Clear filters',
    );
    await navigatedBy(() => clear.at(-1)?.click());
    await harness.fixture.whenStable();

    expect(router.url).toBe('/tickets?group=category');
    expect(subjects()).toHaveLength(5);
  });

  it('appends the next page with Load more', async () => {
    harness = await RouterTestingHarness.create('/tickets');
    expectTicketsRequest().flush(ticketPage([classifiedTicket()], 'token-2'));
    await harness.fixture.whenStable();
    page = harness.routeNativeElement as HTMLElement;

    button('Load more').click();
    harness.fixture.detectChanges();
    expect(button('Loading…').disabled).toBe(true);
    const next = http.expectOne((request) => request.url === '/api/v1/tickets');
    expect(next.request.params.get('continuationToken')).toBe('token-2');
    next.flush(ticketPage([classifiedTicket({ id: 't-003', subject: 'Parcel lost' })]));
    await harness.fixture.whenStable();

    expect(subjects()).toEqual(['Change email address', 'Parcel lost']);
    expect(text()).toContain('Showing 2 tickets');
    expect(page.textContent).not.toContain('Load more');
  });

  it('keeps the filters when a ticket is opened and marks its row as current', async () => {
    await openQueue('/tickets?group=none&state=triaged');

    const link = [...page.querySelectorAll<HTMLAnchorElement>('tbody a')].find(
      (candidate) => candidate.textContent?.trim() === 'Parcel lost',
    );
    expect(link?.getAttribute('href')).toBe('/tickets/t-003?group=none&state=triaged');
    await navigatedBy(() => link?.click());
    await harness.fixture.whenStable();

    expect(router.url).toBe('/tickets/t-003?group=none&state=triaged');
    expect(link?.getAttribute('aria-current')).toBe('page');
    expect(link?.closest('tr')?.className).toContain('bg-primary-soft');
    expect(text()).toContain('Ticket detail');
    expect(text()).not.toContain('Select a ticket to read it');
    await expectAccessible(page);
  });

  it('keeps the open ticket when a filter changes', async () => {
    await openQueue('/tickets/t-003');

    await navigatedBy(() => button('Shipping').click());

    expect(router.url).toBe('/tickets/t-003?category=Shipping');
    expectTicketsRequest().flush(ticketPage([parcelLost]));
    await harness.fixture.whenStable();
    expect(subjects()).toEqual(['Parcel lost']);
  });
});
