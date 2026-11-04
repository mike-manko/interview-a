import {
  ContinuationPageDtoOfTicketDto,
  CreateTicketRequest,
  TicketCategory,
  TicketCreatedDto,
  TicketDto,
  TicketPriority,
  TicketStatus,
  TriageFailureReason,
} from '../api/generated';

// The generated DTOs mirror the OpenAPI document; these aliases add the house readonly rule on top.
export type Ticket = Readonly<TicketDto>;
export type NewTicket = Readonly<CreateTicketRequest>;
export type CreatedTicket = Readonly<TicketCreatedDto>;
export interface TicketPage {
  readonly items: readonly Ticket[];
  readonly continuationToken: ContinuationPageDtoOfTicketDto['continuationToken'];
}
// Re-exported as values too, so features use TicketStatus.PENDING instead of string literals.
export { TicketCategory, TicketPriority, TicketStatus, TriageFailureReason };

export const ticketCategories: readonly TicketCategory[] = Object.values(TicketCategory);

// Most urgent first: the order agents work in, and the order priority groups and filters are shown in.
export const ticketPriorities: readonly TicketPriority[] = [
  TicketPriority.CRITICAL,
  TicketPriority.HIGH,
  TicketPriority.MEDIUM,
  TicketPriority.LOW,
];

// The background worker has not finished with the ticket yet: it may still get a category and priority.
export function isInTriage(ticket: Ticket): boolean {
  return ticket.status === TicketStatus.PENDING || ticket.status === TicketStatus.PROCESSING;
}

// The latest triage attempt failed (it may be retried) or triage gave up; an agent should look at it.
export function needsAttention(ticket: Ticket): boolean {
  return ticket.status === TicketStatus.FAILED || ticket.failureReason !== null;
}
