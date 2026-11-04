import { Ticket } from './ticket';

// Client-side search over the tickets already loaded: agents find a ticket by what a customer or colleague quotes.
export function matchesSearch(ticket: Ticket, search: string): boolean {
  const needle = search.trim().toLowerCase();
  if (needle === '') {
    return true;
  }
  return [ticket.subject, ticket.customerEmail, ticket.id].some((text) =>
    text.toLowerCase().includes(needle),
  );
}
