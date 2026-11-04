import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { expectAccessible } from '../../testing/expect-accessible';
import { groupTickets } from './group-tickets';
import { classifiedTicket, pendingTicket, retryingTicket } from './testing-tickets';
import { TicketGrouping } from './ticket-list-view';
import { TicketQueueTable } from './ticket-queue-table';

const tickets = [
  classifiedTicket({ id: 'c-1', priority: 'Critical', category: 'Billing' }),
  pendingTicket({ id: 'p-1', subject: '' }),
  classifiedTicket({ id: 'c-2', priority: 'Critical', category: 'Technical' }),
  retryingTicket({ id: 'r-1' }),
  classifiedTicket({ id: 'l-1', priority: 'Low', subject: 'Change <b>email</b> address' }),
];

@Component({
  imports: [TicketQueueTable],
  template: `<app-ticket-queue-table [groups]="groups()" [grouping]="grouping()" />`,
})
class Host {
  readonly grouping = signal<TicketGrouping>('priority');
  readonly groups = signal(groupTickets(tickets, 'priority'));
}

describe('TicketQueueTable', () => {
  let fixture: ComponentFixture<Host>;
  let element: HTMLElement;

  async function render(grouping: TicketGrouping): Promise<void> {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(Host);
    fixture.componentInstance.grouping.set(grouping);
    fixture.componentInstance.groups.set(groupTickets(tickets, grouping));
    element = fixture.nativeElement;
    await fixture.whenStable();
  }

  // Each header reads as its name, the visible count and the count spoken to screen readers.
  function groupHeaders(): string[][] {
    return [...element.querySelectorAll('th[scope="rowgroup"]')].map((header) =>
      [...header.querySelectorAll('span > span, span > data')]
        .map((part) => part.textContent?.trim() ?? '')
        .filter((text) => text !== ''),
    );
  }

  it('heads each group with its name and count in a rowgroup header', async () => {
    await render('priority');

    expect(groupHeaders()).toEqual([
      ['Critical', '2', ', 2 tickets'],
      ['Low', '1', ', 1 ticket'],
      ['Not classified yet', '2', ', 2 tickets'],
    ]);
    expect(element.querySelectorAll('tbody')).toHaveLength(3);
    expect(element.querySelector('caption')?.textContent?.trim()).toBe(
      'Tickets grouped by priority, most urgent first, newest first within each group',
    );
    await expectAccessible(element);
  });

  it('colors the rail of a priority group with its severity and the unclassified group neutral', async () => {
    await render('priority');

    const rails = [...element.querySelectorAll('th[scope="rowgroup"] [aria-hidden="true"]')]
      .map((rail) => rail.className)
      .filter((name) => name.includes('rounded-full'));
    expect(rails[0]).toContain('bg-priority-critical');
    expect(rails[1]).toContain('bg-priority-low');
    expect(rails[2]).toContain('bg-border-strong');
  });

  it('links each subject to the ticket, shows the email and id, and the triage state', async () => {
    await render('priority');

    const links = [...element.querySelectorAll<HTMLAnchorElement>('tbody a')];
    expect(links.map((link) => link.textContent?.trim())).toEqual([
      'Change email address',
      'Change email address',
      'Change <b>email</b> address',
      '(no subject)',
      'App crashes on login',
    ]);
    expect(links[0]?.getAttribute('href')).toBe('/c-1');
    expect(links[2]?.title).toBe('Change <b>email</b> address');
    expect(element.querySelector('b')).toBeNull();
    const retryingRow = links[4]?.closest('tr');
    expect(retryingRow?.textContent).toContain('mark.t@example.com');
    expect(retryingRow?.textContent).toContain('r-1');
    expect(retryingRow?.textContent).toContain('Triage failed, retrying');
    expect(retryingRow?.querySelector('time')?.getAttribute('datetime')).toBe(
      '2026-10-28T08:12:00Z',
    );
  });

  it('groups by triage state with the state colors', async () => {
    await render('triage');

    expect(groupHeaders()).toEqual([
      ['Needs attention', '1', ', 1 ticket'],
      ['In triage', '1', ', 1 ticket'],
      ['Triaged', '3', ', 3 tickets'],
    ]);
  });

  it('shows one group without a header when not grouping', async () => {
    await render('none');

    expect(groupHeaders()).toEqual([]);
    expect(element.querySelectorAll('tbody tr')).toHaveLength(5);
    expect(element.querySelector('caption')?.textContent?.trim()).toBe('Tickets, newest first');
    await expectAccessible(element);
  });
});
