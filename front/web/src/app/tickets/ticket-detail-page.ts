import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { describeApiError } from '../api/describe-api-error';
import { findApiError } from '../api/problem-details';
import { TenantContext } from '../tenancy/tenant-context';
import { CategoryBadge } from './category-badge';
import { PriorityBadge } from './priority-badge';
import { SuggestedReply } from './suggested-reply';
import { Ticket } from './ticket';
import { TicketSubject } from './ticket-subject';
import { TicketsApi } from './tickets-api';
import { toTriageState, TriageState } from './triage-state';
import { TriageStateBadge } from './triage-state-badge';

interface TicketDetail {
  readonly ticket: Ticket;
  readonly state: TriageState;
  readonly mailto: string;
  readonly hasMessage: boolean;
  readonly failedAttempts: string;
  readonly calloutClass: string;
}

// The worker picks up a queued ticket within seconds; a retry waits for its backoff, so asking often is wasted.
// A finished ticket no longer changes, so polling stops.
const pollDelaysMs: Readonly<Record<TriageState, number | undefined>> = {
  queued: 3000,
  classifying: 3000,
  retrying: 15000,
  triaged: undefined,
  'gave-up': undefined,
};

// A retry may still succeed, so it warns; giving up is final, so it reads as an error.
const calloutColors: Readonly<Record<TriageState, string>> = {
  queued: 'bg-warning-soft',
  classifying: 'bg-warning-soft',
  retrying: 'bg-warning-soft',
  triaged: 'bg-danger-soft',
  'gave-up': 'bg-danger-soft',
};

@Component({
  selector: 'app-ticket-detail-page',
  imports: [
    CategoryBadge,
    DatePipe,
    PriorityBadge,
    RouterLink,
    SuggestedReply,
    TicketSubject,
    TriageStateBadge,
  ],
  templateUrl: './ticket-detail-page.html',
  styleUrl: './ticket-detail-page.css',
})
export class TicketDetailPage {
  private readonly api = inject(TicketsApi);
  private readonly tenant = inject(TenantContext);

  // Bound from the :id route parameter by withComponentInputBinding().
  readonly id = input.required<string>();

  private readonly ticketResource = this.api.ticket(() => this.id());

  protected readonly detail = computed(() =>
    this.ticketResource.hasValue() ? toTicketDetail(this.ticketResource.value()) : undefined,
  );
  protected readonly loading = this.ticketResource.isLoading;
  protected readonly notFound = computed(
    () => findApiError(this.ticketResource.error())?.kind === 'not-found',
  );
  protected readonly errorMessage = computed(() => {
    const error = this.ticketResource.error();
    return error === undefined ? undefined : describeApiError(error);
  });
  protected readonly tenantId = this.tenant.current;

  constructor() {
    // Each response is a new object, so every answer schedules the next poll; an id change, an error or a finished
    // ticket clears the pending timer through onCleanup, as does destroying the panel.
    effect((onCleanup) => {
      const detail = this.detail();
      const delay = detail === undefined ? undefined : pollDelaysMs[detail.state];
      if (delay === undefined) {
        return;
      }
      const timer = setTimeout(() => this.ticketResource.reload(), delay);
      onCleanup(() => clearTimeout(timer));
    });
  }

  protected reloadTicket(): void {
    this.ticketResource.reload();
  }
}

function toTicketDetail(ticket: Ticket): TicketDetail {
  const state = toTriageState(ticket);
  return {
    ticket,
    state,
    mailto: `mailto:${ticket.customerEmail}`,
    hasMessage: ticket.body.trim() !== '',
    failedAttempts: describeFailedAttempts(ticket.failedTriageAttempts),
    calloutClass: `mt-4 max-w-prose rounded-control px-3 py-2.5 text-sm ${calloutColors[state]}`,
  };
}

function describeFailedAttempts(count: number): string {
  if (count === 0) {
    return '';
  }
  return count === 1 ? '1 failed attempt' : `${count} failed attempts`;
}
