import { anyTicket, filtersClassification, toTicketParams } from './ticket-query';

describe('toTicketParams', () => {
  it('sends only the page size when nothing is filtered', () => {
    expect(toTicketParams(anyTicket)).toEqual({ pageSize: '50' });
  });

  it('repeats category and priority, and asks for hidden tickets only when wanted', () => {
    const params = toTicketParams(
      { categories: ['Billing', 'Technical'], priorities: ['Critical'], includeHidden: true },
      'token-2',
    );

    expect(params).toEqual({
      pageSize: '50',
      category: ['Billing', 'Technical'],
      priority: ['Critical'],
      includeHidden: 'true',
      continuationToken: 'token-2',
    });
  });
});

describe('filtersClassification', () => {
  it('is true once a category or priority is chosen', () => {
    expect(filtersClassification(anyTicket)).toBe(false);
    expect(filtersClassification({ ...anyTicket, categories: ['Other'] })).toBe(true);
    expect(filtersClassification({ ...anyTicket, priorities: ['Low'] })).toBe(true);
  });
});
