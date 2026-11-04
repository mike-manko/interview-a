import { isInTriage, needsAttention, Ticket } from './ticket';

// What an agent has to do with a ticket. Every ticket falls in exactly one bucket, so the counts add up to the queue.
export type TriageBucket = 'attention' | 'in-triage' | 'triaged';

export const triageBuckets: readonly TriageBucket[] = ['attention', 'in-triage', 'triaged'];

export const triageBucketLabels: Readonly<Record<TriageBucket, string>> = {
  attention: 'Needs attention',
  'in-triage': 'In triage',
  triaged: 'Triaged',
};

// A failed attempt wins over "still in triage": the ticket may stay unclassified, so an agent should look at it.
export function toTriageBucket(ticket: Ticket): TriageBucket {
  if (needsAttention(ticket)) {
    return 'attention';
  }
  if (isInTriage(ticket)) {
    return 'in-triage';
  }
  return 'triaged';
}

export function countTriageBuckets(
  tickets: readonly Ticket[],
): Readonly<Record<TriageBucket, number>> {
  const counts = { attention: 0, 'in-triage': 0, triaged: 0 };
  for (const ticket of tickets) {
    counts[toTriageBucket(ticket)] += 1;
  }
  return counts;
}
