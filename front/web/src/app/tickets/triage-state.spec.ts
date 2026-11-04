import { classifiedTicket, hiddenTicket, pendingTicket, retryingTicket } from './testing-tickets';
import { toTriageState, triageStateLabels } from './triage-state';

describe('toTriageState', () => {
  it('maps each API status to what it means for an agent', () => {
    expect(toTriageState(pendingTicket())).toBe('queued');
    expect(toTriageState(pendingTicket({ status: 'Processing' }))).toBe('classifying');
    expect(toTriageState(retryingTicket())).toBe('retrying');
    expect(toTriageState(classifiedTicket())).toBe('triaged');
    expect(toTriageState(hiddenTicket())).toBe('gave-up');
  });

  it('has a label for every state', () => {
    expect(triageStateLabels['gave-up']).toBe('Triage gave up');
  });
});
