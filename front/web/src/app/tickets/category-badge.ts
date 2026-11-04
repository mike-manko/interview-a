import { Component, input } from '@angular/core';
import { TicketCategory } from './ticket';

@Component({
  selector: 'app-category-badge',
  template: `
    @if (category(); as category) {
      <span
        class="inline-block rounded-control bg-surface-sunken px-1.5 py-0.5 text-xs font-medium whitespace-nowrap text-text"
      >
        {{ category }}
      </span>
    } @else {
      <span class="text-xs whitespace-nowrap text-text-muted">No category yet</span>
    }
  `,
})
export class CategoryBadge {
  readonly category = input.required<TicketCategory | null>();
}
