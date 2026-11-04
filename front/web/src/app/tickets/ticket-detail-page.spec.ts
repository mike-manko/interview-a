import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterOutlet, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { problemDetailsInterceptor } from '../api/problem-details-interceptor';
import { TenantContext } from '../tenancy/tenant-context';
import { expectAccessible } from '../../testing/expect-accessible';
import { Ticket } from './ticket';
import { TicketDetailPage } from './ticket-detail-page';
import { classifiedTicket, hiddenTicket, pendingTicket, retryingTicket } from './testing-tickets';

// Stands in for the list page, which renders the detail panel in its own outlet.
@Component({ imports: [RouterOutlet], template: '<router-outlet />' })
class TicketList {}

describe('TicketDetailPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  async function open(url: string): Promise<HTMLElement> {
    harness = await RouterTestingHarness.create(url);
    return harness.fixture.nativeElement as HTMLElement;
  }

  function expectTicketRequest(id: string): TestRequest {
    return http.expectOne(
      (request) => request.method === 'GET' && request.url === `/api/v1/tickets/${id}`,
    );
  }

  // Fake timers run the whole spec, so polling is under the test's control; whenStable would wait on the faked
  // change detection timer, so rendering advances the clock by zero instead, which also settles the microtasks
  // httpResource answers in.
  async function render(): Promise<void> {
    await vi.advanceTimersByTimeAsync(0);
    harness.detectChanges();
  }

  // axe-core schedules its checks on timers, which the faked clock would never run.
  async function expectAccessibleWithRealTimers(page: HTMLElement): Promise<void> {
    vi.useRealTimers();
    await expectAccessible(page);
    vi.useFakeTimers();
  }

  async function wait(ms: number): Promise<void> {
    await vi.advanceTimersByTimeAsync(ms);
    await render();
  }

  async function respond(ticket: Ticket): Promise<void> {
    expectTicketRequest(ticket.id).flush(ticket);
    await render();
  }

  async function openTicket(ticket: Ticket, query = ''): Promise<HTMLElement> {
    const page = await open(`/tickets/${ticket.id}${query}`);
    await respond(ticket);
    return page;
  }

  function text(page: HTMLElement): string {
    return page.textContent?.replace(/\s+/g, ' ') ?? '';
  }

  function link(page: HTMLElement, label: string): HTMLAnchorElement {
    const found = Array.from(page.querySelectorAll('a')).find(
      (anchor) => anchor.textContent?.trim() === label,
    );
    if (!found) {
      throw new Error(`Link "${label}" missing`);
    }
    return found;
  }

  beforeEach(() => {
    vi.useFakeTimers();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            {
              path: 'tickets',
              component: TicketList,
              children: [{ path: ':id', component: TicketDetailPage }],
            },
          ],
          withComponentInputBinding(),
        ),
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('shows a skeleton while the ticket loads', async () => {
    const page = await open('/tickets/t-004');

    expect(page.querySelector('[role="status"]')?.textContent).toContain('Loading ticket');
    expect(page.querySelector('[aria-busy="true"]')).not.toBeNull();
    await expectAccessibleWithRealTimers(page);
    expectTicketRequest('t-004').flush(classifiedTicket());
  });

  it('asks for the ticket with hidden ones included, for the selected tenant', async () => {
    await open('/tickets/t-004');

    const request = expectTicketRequest('t-004');
    expect(request.request.params.get('includeHidden')).toBe('true');
    expect(request.request.headers.get('X-Tenant-Id')).toBe('contoso');
    request.flush(classifiedTicket());
  });

  it('shows a triaged ticket with its classification, suggested reply and message', async () => {
    const page = await openTicket(classifiedTicket());

    expect(page.querySelector('h2')?.textContent?.trim()).toBe('Change email address');
    expect(text(page)).toContain('t-004');
    expect(link(page, 'lisa.m@example.com').getAttribute('href')).toBe('mailto:lisa.m@example.com');
    expect(page.querySelector('time')?.getAttribute('datetime')).toBe('2026-10-28T09:30:00Z');
    expect(text(page)).toContain('Triaged');
    expect(text(page)).toContain('Low');
    expect(text(page)).toContain('Account');
    expect(text(page)).toContain('Classified');
    expect(text(page)).toContain('You can change your email under Settings > Profile.');
    expect(text(page)).toContain('How can I change the email address on my account?');
    await expectAccessibleWithRealTimers(page);
  });

  it('shows customer HTML as text instead of rendering it', async () => {
    const page = await openTicket(
      classifiedTicket({ body: '<img src=x onerror="alert(1)"> Card **** **** **** 4242' }),
    );

    const message = page.querySelector('[aria-labelledby="message-heading"] p');
    expect(message?.textContent?.trim()).toBe(
      '<img src=x onerror="alert(1)"> Card **** **** **** 4242',
    );
    expect(page.querySelector('article img')).toBeNull();
  });

  it('says there is no message when the body is empty', async () => {
    const page = await openTicket(classifiedTicket({ body: '  ' }));

    expect(text(page)).toContain('(no message)');
  });

  it('polls every 3 seconds while the ticket is in triage and stops once it is triaged', async () => {
    const page = await openTicket(pendingTicket());

    expect(page.querySelector('[aria-live="polite"]')?.textContent).toContain(
      'The AI is classifying this ticket. This panel updates by itself.',
    );
    expect(text(page)).toContain('The AI drafts a reply when it classifies this ticket.');

    await wait(2999);
    http.expectNone((request) => request.url === '/api/v1/tickets/t-001');

    await wait(1);
    await respond(pendingTicket({ status: 'Processing' }));
    expect(text(page)).toContain('Classifying');

    await wait(3000);
    await respond(classifiedTicket({ id: 't-001', suggestedReply: 'We refunded one charge.' }));
    expect(text(page)).toContain('We refunded one charge.');
    expect(text(page)).not.toContain('The AI is classifying this ticket.');

    await wait(60_000);
    http.expectNone((request) => request.url === '/api/v1/tickets/t-001');
  });

  it('polls every 15 seconds while triage is retried and shows why it failed', async () => {
    const page = await openTicket(retryingTicket());

    expect(text(page)).toContain('Triage failed, retrying');
    expect(text(page)).toContain(
      'The AI service could not be reached. Triage will be retried automatically.',
    );
    expect(text(page)).toContain('1 failed attempt');
    expect(page.querySelector('.bg-warning-soft')).not.toBeNull();
    expect(text(page)).toContain('Triage failed and is being retried.');

    await wait(14_999);
    http.expectNone((request) => request.url === '/api/v1/tickets/t-002');

    await wait(1);
    await respond(retryingTicket({ failedTriageAttempts: 2 }));
    expect(text(page)).toContain('2 failed attempts');
    await wait(15_000);
    expectTicketRequest('t-002').flush(retryingTicket({ failedTriageAttempts: 2 }));
  });

  it('explains a ticket triage gave up on, and why it is hidden, without polling', async () => {
    const page = await openTicket(hiddenTicket());

    expect(text(page)).toContain('Triage gave up');
    expect(text(page)).toContain('The ticket tries to instruct the AI, so it was not classified.');
    expect(page.querySelector('.bg-danger-soft')).not.toBeNull();
    expect(text(page)).toContain(
      'Hidden from the queue because triage gave up. It shows only with "Show tickets triage gave up on".',
    );
    expect(text(page)).toContain('Triage gave up on this ticket, so there is no AI draft.');

    await wait(60_000);
    http.expectNone((request) => request.url === '/api/v1/tickets/t-005');
  });

  it('stops polling when the panel closes', async () => {
    const page = await openTicket(pendingTicket(), '?priority=High');

    link(page, 'Close').click();
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/tickets?priority=High'));
    await wait(60_000);

    http.expectNone((request) => request.url === '/api/v1/tickets/t-001');
  });

  it('starts over when another ticket is opened', async () => {
    await openTicket(pendingTicket());

    await harness.navigateByUrl('/tickets/t-004');
    await respond(classifiedTicket());
    await wait(60_000);

    http.expectNone((request) => request.url === '/api/v1/tickets/t-001');
  });

  it('closes back to the list and keeps its filters', async () => {
    const page = await openTicket(classifiedTicket(), '?category=Billing&priority=High');

    const close = link(page, 'Close');
    expect(close.getAttribute('href')).toBe('/tickets?category=Billing&priority=High');
  });

  it('says the ticket is not in the selected tenant when the API answers 404', async () => {
    TestBed.inject(TenantContext).select('fabrikam');
    const page = await open('/tickets/t-004');

    expectTicketRequest('t-004').flush(
      { title: 'Not Found', status: 404 },
      { status: 404, statusText: 'Not Found' },
    );
    await render();

    expect(text(page)).toContain('This ticket is not in tenant fabrikam.');
    expect(link(page, 'Back to tickets').getAttribute('href')).toBe('/tickets');
    await expectAccessibleWithRealTimers(page);
  });

  it('describes other errors and loads again on Try again', async () => {
    const page = await open('/tickets/t-004');

    expectTicketRequest('t-004').error(new ProgressEvent('error'));
    await render();
    expect(page.querySelector('[role="alert"]')?.textContent).toContain('Cannot reach the server');
    await expectAccessibleWithRealTimers(page);

    page.querySelector<HTMLButtonElement>('button')?.click();
    await render();
    await respond(classifiedTicket());

    expect(page.querySelector('h2')?.textContent?.trim()).toBe('Change email address');
  });
});
