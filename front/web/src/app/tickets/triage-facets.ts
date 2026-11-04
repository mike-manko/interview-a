import { Component, computed, input, output } from '@angular/core';
import { TriageFilter } from './ticket-list-view';
import { TriageBucket, triageBucketLabels, triageBuckets } from './triage-bucket';

// A segmented control built from native radios: one choice, arrow keys move between options, no custom ARIA.
@Component({
  selector: 'app-triage-facets',
  template: `
    <fieldset>
      <legend class="sr-only">Triage state</legend>
      <div class="inline-flex flex-wrap gap-0.5 rounded-control bg-surface-sunken p-0.5">
        @for (option of options(); track option.value) {
          <label
            class="inline-flex h-8 cursor-pointer items-center gap-2 rounded-control px-3 text-sm font-medium text-text-muted transition-colors duration-150 ease-out-expo hover:text-text has-checked:bg-surface has-checked:text-text has-checked:shadow-card has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-primary"
          >
            <input
              type="radio"
              name="triage-facet"
              class="sr-only"
              [value]="option.value"
              [checked]="option.value === selected()"
              (change)="pick(option.value)"
            />
            {{ option.label }}
            <data class="text-xs text-text-muted" [value]="option.count">{{ option.count }}</data>
          </label>
        }
      </div>
    </fieldset>
  `,
})
export class TriageFacets {
  readonly selected = input.required<TriageFilter>();
  readonly counts = input.required<Readonly<Record<TriageBucket, number>>>();

  readonly picked = output<TriageFilter>();

  protected readonly options = computed(() => {
    const counts = this.counts();
    const total = triageBuckets.reduce((sum, bucket) => sum + counts[bucket], 0);
    return [
      { value: 'all' as const, label: 'All', count: total },
      ...triageBuckets.map((bucket) => ({
        value: bucket,
        label: triageBucketLabels[bucket],
        count: counts[bucket],
      })),
    ];
  });

  protected pick(filter: TriageFilter): void {
    this.picked.emit(filter);
  }
}
