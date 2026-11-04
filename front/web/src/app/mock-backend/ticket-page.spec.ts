import { Ticket } from '../tickets/ticket';
import { decodeContinuationToken } from './continuation-token';
import { TicketListQuery } from './ticket-list-query';
import { selectPage } from './ticket-page';

function ticket(id: string, createdAt: string, changes: Partial<Ticket> = {}): Ticket {
  return {
    id,
    customerEmail: 'a@example.com',
    subject: id,
    body: '',
    status: 'Completed',
    category: 'Billing',
    priority: 'High',
    suggestedReply: null,
    createdAt,
    classifiedAt: createdAt,
    isHidden: false,
    failedTriageAttempts: 0,
    failureReason: null,
    failureMessage: null,
    ...changes,
  };
}

const firstPage: TicketListQuery = {
  pageSize: 20,
  after: null,
  categories: [],
  priorities: [],
  includeHidden: false,
};

const tickets: readonly Ticket[] = [
  ticket('t-1', '2026-10-28T08:00:00Z'),
  ticket('t-3', '2026-10-28T10:00:00Z', { category: 'Technical', priority: 'Low' }),
  ticket('t-2', '2026-10-28T09:00:00Z', { status: 'Pending', category: null, priority: null }),
  ticket('t-4', '2026-10-28T11:00:00.000Z', {
    status: 'Failed',
    isHidden: true,
    category: null,
    priority: null,
  }),
];

function ids(page: { readonly items: readonly Ticket[] }): readonly string[] {
  return page.items.map((item) => item.id);
}

describe('selectPage', () => {
  it('lists visible tickets newest first', () => {
    expect(ids(selectPage(tickets, firstPage))).toEqual(['t-3', 't-2', 't-1']);
  });

  it('lists hidden tickets only when asked', () => {
    expect(ids(selectPage(tickets, { ...firstPage, includeHidden: true }))).toEqual([
      't-4',
      't-3',
      't-2',
      't-1',
    ]);
  });

  it('keeps tickets with one of the given categories and leaves out unclassified ones', () => {
    const page = selectPage(tickets, { ...firstPage, categories: ['Billing', 'Technical'] });

    expect(ids(page)).toEqual(['t-3', 't-1']);
  });

  it('keeps tickets with one of the given priorities', () => {
    expect(ids(selectPage(tickets, { ...firstPage, priorities: ['High'] }))).toEqual(['t-1']);
  });

  it('pages with a continuation token until the last page', () => {
    const page1 = selectPage(tickets, { ...firstPage, pageSize: 2 });
    const after =
      page1.continuationToken === null ? null : decodeContinuationToken(page1.continuationToken);
    const page2 = selectPage(tickets, { ...firstPage, pageSize: 2, after: after ?? null });

    expect(ids(page1)).toEqual(['t-3', 't-2']);
    expect(ids(page2)).toEqual(['t-1']);
    expect(page2.continuationToken).toBeNull();
  });

  it('does not repeat tickets when a newer one arrives between pages', () => {
    const page1 = selectPage(tickets, { ...firstPage, pageSize: 2 });
    const after =
      page1.continuationToken === null ? null : decodeContinuationToken(page1.continuationToken);
    const withNewer = [...tickets, ticket('t-5', '2026-10-28T12:00:00Z')];

    expect(ids(selectPage(withNewer, { ...firstPage, pageSize: 2, after: after ?? null }))).toEqual(
      ['t-1'],
    );
  });

  it('returns no token when everything fits on one page', () => {
    expect(selectPage(tickets, firstPage).continuationToken).toBeNull();
  });
});
