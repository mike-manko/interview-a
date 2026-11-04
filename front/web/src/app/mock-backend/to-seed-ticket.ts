import {
  TicketCategory,
  ticketCategories,
  TicketPriority,
  ticketPriorities,
} from '../tickets/ticket';
import { maskPaymentCard } from './mask-payment-card';
import { ImportedTriage, QueuedTriage, StoredTicket } from './stored-ticket';

// Mirrors the API's Ticket.Import: card data is masked, and a category or priority outside the allowed names ("tech",
// "Urgent") is dropped together with its partner, so the ticket goes through triage. A valid pair is kept as triaged.
// The suggested reply is kept either way. queuedAt is when the simulated worker picks the ticket up if it needs triage.
export function toSeedTicket(
  raw: Readonly<Record<string, unknown>>,
  queuedAt: number,
): StoredTicket {
  const category = asCategory(raw['category']);
  const priority = asPriority(raw['priority']);
  const suggestedReply = asString(raw['suggestedReply']);
  return {
    tenantId: asString(raw['tenantId']) ?? '',
    id: asString(raw['id']) ?? '',
    customerEmail: asString(raw['customerEmail']) ?? '',
    subject: maskPaymentCard(asString(raw['subject']) ?? ''),
    body: maskPaymentCard(asString(raw['body']) ?? ''),
    createdAt: asString(raw['createdAt']) ?? '',
    suggestedReply: suggestedReply === null || suggestedReply.trim() === '' ? null : suggestedReply,
    triage: toTriage(category, priority, queuedAt),
  };
}

function toTriage(
  category: TicketCategory | null,
  priority: TicketPriority | null,
  queuedAt: number,
): ImportedTriage | QueuedTriage {
  if (category === null || priority === null) {
    return { kind: 'queued', queuedAt };
  }
  return { kind: 'imported', category, priority };
}

function asCategory(value: unknown): TicketCategory | null {
  return ticketCategories.find((category) => category === value) ?? null;
}

function asPriority(value: unknown): TicketPriority | null {
  return ticketPriorities.find((priority) => priority === value) ?? null;
}

function asString(value: unknown): string | null {
  return typeof value === 'string' ? value : null;
}
