import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CategoryBadge } from './category-badge';
import { GroupTone, TicketGroup } from './group-tickets';
import { PriorityBadge } from './priority-badge';
import { ticketCountLabel } from './ticket-count-label';
import { TicketGrouping } from './ticket-list-view';
import { TicketSubject } from './ticket-subject';
import { toTriageState } from './triage-state';
import { TriageStateBadge } from './triage-state-badge';

// The rail is the one place severity color is used at group level (DESIGN.md "Group headers").
const railColors: Readonly<Record<GroupTone, string>> = {
  critical: 'bg-priority-critical',
  high: 'bg-priority-high',
  medium: 'bg-priority-medium',
  low: 'bg-priority-low',
  danger: 'bg-danger',
  primary: 'bg-primary',
  success: 'bg-success',
  neutral: 'bg-border-strong',
};

const captions: Readonly<Record<TicketGrouping, string>> = {
  priority: 'Tickets grouped by priority, most urgent first, newest first within each group',
  category: 'Tickets grouped by category, newest first within each group',
  triage: 'Tickets grouped by triage state, newest first within each group',
  none: 'Tickets, newest first',
};

// One table with a tbody per group, so screen readers announce the group with every row. Below md the rows stack,
// subject first, instead of scrolling sideways.
@Component({
  selector: 'app-ticket-queue-table',
  imports: [
    CategoryBadge,
    DatePipe,
    PriorityBadge,
    RouterLink,
    RouterLinkActive,
    TicketSubject,
    TriageStateBadge,
  ],
  templateUrl: './ticket-queue-table.html',
})
export class TicketQueueTable {
  readonly groups = input.required<readonly TicketGroup[]>();
  readonly grouping = input.required<TicketGrouping>();

  protected readonly caption = computed(() => captions[this.grouping()]);
  protected readonly showGroupHeaders = computed(() => this.grouping() !== 'none');
  protected readonly rows = computed(() =>
    this.groups().map((group) => ({
      key: group.key,
      label: group.label,
      railClass: `h-4 w-0.75 shrink-0 rounded-full ${railColors[group.tone]}`,
      count: group.tickets.length,
      countLabel: ticketCountLabel(group.tickets.length),
      tickets: group.tickets.map((ticket) => ({ ticket, triageState: toTriageState(ticket) })),
    })),
  );
}
