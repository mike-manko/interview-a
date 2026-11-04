import { TicketCategory, TicketPriority } from './ticket';

// The filters GET /api/v1/tickets applies on the server. An empty list means "any value"; the API leaves
// unclassified tickets out as soon as a category or priority filter is set.
export interface TicketQuery {
  readonly categories: readonly TicketCategory[];
  readonly priorities: readonly TicketPriority[];
  readonly includeHidden: boolean;
}

export const anyTicket: TicketQuery = { categories: [], priorities: [], includeHidden: false };

// Large enough that one page usually holds a day's queue; "Load more" fetches the rest.
export const ticketPageSize = 50;

export type TicketParams = Readonly<Record<string, string | readonly string[]>>;

export function toTicketParams(query: TicketQuery, continuationToken?: string): TicketParams {
  const params: Record<string, string | readonly string[]> = {
    pageSize: String(ticketPageSize),
  };
  if (query.categories.length > 0) {
    params['category'] = query.categories;
  }
  if (query.priorities.length > 0) {
    params['priority'] = query.priorities;
  }
  if (query.includeHidden) {
    params['includeHidden'] = 'true';
  }
  if (continuationToken !== undefined) {
    params['continuationToken'] = continuationToken;
  }
  return params;
}

export function filtersClassification(query: TicketQuery): boolean {
  return query.categories.length > 0 || query.priorities.length > 0;
}
