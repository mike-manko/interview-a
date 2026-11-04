import { Ticket, ticketCategories, TicketPriority, ticketPriorities } from './ticket';
import { TicketGrouping } from './ticket-list-view';
import { toTriageBucket, TriageBucket, triageBucketLabels, triageBuckets } from './triage-bucket';

// The color of a group's rail: priority severity, triage state, or neutral where the group carries no urgency.
export type GroupTone =
  'critical' | 'high' | 'medium' | 'low' | 'danger' | 'primary' | 'success' | 'neutral';

export interface TicketGroup {
  readonly key: string;
  readonly label: string;
  readonly tone: GroupTone;
  readonly tickets: readonly Ticket[];
}

const priorityTones: Readonly<Record<TicketPriority, GroupTone>> = {
  Critical: 'critical',
  High: 'high',
  Medium: 'medium',
  Low: 'low',
};

const triageTones: Readonly<Record<TriageBucket, GroupTone>> = {
  attention: 'danger',
  'in-triage': 'primary',
  triaged: 'success',
};

const unclassifiedLabel = 'Not classified yet';

// Groups come in working order (most urgent first), empty groups are left out, and each group keeps the API's
// newest-first order.
export function groupTickets(
  tickets: readonly Ticket[],
  grouping: TicketGrouping,
): readonly TicketGroup[] {
  return allGroups(tickets, grouping).filter((group) => group.tickets.length > 0);
}

function allGroups(tickets: readonly Ticket[], grouping: TicketGrouping): readonly TicketGroup[] {
  switch (grouping) {
    case 'priority':
      return [
        ...ticketPriorities.map((priority): TicketGroup => ({
          key: priority,
          label: priority,
          tone: priorityTones[priority],
          tickets: tickets.filter((ticket) => ticket.priority === priority),
        })),
        unclassifiedGroup(tickets.filter((ticket) => ticket.priority === null)),
      ];
    case 'category':
      return [
        ...ticketCategories.map((category): TicketGroup => ({
          key: category,
          label: category,
          tone: 'neutral',
          tickets: tickets.filter((ticket) => ticket.category === category),
        })),
        unclassifiedGroup(tickets.filter((ticket) => ticket.category === null)),
      ];
    case 'triage':
      return triageBuckets.map((bucket): TicketGroup => ({
        key: bucket,
        label: triageBucketLabels[bucket],
        tone: triageTones[bucket],
        tickets: tickets.filter((ticket) => toTriageBucket(ticket) === bucket),
      }));
    case 'none':
      return [{ key: 'all', label: 'All tickets', tone: 'neutral', tickets }];
    default:
      return grouping satisfies never;
  }
}

function unclassifiedGroup(tickets: readonly Ticket[]): TicketGroup {
  return { key: 'unclassified', label: unclassifiedLabel, tone: 'neutral', tickets };
}
