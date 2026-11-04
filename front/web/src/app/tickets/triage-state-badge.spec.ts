import { TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { TriageStateBadge } from './triage-state-badge';

describe('TriageStateBadge', () => {
  it('names the state and colours its dot', async () => {
    const fixture = TestBed.createComponent(TriageStateBadge);
    fixture.componentRef.setInput('state', 'gave-up');
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.textContent?.trim()).toBe('Triage gave up');
    expect(element.querySelector('.bg-danger')).not.toBeNull();
    await expectAccessible(element);
  });

  it('pulses the dot only while classifying', async () => {
    const fixture = TestBed.createComponent(TriageStateBadge);
    fixture.componentRef.setInput('state', 'classifying');
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[class*="animate-pulse"]')).not.toBeNull();
  });
});
