import { TicketCategory, TicketPriority } from '../tickets/ticket';

// A seed ticket that arrived with an allowed category and priority: imported as triaged, like the API's seed import.
export interface ImportedTriage {
  readonly kind: 'imported';
  readonly category: TicketCategory;
  readonly priority: TicketPriority;
}

// A ticket waiting for the simulated worker; its state follows from how long ago it was queued (triage-simulation.ts).
export interface QueuedTriage {
  readonly kind: 'queued';
  readonly queuedAt: number;
}

// What the mock keeps per ticket. Unlike the API's TicketDto it knows the tenant, so every read can be partitioned.
export interface StoredTicket {
  readonly tenantId: string;
  readonly id: string;
  readonly customerEmail: string;
  readonly subject: string;
  readonly body: string;
  readonly createdAt: string;
  readonly suggestedReply: string | null;
  readonly triage: ImportedTriage | QueuedTriage;
}
