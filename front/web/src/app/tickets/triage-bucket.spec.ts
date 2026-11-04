import { classifiedTicket, hiddenTicket, pendingTicket, retryingTicket } from './testing-tickets';
import { countTriageBuckets, toTriageBucket } from './triage-bucket';

describe('toTriageBucket', () => {
  it('puts a ticket whose triage failed or gave up under Needs attention', () => {
    expect(toTriageBucket(retryingTicket())).toBe('attention');
    expect(toTriageBucket(hiddenTicket())).toBe('attention');
  });

  it('puts a queued or classifying ticket without a failure under In triage', () => {
    expect(toTriageBucket(pendingTicket())).toBe('in-triage');
    expect(toTriageBucket(pendingTicket({ status: 'Processing' }))).toBe('in-triage');
  });

  it('puts a classified ticket under Triaged', () => {
    expect(toTriageBucket(classifiedTicket())).toBe('triaged');
  });

  it('keeps a classified ticket that still carries a failure under Needs attention', () => {
    expect(toTriageBucket(classifiedTicket({ failureReason: 'InvalidResponse' }))).toBe(
      'attention',
    );
  });
});

describe('countTriageBuckets', () => {
  it('counts every ticket exactly once', () => {
    const tickets = [
      retryingTicket(),
      hiddenTicket(),
      pendingTicket(),
      classifiedTicket(),
      classifiedTicket({ id: 't-006' }),
    ];

    const counts = countTriageBuckets(tickets);

    expect(counts).toEqual({ attention: 2, 'in-triage': 1, triaged: 2 });
    expect(counts.attention + counts['in-triage'] + counts.triaged).toBe(tickets.length);
  });

  it('counts zero for an empty queue', () => {
    expect(countTriageBuckets([])).toEqual({ attention: 0, 'in-triage': 0, triaged: 0 });
  });
});
