import { Ticket, TicketStatus } from './ticket';

// What the background worker's progress means to an agent. A failed attempt that will be retried is its own state,
// because the ticket stays unclassified and the agent may want to handle it by hand.
export type TriageState = 'queued' | 'classifying' | 'retrying' | 'triaged' | 'gave-up';

export const triageStateLabels: Readonly<Record<TriageState, string>> = {
  queued: 'Waiting for triage',
  classifying: 'Classifying',
  retrying: 'Triage failed, retrying',
  triaged: 'Triaged',
  'gave-up': 'Triage gave up',
};

export function toTriageState(ticket: Ticket): TriageState {
  switch (ticket.status) {
    case TicketStatus.COMPLETED:
      return 'triaged';
    case TicketStatus.FAILED:
      return 'gave-up';
    case TicketStatus.PROCESSING:
      return 'classifying';
    case TicketStatus.PENDING:
      return ticket.failureReason === null ? 'queued' : 'retrying';
    default:
      return ticket.status satisfies never;
  }
}
