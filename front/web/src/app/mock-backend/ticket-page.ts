import { Ticket, TicketPage } from '../tickets/ticket';
import { encodeContinuationToken, TicketKey } from './continuation-token';
import { TicketListQuery } from './ticket-list-query';

// One page of GET /api/v1/tickets: newest first, filtered, and a token for the next page (null on the last one).
export function selectPage(tickets: readonly Ticket[], query: TicketListQuery): TicketPage {
  // filter() returns a new array, so sorting it leaves the caller's array alone.
  const matching = tickets.filter((ticket) => matches(ticket, query)).sort(newestFirst);
  const after = query.after;
  const remaining =
    after === null ? matching : matching.filter((ticket) => comesAfter(ticket, after));
  const items = remaining.slice(0, query.pageSize);
  const last = items.at(-1);
  const hasMore = remaining.length > items.length;
  return {
    items,
    continuationToken: hasMore && last ? encodeContinuationToken(keyOf(last)) : null,
  };
}

// A category or priority filter leaves out tickets without a classification yet, as the API does.
function matches(ticket: Ticket, query: TicketListQuery): boolean {
  if (ticket.isHidden && !query.includeHidden) {
    return false;
  }
  if (query.categories.length > 0) {
    if (ticket.category === null || !query.categories.includes(ticket.category)) {
      return false;
    }
  }
  if (query.priorities.length > 0) {
    if (ticket.priority === null || !query.priorities.includes(ticket.priority)) {
      return false;
    }
  }
  return true;
}

// createdAt descending; the id breaks ties so the order, and so the paging, is stable.
function newestFirst(first: Ticket, second: Ticket): number {
  return compareKeys(keyOf(second), keyOf(first));
}

function comesAfter(ticket: Ticket, key: TicketKey): boolean {
  return compareKeys(keyOf(ticket), key) < 0;
}

function compareKeys(first: TicketKey, second: TicketKey): number {
  if (first.createdAt !== second.createdAt) {
    return first.createdAt - second.createdAt;
  }
  if (first.id === second.id) {
    return 0;
  }
  return first.id < second.id ? -1 : 1;
}

function keyOf(ticket: Ticket): TicketKey {
  return { createdAt: Date.parse(ticket.createdAt), id: ticket.id };
}
