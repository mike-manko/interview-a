import { describeApiError } from './describe-api-error';

describe('describeApiError', () => {
  it('tells the user to check the connection on a network error', () => {
    expect(describeApiError({ kind: 'network' })).toContain('Cannot reach the server');
  });

  it('shows the support reference of a server error', () => {
    const message = describeApiError({
      kind: 'server',
      problem: { title: 'Classification failed', traceId: '00-abc' },
    });

    expect(message).toBe('Classification failed (support reference: 00-abc)');
  });

  it('falls back to a generic sentence for an unknown error', () => {
    expect(describeApiError(new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});
