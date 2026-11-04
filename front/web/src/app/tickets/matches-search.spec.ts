import { matchesSearch } from './matches-search';
import { pendingTicket } from './testing-tickets';

describe('matchesSearch', () => {
  const ticket = pendingTicket({
    id: 'T-123-abc',
    subject: 'Charged twice for October',
    customerEmail: 'Anna.K@example.com',
  });

  it('matches every ticket when the search is empty or blank', () => {
    expect(matchesSearch(ticket, '')).toBe(true);
    expect(matchesSearch(ticket, '   ')).toBe(true);
  });

  it('matches the subject, ignoring case and surrounding spaces', () => {
    expect(matchesSearch(ticket, '  CHARGED twice ')).toBe(true);
  });

  it('matches the customer email', () => {
    expect(matchesSearch(ticket, 'anna.k@')).toBe(true);
  });

  it('matches the ticket id', () => {
    expect(matchesSearch(ticket, 't-123')).toBe(true);
  });

  it('does not match text that is only in the body', () => {
    expect(matchesSearch(ticket, '49 EUR')).toBe(false);
  });
});
