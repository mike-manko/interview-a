import { decodeContinuationToken, encodeContinuationToken } from './continuation-token';

describe('continuation token', () => {
  it('round-trips the key of the last ticket on a page', () => {
    const key = { createdAt: Date.parse('2026-10-28T08:12:00Z'), id: 't-001' };

    expect(decodeContinuationToken(encodeContinuationToken(key))).toEqual(key);
  });

  it('is safe in a query string without encoding', () => {
    const token = encodeContinuationToken({ createdAt: 1, id: 'ä?&=/+' });

    expect(token).toMatch(/^[A-Za-z0-9_-]+$/);
  });

  it.each(['not a token', 'e30', btoa('{"createdAt":"x","id":1}'), ''])('rejects "%s"', (token) => {
    expect(decodeContinuationToken(token)).toBeUndefined();
  });
});
