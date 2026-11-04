import { Pipe, PipeTransform } from '@angular/core';
import { Ticket } from './ticket';

// Seed and customer tickets may arrive without a subject; a blank link or heading is unusable.
@Pipe({ name: 'ticketSubject' })
export class TicketSubject implements PipeTransform {
  transform(ticket: Pick<Ticket, 'subject'>): string {
    const subject = ticket.subject.trim();
    return subject === '' ? '(no subject)' : subject;
  }
}
