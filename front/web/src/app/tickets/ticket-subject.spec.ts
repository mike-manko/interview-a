import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TicketSubject } from './ticket-subject';

@Component({
  imports: [TicketSubject],
  template: `<p>{{ ticket() | ticketSubject }}</p>`,
})
class Host {
  readonly ticket = signal({ subject: 'Charged twice' });
}

describe('TicketSubject', () => {
  it('renders the subject', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toBe('Charged twice');
  });

  it('renders a placeholder for a blank subject', async () => {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.ticket.set({ subject: '   ' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toBe('(no subject)');
  });
});
