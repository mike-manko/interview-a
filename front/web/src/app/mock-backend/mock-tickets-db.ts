import { inject, Service } from '@angular/core';
import { CreatedTicket, Ticket, TicketStatus } from '../tickets/ticket';
import { maskPaymentCard } from './mask-payment-card';
import { mockClock } from './mock-clock';
import seedTickets from './seed-tickets.json';
import { StoredTicket } from './stored-ticket';
import { toSeedTicket } from './to-seed-ticket';
import { toTicketDto } from './triage-simulation';
import { ValidNewTicket } from './validate-new-ticket';

// Each tenant's seed tickets reach the simulated worker this far apart, so the demo shows them triaged one by one.
export const seedStaggerMs = 400;

// In-memory stand-in for the API's tenant-partitioned store; it resets on every page reload. Every method takes the
// tenant, and nothing is ever returned across tenants: another tenant's ticket is simply not found.
@Service()
export class MockTicketsDb {
  private readonly clock = inject(mockClock);
  private tickets: readonly StoredTicket[] = seed(this.clock());
  private nextId = 1;

  /** All of the tenant's tickets as the API would show them now, hidden ones included, in no particular order. */
  list(tenantId: string): readonly Ticket[] {
    const now = this.clock();
    return this.tickets
      .filter((ticket) => ticket.tenantId === tenantId)
      .map((ticket) => toTicketDto(ticket, now));
  }

  find(tenantId: string, ticketId: string): Ticket | undefined {
    const stored = this.tickets.find(
      (ticket) => ticket.tenantId === tenantId && ticket.id === ticketId,
    );
    return stored === undefined ? undefined : toTicketDto(stored, this.clock());
  }

  /** Stores the ticket with its card data masked and queues it for the simulated worker. */
  create(tenantId: string, request: ValidNewTicket): CreatedTicket {
    const now = this.clock();
    const ticket: StoredTicket = {
      tenantId,
      id: `mock-${this.nextId}`,
      customerEmail: request.customerEmail,
      subject: maskPaymentCard(request.subject),
      body: maskPaymentCard(request.body),
      createdAt: new Date(now).toISOString(),
      suggestedReply: null,
      triage: { kind: 'queued', queuedAt: now },
    };
    this.nextId += 1;
    this.tickets = [...this.tickets, ticket];
    return { id: ticket.id, status: TicketStatus.PENDING };
  }
}

function seed(startedAt: number): readonly StoredTicket[] {
  const queuedPerTenant = new Map<string, number>();
  return seedTickets.map((raw) => {
    const position = queuedPerTenant.get(raw.tenantId) ?? 0;
    queuedPerTenant.set(raw.tenantId, position + 1);
    return toSeedTicket(raw, startedAt + position * seedStaggerMs);
  });
}
