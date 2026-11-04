import { ticketCountLabel } from './ticket-count-label';

describe('ticketCountLabel', () => {
  it('uses the singular for one ticket', () => {
    expect(ticketCountLabel(1)).toBe('1 ticket');
  });

  it('uses the plural for none or several tickets', () => {
    expect(ticketCountLabel(0)).toBe('0 tickets');
    expect(ticketCountLabel(12)).toBe('12 tickets');
  });
});
