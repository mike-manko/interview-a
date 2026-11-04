import { toSeedTicket } from './to-seed-ticket';

const queuedAt = 1_000;

const seed = {
  id: 't-1',
  tenantId: 'contoso',
  customerEmail: 'a@example.com',
  subject: 'Hi',
  body: 'Body',
  createdAt: '2026-10-28T08:12:00Z',
  category: null,
  priority: null,
  suggestedReply: null,
};

describe('toSeedTicket', () => {
  it('keeps the tenant and the source id', () => {
    expect(toSeedTicket(seed, queuedAt)).toMatchObject({ tenantId: 'contoso', id: 't-1' });
  });

  it('imports a ticket with an allowed category and priority as triaged', () => {
    const ticket = toSeedTicket(
      { ...seed, category: 'Account', priority: 'Low', suggestedReply: 'Reply' },
      queuedAt,
    );

    expect(ticket.triage).toEqual({ kind: 'imported', category: 'Account', priority: 'Low' });
    expect(ticket.suggestedReply).toBe('Reply');
  });

  it('queues a ticket whose priority is not an allowed name, keeping its suggested reply', () => {
    const ticket = toSeedTicket(
      { ...seed, category: 'Shipping', priority: 'Urgent', suggestedReply: 'Reply' },
      queuedAt,
    );

    expect(ticket.triage).toEqual({ kind: 'queued', queuedAt });
    expect(ticket.suggestedReply).toBe('Reply');
  });

  it('queues a ticket whose category is not an allowed name', () => {
    const ticket = toSeedTicket({ ...seed, category: 'tech', priority: 'Medium' }, queuedAt);

    expect(ticket.triage).toEqual({ kind: 'queued', queuedAt });
  });

  it('masks card data before storing the ticket', () => {
    const ticket = toSeedTicket({ ...seed, body: 'Card 4111 1111 1111 1111, CVV 123.' }, queuedAt);

    expect(ticket.body).toBe('Card [card ending 1111], CVV [removed].');
  });

  it('turns a missing subject into an empty string', () => {
    expect(toSeedTicket({ ...seed, subject: null }, queuedAt).subject).toBe('');
  });
});
