import { anyTicket } from './ticket-query';
import {
  hasFilters,
  sameTicketQuery,
  TicketListView,
  toTicketListParams,
  toTicketListView,
  withoutFilters,
} from './ticket-list-view';

const defaultView: TicketListView = {
  query: anyTicket,
  grouping: 'priority',
  triage: 'all',
  search: '',
};

describe('toTicketListView', () => {
  it('shows every ticket grouped by priority when the URL has no params', () => {
    expect(toTicketListView({})).toEqual(defaultView);
  });

  it('reads one or several categories and priorities in canonical order', () => {
    const view = toTicketListView({
      category: ['Shipping', 'Billing'],
      priority: 'High',
    });

    expect(view.query.categories).toEqual(['Billing', 'Shipping']);
    expect(view.query.priorities).toEqual(['High']);
  });

  it('orders priorities most urgent first and drops repeats', () => {
    const view = toTicketListView({ priority: ['Low', 'Critical', 'Low'] });

    expect(view.query.priorities).toEqual(['Critical', 'Low']);
  });

  it('ignores values that are not a category, priority, grouping or triage state', () => {
    const view = toTicketListView({
      category: ['billing', 'Nope', 'Account'],
      priority: 'Urgent',
      group: 'status',
      state: 'done',
    });

    expect(view.query.categories).toEqual(['Account']);
    expect(view.query.priorities).toEqual([]);
    expect(view.grouping).toBe('priority');
    expect(view.triage).toBe('all');
  });

  it('includes hidden tickets only for hidden=1', () => {
    expect(toTicketListView({ hidden: '1' }).query.includeHidden).toBe(true);
    expect(toTicketListView({ hidden: 'true' }).query.includeHidden).toBe(false);
  });

  it('reads the grouping, triage state and search', () => {
    expect(toTicketListView({ group: 'triage', state: 'in-triage', q: 'anna' })).toEqual({
      ...defaultView,
      grouping: 'triage',
      triage: 'in-triage',
      search: 'anna',
    });
  });

  it('uses the first value when a single-valued param is repeated', () => {
    expect(toTicketListView({ group: ['none', 'category'], q: ['a', 'b'] })).toMatchObject({
      grouping: 'none',
      search: 'a',
    });
  });
});

describe('toTicketListParams', () => {
  it('removes every param that has its default value', () => {
    expect(toTicketListParams(defaultView)).toEqual({
      category: null,
      priority: null,
      hidden: null,
      group: null,
      state: null,
      q: null,
    });
  });

  it('writes the view so toTicketListView reads it back', () => {
    const view: TicketListView = {
      query: { categories: ['Billing', 'Other'], priorities: ['Critical'], includeHidden: true },
      grouping: 'none',
      triage: 'attention',
      search: 'refund',
    };

    const params = toTicketListParams(view);

    expect(params).toEqual({
      category: ['Billing', 'Other'],
      priority: ['Critical'],
      hidden: '1',
      group: 'none',
      state: 'attention',
      q: 'refund',
    });
    expect(toTicketListView(params)).toEqual(view);
  });
});

describe('hasFilters and withoutFilters', () => {
  it('counts category, priority, triage state and search as filters', () => {
    expect(hasFilters(defaultView)).toBe(false);
    expect(hasFilters({ ...defaultView, query: { ...anyTicket, categories: ['Billing'] } })).toBe(
      true,
    );
    expect(hasFilters({ ...defaultView, query: { ...anyTicket, priorities: ['Low'] } })).toBe(true);
    expect(hasFilters({ ...defaultView, triage: 'triaged' })).toBe(true);
    expect(hasFilters({ ...defaultView, search: 'anna' })).toBe(true);
  });

  it('does not count grouping, hidden tickets or a blank search as filters', () => {
    expect(
      hasFilters({
        ...defaultView,
        grouping: 'none',
        search: '  ',
        query: { ...anyTicket, includeHidden: true },
      }),
    ).toBe(false);
  });

  it('clears the filters but keeps grouping and hidden tickets', () => {
    const view: TicketListView = {
      query: { categories: ['Billing'], priorities: ['High'], includeHidden: true },
      grouping: 'category',
      triage: 'attention',
      search: 'anna',
    };

    expect(withoutFilters(view)).toEqual({
      query: { categories: [], priorities: [], includeHidden: true },
      grouping: 'category',
      triage: 'all',
      search: '',
    });
  });
});

describe('sameTicketQuery', () => {
  it('treats queries with the same values as the same', () => {
    expect(
      sameTicketQuery(
        { categories: ['Billing'], priorities: ['High', 'Low'], includeHidden: true },
        { categories: ['Billing'], priorities: ['High', 'Low'], includeHidden: true },
      ),
    ).toBe(true);
  });

  it('tells queries apart by any value', () => {
    expect(sameTicketQuery(anyTicket, { ...anyTicket, includeHidden: true })).toBe(false);
    expect(sameTicketQuery(anyTicket, { ...anyTicket, categories: ['Billing'] })).toBe(false);
    expect(
      sameTicketQuery(
        { ...anyTicket, priorities: ['High'] },
        { ...anyTicket, priorities: ['Low'] },
      ),
    ).toBe(false);
  });
});
