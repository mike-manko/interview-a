import { groupTickets, TicketGroup } from './group-tickets';
import { classifiedTicket, hiddenTicket, pendingTicket, retryingTicket } from './testing-tickets';

function summary(groups: readonly TicketGroup[]): [string, string, string[]][] {
  return groups.map((group) => [group.label, group.tone, group.tickets.map((ticket) => ticket.id)]);
}

describe('groupTickets', () => {
  // Newest first, as the API returns them.
  const tickets = [
    classifiedTicket({ id: 'low-new', priority: 'Low', category: 'Shipping' }),
    pendingTicket({ id: 'pending' }),
    classifiedTicket({ id: 'critical', priority: 'Critical', category: 'Billing' }),
    retryingTicket({ id: 'retrying' }),
    classifiedTicket({ id: 'low-old', priority: 'Low', category: 'Billing' }),
    hiddenTicket({ id: 'gave-up' }),
  ];

  it('groups by priority most urgent first, then the unclassified, leaving out empty groups', () => {
    expect(summary(groupTickets(tickets, 'priority'))).toEqual([
      ['Critical', 'critical', ['critical']],
      ['Low', 'low', ['low-new', 'low-old']],
      ['Not classified yet', 'neutral', ['pending', 'retrying', 'gave-up']],
    ]);
  });

  it('gives each priority group its severity tone', () => {
    const groups = groupTickets(
      [
        classifiedTicket({ id: 'h', priority: 'High' }),
        classifiedTicket({ id: 'm', priority: 'Medium' }),
      ],
      'priority',
    );

    expect(summary(groups)).toEqual([
      ['High', 'high', ['h']],
      ['Medium', 'medium', ['m']],
    ]);
  });

  it('groups by category in the API order, then the unclassified', () => {
    expect(summary(groupTickets(tickets, 'category'))).toEqual([
      ['Billing', 'neutral', ['critical', 'low-old']],
      ['Shipping', 'neutral', ['low-new']],
      ['Not classified yet', 'neutral', ['pending', 'retrying', 'gave-up']],
    ]);
  });

  it('groups by triage state: needs attention, in triage, triaged', () => {
    expect(summary(groupTickets(tickets, 'triage'))).toEqual([
      ['Needs attention', 'danger', ['retrying', 'gave-up']],
      ['In triage', 'primary', ['pending']],
      ['Triaged', 'success', ['low-new', 'critical', 'low-old']],
    ]);
  });

  it('keeps one group in API order when not grouping', () => {
    expect(summary(groupTickets(tickets, 'none'))).toEqual([
      [
        'All tickets',
        'neutral',
        ['low-new', 'pending', 'critical', 'retrying', 'low-old', 'gave-up'],
      ],
    ]);
  });

  it('returns no groups for no tickets', () => {
    expect(groupTickets([], 'priority')).toEqual([]);
    expect(groupTickets([], 'none')).toEqual([]);
  });
});
