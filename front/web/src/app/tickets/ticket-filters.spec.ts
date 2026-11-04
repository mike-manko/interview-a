import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { TicketFilters } from './ticket-filters';
import { TicketListView } from './ticket-list-view';
import { anyTicket } from './ticket-query';

const defaultView: TicketListView = {
  query: anyTicket,
  grouping: 'priority',
  triage: 'all',
  search: '',
};

@Component({
  imports: [TicketFilters],
  template: `<app-ticket-filters [view]="view()" (changed)="changes.push($event)" />`,
})
class Host {
  readonly view = signal<TicketListView>(defaultView);
  readonly changes: TicketListView[] = [];
}

describe('TicketFilters', () => {
  let fixture: ComponentFixture<Host>;
  let element: HTMLElement;

  async function render(view: TicketListView = defaultView): Promise<void> {
    fixture = TestBed.createComponent(Host);
    fixture.componentInstance.view.set(view);
    element = fixture.nativeElement;
    await fixture.whenStable();
  }

  function chip(group: string, name: string): HTMLButtonElement {
    const button = [
      ...element.querySelectorAll<HTMLButtonElement>(`[aria-labelledby="${group}"] button`),
    ].find((candidate) => candidate.textContent?.trim() === name);
    if (button === undefined) {
      throw new Error(`chip ${name} missing`);
    }
    return button;
  }

  function lastChange(): TicketListView | undefined {
    return fixture.componentInstance.changes.at(-1);
  }

  it('offers every category and priority as a toggle chip in a labelled group', async () => {
    await render({ ...defaultView, query: { ...anyTicket, categories: ['Billing'] } });

    const categories = element.querySelectorAll('[aria-labelledby="category-chips-label"] button');
    const priorities = element.querySelectorAll('[aria-labelledby="priority-chips-label"] button');
    expect([...categories].map((button) => button.textContent?.trim())).toEqual([
      'Billing',
      'Technical',
      'Account',
      'Shipping',
      'Other',
    ]);
    expect([...priorities].map((button) => button.textContent?.trim())).toEqual([
      'Critical',
      'High',
      'Medium',
      'Low',
    ]);
    expect(chip('category-chips-label', 'Billing').getAttribute('aria-pressed')).toBe('true');
    expect(chip('category-chips-label', 'Account').getAttribute('aria-pressed')).toBe('false');
    await expectAccessible(element);
  });

  it('adds a category and keeps the canonical order', async () => {
    await render({ ...defaultView, query: { ...anyTicket, categories: ['Shipping'] } });

    chip('category-chips-label', 'Billing').click();

    expect(lastChange()?.query.categories).toEqual(['Billing', 'Shipping']);
  });

  it('removes a pressed priority', async () => {
    await render({ ...defaultView, query: { ...anyTicket, priorities: ['Critical', 'High'] } });

    chip('priority-chips-label', 'Critical').click();

    expect(lastChange()?.query.priorities).toEqual(['High']);
  });

  it('emits the search text as it is typed', async () => {
    await render();

    const search = element.querySelector<HTMLInputElement>('#ticket-search');
    if (search === null) {
      throw new Error('search missing');
    }
    expect(search.labels?.[0]?.textContent?.trim()).toBe('Search loaded tickets');
    search.value = 'anna';
    search.dispatchEvent(new Event('input'));

    expect(lastChange()).toEqual({ ...defaultView, search: 'anna' });
  });

  it('includes the tickets triage gave up on when the checkbox is ticked', async () => {
    await render();

    const checkbox = element.querySelector<HTMLInputElement>('input[type="checkbox"]');
    if (checkbox === null) {
      throw new Error('checkbox missing');
    }
    expect(checkbox.labels?.[0]?.textContent?.trim()).toBe('Show tickets triage gave up on');
    checkbox.click();

    expect(lastChange()?.query.includeHidden).toBe(true);
  });

  it('changes the grouping from the Group by select', async () => {
    await render();

    const select = element.querySelector<HTMLSelectElement>('#ticket-grouping');
    if (select === null) {
      throw new Error('select missing');
    }
    expect(select.value).toBe('priority');
    select.value = 'triage';
    select.dispatchEvent(new Event('change'));

    expect(lastChange()?.grouping).toBe('triage');
  });

  it('warns that unclassified tickets are hidden while a classification filter is on', async () => {
    await render();
    expect(element.textContent).not.toContain('Tickets without a category or priority yet');

    fixture.componentInstance.view.set({
      ...defaultView,
      query: { ...anyTicket, priorities: ['Low'] },
    });
    await fixture.whenStable();

    expect(element.textContent).toContain(
      'Tickets without a category or priority yet are hidden while these filters are on.',
    );
    await expectAccessible(element);
  });

  it('offers Clear filters only when a filter is set, and keeps grouping and hidden tickets', async () => {
    await render({
      ...defaultView,
      grouping: 'none',
      query: { ...anyTicket, includeHidden: true },
    });
    expect(element.textContent).not.toContain('Clear filters');

    fixture.componentInstance.view.set({
      grouping: 'none',
      triage: 'attention',
      search: 'anna',
      query: { categories: ['Billing'], priorities: ['High'], includeHidden: true },
    });
    await fixture.whenStable();
    const clear = [...element.querySelectorAll('button')].find(
      (button) => button.textContent?.trim() === 'Clear filters',
    );
    clear?.click();

    expect(lastChange()).toEqual({
      grouping: 'none',
      triage: 'all',
      search: '',
      query: { categories: [], priorities: [], includeHidden: true },
    });
  });
});
