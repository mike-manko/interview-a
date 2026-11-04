import { TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { PriorityBadge } from './priority-badge';

describe('PriorityBadge', () => {
  it('fills all four bars for a critical priority and names it', async () => {
    const fixture = TestBed.createComponent(PriorityBadge);
    fixture.componentRef.setInput('priority', 'Critical');
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.textContent?.trim()).toBe('Critical');
    expect(element.querySelectorAll('.bg-priority-critical')).toHaveLength(4);
    await expectAccessible(element);
  });

  it('fills one bar for a low priority', async () => {
    const fixture = TestBed.createComponent(PriorityBadge);
    fixture.componentRef.setInput('priority', 'Low');
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelectorAll('.bg-priority-low')).toHaveLength(1);
    expect(element.querySelectorAll('.bg-border')).toHaveLength(3);
  });

  it('says so when there is no priority yet', async () => {
    const fixture = TestBed.createComponent(PriorityBadge);
    fixture.componentRef.setInput('priority', null);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent.trim()).toBe('No priority yet');
    await expectAccessible(fixture.nativeElement);
  });
});
