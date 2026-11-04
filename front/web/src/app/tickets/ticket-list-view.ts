import { Params } from '@angular/router';
import { ticketCategories, ticketPriorities } from './ticket';
import { TicketQuery } from './ticket-query';
import { TriageBucket, triageBuckets } from './triage-bucket';

export type TicketGrouping = 'priority' | 'category' | 'triage' | 'none';

export const ticketGroupings: readonly TicketGrouping[] = [
  'priority',
  'category',
  'triage',
  'none',
];

export type TriageFilter = 'all' | TriageBucket;

// Everything the queue shows lives in the URL, so a filtered queue can be shared, reloaded and survives opening a
// ticket. The query goes to the API; grouping, triage and search work on the tickets already loaded.
export interface TicketListView {
  readonly query: TicketQuery;
  readonly grouping: TicketGrouping;
  readonly triage: TriageFilter;
  readonly search: string;
}

// A query param as the router binds it to an input: absent, once, or repeated.
export type QueryParamValue = string | readonly string[] | undefined;

export interface TicketListParams {
  readonly category?: QueryParamValue;
  readonly priority?: QueryParamValue;
  readonly hidden?: QueryParamValue;
  readonly group?: QueryParamValue;
  readonly state?: QueryParamValue;
  readonly q?: QueryParamValue;
}

// URLs are typed by hand and shared, so unknown values are ignored instead of failing the whole page.
export function toTicketListView(params: TicketListParams): TicketListView {
  const categories = allValues(params.category);
  const priorities = allValues(params.priority);
  const group = firstValue(params.group);
  const state = firstValue(params.state);
  return {
    query: {
      categories: ticketCategories.filter((category) => categories.includes(category)),
      priorities: ticketPriorities.filter((priority) => priorities.includes(priority)),
      includeHidden: firstValue(params.hidden) === '1',
    },
    grouping: ticketGroupings.find((grouping) => grouping === group) ?? 'priority',
    triage: triageBuckets.find((bucket) => bucket === state) ?? 'all',
    search: firstValue(params.q) ?? '',
  };
}

// Defaults map to null, which removes the param when navigating with queryParamsHandling 'merge'.
export function toTicketListParams(view: TicketListView): Params {
  return {
    category: view.query.categories.length > 0 ? view.query.categories : null,
    priority: view.query.priorities.length > 0 ? view.query.priorities : null,
    hidden: view.query.includeHidden ? '1' : null,
    group: view.grouping === 'priority' ? null : view.grouping,
    state: view.triage === 'all' ? null : view.triage,
    q: view.search === '' ? null : view.search,
  };
}

// Narrowing filters only: grouping and "show tickets triage gave up on" change how much is shown, not what matches.
export function hasFilters(view: TicketListView): boolean {
  return (
    view.query.categories.length > 0 ||
    view.query.priorities.length > 0 ||
    view.triage !== 'all' ||
    view.search.trim() !== ''
  );
}

export function withoutFilters(view: TicketListView): TicketListView {
  return {
    ...view,
    query: { ...view.query, categories: [], priorities: [] },
    triage: 'all',
    search: '',
  };
}

// Lists are kept in canonical order by toTicketListView, so element-wise comparison is enough.
export function sameTicketQuery(first: TicketQuery, second: TicketQuery): boolean {
  return (
    first.includeHidden === second.includeHidden &&
    sameValues(first.categories, second.categories) &&
    sameValues(first.priorities, second.priorities)
  );
}

function sameValues(first: readonly string[], second: readonly string[]): boolean {
  return first.length === second.length && first.every((value, index) => value === second[index]);
}

function allValues(value: QueryParamValue): readonly string[] {
  if (value === undefined) {
    return [];
  }
  if (typeof value === 'string') {
    return [value];
  }
  return value;
}

function firstValue(value: QueryParamValue): string | undefined {
  return allValues(value)[0];
}
