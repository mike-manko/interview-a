import { Component, computed, input } from '@angular/core';
import { TicketPriority } from './ticket';

// Level 1-4 fills that many bars, so severity reads without relying on color alone.
const priorityLevels: Readonly<Record<TicketPriority, number>> = {
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
};

const priorityBarColors: Readonly<Record<TicketPriority, string>> = {
  Low: 'bg-priority-low',
  Medium: 'bg-priority-medium',
  High: 'bg-priority-high',
  Critical: 'bg-priority-critical',
};

const barHeights = ['h-1.5', 'h-2', 'h-2.5', 'h-3'];

@Component({
  selector: 'app-priority-badge',
  template: `
    @if (priority(); as priority) {
      <span class="inline-flex items-center gap-1.5 text-xs whitespace-nowrap">
        <span class="inline-flex h-3 items-end gap-px" aria-hidden="true">
          @for (bar of bars(); track $index) {
            <span [class]="bar"></span>
          }
        </span>
        <span [class]="labelClass()">{{ priority }}</span>
      </span>
    } @else {
      <span class="text-xs whitespace-nowrap text-text-muted">No priority yet</span>
    }
  `,
})
export class PriorityBadge {
  readonly priority = input.required<TicketPriority | null>();

  protected readonly bars = computed(() => {
    const priority = this.priority();
    const level = priority === null ? 0 : priorityLevels[priority];
    const fill = priority === null ? '' : priorityBarColors[priority];
    return barHeights.map((height, index) => {
      const color = index < level ? fill : 'bg-border';
      return `w-[3px] rounded-[1px] ${height} ${color}`;
    });
  });

  protected readonly labelClass = computed(() =>
    this.priority() === 'Critical' ? 'font-semibold text-danger' : 'font-medium text-text',
  );
}
