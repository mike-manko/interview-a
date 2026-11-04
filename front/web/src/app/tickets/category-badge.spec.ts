import { TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { CategoryBadge } from './category-badge';

describe('CategoryBadge', () => {
  it('shows the category', async () => {
    const fixture = TestBed.createComponent(CategoryBadge);
    fixture.componentRef.setInput('category', 'Billing');
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent.trim()).toBe('Billing');
    await expectAccessible(fixture.nativeElement);
  });

  it('says so when there is no category yet', async () => {
    const fixture = TestBed.createComponent(CategoryBadge);
    fixture.componentRef.setInput('category', null);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent.trim()).toBe('No category yet');
    await expectAccessible(fixture.nativeElement);
  });
});
