import { Component, computed, input, output } from '@angular/core';
import { PriorityBadge } from './priority-badge';
import { ticketCategories, TicketCategory, TicketPriority, ticketPriorities } from './ticket';
import { filtersClassification } from './ticket-query';
import {
  hasFilters,
  TicketGrouping,
  ticketGroupings,
  TicketListView,
  withoutFilters,
} from './ticket-list-view';

const groupingLabels: Readonly<Record<TicketGrouping, string>> = {
  priority: 'Priority',
  category: 'Category',
  triage: 'Triage state',
  none: 'No grouping',
};

// The queue's toolbar: every control emits the whole next view, and the page puts it in the URL.
@Component({
  selector: 'app-ticket-filters',
  imports: [PriorityBadge],
  templateUrl: './ticket-filters.html',
})
export class TicketFilters {
  readonly view = input.required<TicketListView>();

  readonly changed = output<TicketListView>();

  protected readonly groupingOptions = ticketGroupings.map((grouping) => ({
    value: grouping,
    label: groupingLabels[grouping],
  }));
  protected readonly categoryChips = computed(() =>
    ticketCategories.map((category) => ({
      value: category,
      pressed: this.view().query.categories.includes(category),
    })),
  );
  protected readonly priorityChips = computed(() =>
    ticketPriorities.map((priority) => ({
      value: priority,
      pressed: this.view().query.priorities.includes(priority),
    })),
  );
  protected readonly filtered = computed(() => hasFilters(this.view()));
  protected readonly hidesUnclassified = computed(() => filtersClassification(this.view().query));

  protected searchFor(search: string): void {
    this.changed.emit({ ...this.view(), search });
  }

  protected toggleCategory(category: TicketCategory): void {
    const query = this.view().query;
    const categories = toggled(ticketCategories, query.categories, category);
    this.changed.emit({ ...this.view(), query: { ...query, categories } });
  }

  protected togglePriority(priority: TicketPriority): void {
    const query = this.view().query;
    const priorities = toggled(ticketPriorities, query.priorities, priority);
    this.changed.emit({ ...this.view(), query: { ...query, priorities } });
  }

  protected showHidden(includeHidden: boolean): void {
    const query = this.view().query;
    this.changed.emit({ ...this.view(), query: { ...query, includeHidden } });
  }

  protected groupBy(value: string): void {
    const grouping = ticketGroupings.find((option) => option === value);
    if (grouping !== undefined) {
      this.changed.emit({ ...this.view(), grouping });
    }
  }

  protected clearFilters(): void {
    this.changed.emit(withoutFilters(this.view()));
  }
}

// Keeps the canonical order, so the URL reads the same whatever order the chips were pressed in.
function toggled<T>(options: readonly T[], selected: readonly T[], value: T): readonly T[] {
  if (selected.includes(value)) {
    return selected.filter((option) => option !== value);
  }
  return options.filter((option) => option === value || selected.includes(option));
}
