import { Ticket, TicketStatus, TriageFailureReason } from '../tickets/ticket';
import { fakeClassify } from './fake-classify';
import { looksLikePromptInjection } from './prompt-injection';
import { QueuedTriage, StoredTicket } from './stored-ticket';

// The simulated background worker. Nothing runs on a timer: a ticket's state is worked out from how long ago it was
// queued every time it is read, so the demo shows tickets moving through triage and tests only move a clock.
export const pendingMs = 1500;
export const processingMs = 2500;
// After a failed attempt the ticket waits this long, Pending, before the worker claims it again.
export const retryDelayMs = 8000;

// The API's fixed operator messages (TriageFailureMessages.cs): reason text plus a retrying or hidden suffix.
export const invalidResponseMessage =
  'Automatic classification failed: the AI did not return an allowed category and priority. Triage will be retried automatically.';
export const promptInjectionMessage =
  'Flagged as a possible prompt injection: the ticket contains text that tries to instruct the AI, so it was not sent for automatic classification. Triage has stopped; please classify this ticket manually.';

type TicketBase = Omit<Ticket, 'status'>;

export function toTicketDto(stored: StoredTicket, now: number): Ticket {
  const base: TicketBase = {
    id: stored.id,
    customerEmail: stored.customerEmail,
    subject: stored.subject,
    body: stored.body,
    createdAt: stored.createdAt,
    category: null,
    priority: null,
    suggestedReply: stored.suggestedReply,
    classifiedAt: null,
    isHidden: false,
    failedTriageAttempts: 0,
    failureReason: null,
    failureMessage: null,
  };
  if (stored.triage.kind === 'imported') {
    return {
      ...base,
      status: TicketStatus.COMPLETED,
      category: stored.triage.category,
      priority: stored.triage.priority,
      classifiedAt: stored.createdAt,
    };
  }
  return toTriagedTicket(base, stored.triage, now);
}

function toTriagedTicket(base: TicketBase, triage: QueuedTriage, now: number): Ticket {
  if (!hasContent(base)) {
    return toRetryingTicket(base, now - triage.queuedAt);
  }
  const elapsed = now - triage.queuedAt;
  if (elapsed < pendingMs) {
    return { ...base, status: TicketStatus.PENDING };
  }
  if (elapsed < pendingMs + processingMs) {
    return { ...base, status: TicketStatus.PROCESSING };
  }
  if (looksLikePromptInjection(base.subject, base.body)) {
    return {
      ...base,
      status: TicketStatus.FAILED,
      isHidden: true,
      failureReason: TriageFailureReason.PROMPT_INJECTION,
      failureMessage: promptInjectionMessage,
    };
  }
  const classification = fakeClassify(base.subject, base.body);
  return {
    ...base,
    status: TicketStatus.COMPLETED,
    category: classification.category,
    priority: classification.priority,
    suggestedReply: base.suggestedReply ?? classification.suggestedReply,
    classifiedAt: new Date(triageEnd(triage)).toISOString(),
  };
}

// A ticket with neither subject nor body never gets a valid classification from the fake AI, so it shows the retry
// loop: every attempt fails, the attempt count grows, and the ticket waits Pending for the next one.
function toRetryingTicket(base: TicketBase, elapsed: number): Ticket {
  if (elapsed < pendingMs) {
    return { ...base, status: TicketStatus.PENDING };
  }
  const attemptCycleMs = processingMs + retryDelayMs;
  const sinceFirstClaim = elapsed - pendingMs;
  const finishedAttempts = Math.floor(sinceFirstClaim / attemptCycleMs);
  const isProcessing = sinceFirstClaim % attemptCycleMs < processingMs;
  const failedTriageAttempts = isProcessing ? finishedAttempts : finishedAttempts + 1;
  const status = isProcessing ? TicketStatus.PROCESSING : TicketStatus.PENDING;
  if (failedTriageAttempts === 0) {
    return { ...base, status };
  }
  // Like the API, the latest failure stays on the ticket while the next attempt is processing.
  return {
    ...base,
    status,
    failedTriageAttempts,
    failureReason: TriageFailureReason.INVALID_RESPONSE,
    failureMessage: invalidResponseMessage,
  };
}

function hasContent(ticket: TicketBase): boolean {
  return ticket.subject.trim() !== '' || ticket.body.trim() !== '';
}

function triageEnd(triage: QueuedTriage): number {
  return triage.queuedAt + pendingMs + processingMs;
}
