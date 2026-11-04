import { TestBed } from '@angular/core/testing';
import { mockClock } from './mock-clock';
import { MockTicketsDb, seedStaggerMs } from './mock-tickets-db';
import { pendingMs, processingMs } from './triage-simulation';

const startedAt = Date.parse('2026-11-04T10:00:00Z');
const allTriaged = 60_000;

describe('MockTicketsDb', () => {
  let now: number;
  let db: MockTicketsDb;

  beforeEach(() => {
    now = startedAt;
    TestBed.configureTestingModule({ providers: [{ provide: mockClock, useValue: () => now }] });
    db = TestBed.inject(MockTicketsDb);
  });

  function ids(tenantId: string): readonly string[] {
    return db.list(tenantId).map((ticket) => ticket.id);
  }

  it("seeds each tenant with its own tickets and no one else's", () => {
    expect(ids('contoso')).toContain('t-001');
    expect(ids('contoso')).not.toContain('t-101');
    expect(ids('fabrikam')).toContain('t-101');
    expect(ids('fabrikam')).not.toContain('t-001');
    expect(ids('northwind')).toEqual([]);
  });

  it("never finds another tenant's ticket", () => {
    expect(db.find('contoso', 't-001')).toBeDefined();
    expect(db.find('fabrikam', 't-001')).toBeUndefined();
  });

  it('imports a seed ticket with a valid classification as completed', () => {
    expect(db.find('contoso', 't-004')).toMatchObject({
      status: 'Completed',
      category: 'Account',
      priority: 'Low',
    });
  });

  it('queues seed tickets with values outside the contract for triage', () => {
    expect(db.find('contoso', 't-012')).toMatchObject({ status: 'Pending', priority: null });
    expect(db.find('contoso', 't-013')).toMatchObject({ status: 'Pending', category: null });
  });

  it("staggers each tenant's seed tickets through the worker", () => {
    now = startedAt + pendingMs + processingMs;

    expect(db.find('contoso', 't-001')?.status).toBe('Completed');
    expect(db.find('contoso', 't-002')?.status).toBe('Processing');
    expect(db.find('fabrikam', 't-101')?.status).toBe('Completed');

    now = startedAt + seedStaggerMs + pendingMs + processingMs;
    expect(db.find('contoso', 't-002')?.status).toBe('Completed');
  });

  it('triages the seed tickets like the demo expects', () => {
    now = startedAt + allTriaged;

    expect(db.find('contoso', 't-001')).toMatchObject({ category: 'Billing', priority: 'High' });
    expect(db.find('contoso', 't-005')).toMatchObject({
      status: 'Failed',
      isHidden: true,
      failureReason: 'PromptInjection',
    });
    expect(db.find('contoso', 't-007')).toMatchObject({
      status: expect.stringMatching(/^(Pending|Processing)$/),
      failureReason: 'InvalidResponse',
      failedTriageAttempts: expect.any(Number),
    });
    expect(db.find('contoso', 't-012')).toMatchObject({
      status: 'Completed',
      category: 'Shipping',
      suggestedReply: 'Hi Adam, sorry about that. We will send a return label today.',
    });
    expect(db.find('contoso', 't-013')).toMatchObject({
      status: 'Completed',
      category: 'Technical',
    });
  });

  it('masks the card number in the seed data', () => {
    expect(db.find('contoso', 't-009')?.body).toContain('[card ending 1111]');
    expect(db.find('contoso', 't-009')?.body).toContain('CVV [removed]');
    expect(db.find('contoso', 't-009')?.body).not.toContain('4111');
  });

  it('stores a new ticket for its tenant only, masked and pending', () => {
    const created = db.create('fabrikam', {
      customerEmail: 'a@example.com',
      subject: 'Card',
      body: 'Use 4111-1111-1111-1111 please',
    });

    expect(created).toEqual({ id: 'mock-1', status: 'Pending' });
    expect(db.find('fabrikam', 'mock-1')).toMatchObject({
      body: 'Use [card ending 1111] please',
      status: 'Pending',
      createdAt: new Date(startedAt).toISOString(),
    });
    expect(db.find('contoso', 'mock-1')).toBeUndefined();
  });

  it('triages a new ticket from the moment it was created', () => {
    now = startedAt + allTriaged;
    db.create('contoso', { customerEmail: 'a@example.com', subject: 'Refund', body: 'Please.' });

    expect(db.find('contoso', 'mock-1')?.status).toBe('Pending');

    now += pendingMs + processingMs;
    expect(db.find('contoso', 'mock-1')).toMatchObject({
      status: 'Completed',
      category: 'Billing',
    });
  });

  it('gives every new ticket its own id', () => {
    const first = db.create('contoso', { customerEmail: 'a@example.com', subject: 'A', body: '' });
    const second = db.create('fabrikam', {
      customerEmail: 'a@example.com',
      subject: 'B',
      body: '',
    });

    expect([first.id, second.id]).toEqual(['mock-1', 'mock-2']);
  });
});
