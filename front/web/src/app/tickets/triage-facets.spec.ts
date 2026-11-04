import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { TriageFilter } from './ticket-list-view';
import { TriageFacets } from './triage-facets';

@Component({
  imports: [TriageFacets],
  template: `
    <app-triage-facets
      [selected]="selected()"
      [counts]="{ attention: 2, 'in-triage': 5, triaged: 4 }"
      (picked)="picks.push($event)"
    />
  `,
})
class Host {
  readonly selected = signal<TriageFilter>('all');
  readonly picks: TriageFilter[] = [];
}

describe('TriageFacets', () => {
  it('offers All and each triage state with counts that add up', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    const labels = [...element.querySelectorAll('label')].map((label) =>
      label.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(labels).toEqual(['All 11', 'Needs attention 2', 'In triage 5', 'Triaged 4']);
    expect(element.querySelector('legend')?.textContent).toBe('Triage state');
    await expectAccessible(element);
  });

  it('checks the selected state', async () => {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.selected.set('in-triage');
    await fixture.whenStable();

    const checked = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input:checked',
    );
    expect(checked?.value).toBe('in-triage');
  });

  it('emits the state that is picked', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    const attention = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[value="attention"]',
    );
    attention?.click();

    expect(fixture.componentInstance.picks).toEqual(['attention']);
  });
});
