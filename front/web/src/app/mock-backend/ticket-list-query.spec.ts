import { encodeContinuationToken } from './continuation-token';
import { parseTicketListQuery } from './ticket-list-query';

function parse(query: string) {
  return parseTicketListQuery(new URLSearchParams(query));
}

describe('parseTicketListQuery', () => {
  it('defaults to the first page of 20 visible tickets with no filters', () => {
    expect(parse('')).toEqual({
      kind: 'query',
      query: { pageSize: 20, after: null, categories: [], priorities: [], includeHidden: false },
    });
  });

  it.each([
    ['0', 1],
    ['-5', 1],
    ['50', 50],
    ['500', 100],
  ])('clamps pageSize %s to %d', (pageSize, expected) => {
    expect(parse(`pageSize=${pageSize}`)).toMatchObject({ query: { pageSize: expected } });
  });

  it('reads repeated category and priority filters and includeHidden', () => {
    expect(
      parse('category=Billing&category=Technical&priority=High&includeHidden=true'),
    ).toMatchObject({
      query: { categories: ['Billing', 'Technical'], priorities: ['High'], includeHidden: true },
    });
  });

  it('reports every filter name that is not allowed', () => {
    expect(parse('category=tech&category=Billing&priority=Urgent')).toEqual({
      kind: 'domain-errors',
      errors: [
        {
          code: 2203,
          key: 'category',
          message:
            "Category filter 'tech' is not one of: Billing, Technical, Account, Shipping, Other.",
        },
        {
          code: 2204,
          key: 'priority',
          message: "Priority filter 'Urgent' is not one of: Low, Medium, High, Critical.",
        },
      ],
    });
  });

  it('matches filter names exactly', () => {
    expect(parse('category=billing')).toMatchObject({ kind: 'domain-errors' });
  });

  it('decodes a continuation token', () => {
    const key = { createdAt: 5, id: 't-1' };

    expect(parse(`continuationToken=${encodeContinuationToken(key)}`)).toMatchObject({
      query: { after: key },
    });
  });

  it('rejects a continuation token it did not issue', () => {
    expect(parse('continuationToken=garbage')).toEqual({
      kind: 'domain-errors',
      errors: [
        {
          code: 4001,
          key: 'continuationToken',
          message: 'The continuation token is not valid; start again from the first page.',
        },
      ],
    });
  });

  it('reports values model binding cannot convert', () => {
    expect(parse('pageSize=ten&includeHidden=maybe')).toEqual({
      kind: 'binding-errors',
      errors: {
        pageSize: ["The value 'ten' is not valid."],
        includeHidden: ["The value 'maybe' is not valid."],
      },
    });
  });
});
