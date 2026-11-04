import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { problemDetailsInterceptor } from '../api/problem-details-interceptor';
import { TenantContext } from '../tenancy/tenant-context';
import { expectAccessible } from '../../testing/expect-accessible';
import { TicketCreatePage } from './ticket-create-page';

// Stands in for the list and the detail panel the form navigates to.
@Component({ template: '' })
class TicketList {}

describe('TicketCreatePage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  async function openForm(): Promise<HTMLElement> {
    harness = await RouterTestingHarness.create('/tickets/new');
    await harness.fixture.whenStable();
    return harness.routeNativeElement as HTMLElement;
  }

  function type(page: HTMLElement, selector: string, value: string): void {
    const field = page.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
    if (!field) {
      throw new Error(`${selector} missing`);
    }
    field.value = value;
    field.dispatchEvent(new Event('input'));
    field.dispatchEvent(new Event('blur'));
  }

  function fillValidTicket(page: HTMLElement): void {
    type(page, '#customer-email', 'anna.k@example.com');
    type(page, '#subject', 'Charged twice');
    type(page, '#body', 'Please refund one charge.');
  }

  function submitButton(page: HTMLElement): HTMLButtonElement {
    const button = page.querySelector<HTMLButtonElement>('button[type="submit"]');
    if (!button) {
      throw new Error('Submit button missing');
    }
    return button;
  }

  async function submit(page: HTMLElement): Promise<void> {
    submitButton(page).click();
    await harness.fixture.whenStable();
  }

  function expectCreateRequest() {
    return http.expectOne({ method: 'POST', url: '/api/v1/tickets' });
  }

  function errorText(page: HTMLElement, id: string): string {
    return page.querySelector(`#${id}`)?.textContent?.trim() ?? '';
  }

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            { path: 'tickets/new', component: TicketCreatePage },
            { path: 'tickets', component: TicketList },
            { path: 'tickets/:id', component: TicketList },
          ],
          withComponentInputBinding(),
        ),
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('says which tenant the ticket is created in', async () => {
    TestBed.inject(TenantContext).select('fabrikam');
    const page = await openForm();

    expect(page.textContent?.replace(/\s+/g, ' ')).toContain(
      'The ticket is created in tenant fabrikam.',
    );
    expect(page.querySelector('h1')?.textContent).toContain('New ticket');
    await expectAccessible(page);
  });

  it('shows what is missing instead of submitting an empty form', async () => {
    const page = await openForm();

    await submit(page);

    expect(errorText(page, 'customer-email-error')).toBe('Enter the customer email.');
    expect(errorText(page, 'subject-error')).toBe('Provide a subject or a body.');
    expect(errorText(page, 'body-error')).toBe('Provide a subject or a body.');
    expect(page.querySelector('#subject')?.getAttribute('aria-invalid')).toBe('true');
    http.expectNone({ method: 'POST', url: '/api/v1/tickets' });
    await expectAccessible(page);
  });

  it('accepts a ticket with only a message', async () => {
    const page = await openForm();

    type(page, '#customer-email', 'anna.k@example.com');
    type(page, '#body', 'Please refund one charge.');
    await harness.fixture.whenStable();

    expect(errorText(page, 'subject-error')).toBe('');
    expect(errorText(page, 'body-error')).toBe('');
  });

  it('rejects an email address without an @', async () => {
    const page = await openForm();

    type(page, '#customer-email', 'anna');
    await harness.fixture.whenStable();

    expect(errorText(page, 'customer-email-error')).toBe('Enter a valid email address.');
  });

  it('rejects a subject over 200 characters', async () => {
    const page = await openForm();

    type(page, '#subject', 'x'.repeat(201));
    await harness.fixture.whenStable();

    expect(errorText(page, 'subject-error')).toBe('The subject can have at most 200 characters.');
  });

  it('counts characters once the message nears its limit', async () => {
    const page = await openForm();

    type(page, '#body', 'x'.repeat(9000));
    await harness.fixture.whenStable();
    expect(errorText(page, 'body-counter')).toBe('');

    type(page, '#body', 'x'.repeat(9001));
    await harness.fixture.whenStable();
    expect(errorText(page, 'body-counter')).toBe('9,001 / 10,000 characters');

    type(page, '#body', 'x'.repeat(10001));
    await harness.fixture.whenStable();
    expect(errorText(page, 'body-error')).toBe('The message can have at most 10,000 characters.');
    await expectAccessible(page);
  });

  it('creates the ticket in the selected tenant and opens it to watch triage', async () => {
    const page = await openForm();

    fillValidTicket(page);
    await submit(page);
    const request = expectCreateRequest();
    expect(request.request.headers.get('X-Tenant-Id')).toBe('contoso');
    expect(request.request.body).toEqual({
      customerEmail: 'anna.k@example.com',
      subject: 'Charged twice',
      body: 'Please refund one charge.',
    });
    expect(submitButton(page).disabled).toBe(true);
    expect(submitButton(page).textContent?.trim()).toBe('Creating…');

    request.flush({ id: 't-900', status: 'Pending' }, { status: 202, statusText: 'Accepted' });

    // The submit action resolves a few microtasks after the response, so poll for its effect.
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/tickets/t-900'));
  });

  it('puts server validation messages on their fields', async () => {
    const page = await openForm();

    fillValidTicket(page);
    await submit(page);
    expectCreateRequest().flush(
      {
        status: 400,
        errors: { CustomerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await vi.waitFor(() =>
      expect(errorText(page, 'customer-email-error')).toBe(
        'The CustomerEmail field is not a valid e-mail address.',
      ),
    );
    expect(page.querySelector('#customer-email')?.getAttribute('aria-invalid')).toBe('true');
    expect(submitButton(page).disabled).toBe(false);
    await expectAccessible(page);
  });

  it('shows a server error for a field the form does not have on the form', async () => {
    const page = await openForm();

    fillValidTicket(page);
    await submit(page);
    expectCreateRequest().flush(
      { status: 400, errors: { Priority: ['Priority cannot be set by the caller.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    await vi.waitFor(() =>
      expect(page.querySelector('[role="alert"]')?.textContent).toContain(
        'Priority cannot be set by the caller.',
      ),
    );
    expect(TestBed.inject(Router).url).toBe('/tickets/new');
  });

  it('shows an alert when the server cannot be reached', async () => {
    const page = await openForm();

    fillValidTicket(page);
    await submit(page);
    expectCreateRequest().error(new ProgressEvent('error'));

    await vi.waitFor(() =>
      expect(page.querySelector('[role="alert"]')?.textContent).toContain(
        'Cannot reach the server',
      ),
    );
    expect(TestBed.inject(Router).url).toBe('/tickets/new');
    await expectAccessible(page);
  });

  it('goes back to the list on Cancel', async () => {
    const page = await openForm();

    const cancel = Array.from(page.querySelectorAll('a')).find(
      (anchor) => anchor.textContent?.trim() === 'Cancel',
    );
    expect(cancel?.getAttribute('href')).toBe('/tickets');
  });
});
