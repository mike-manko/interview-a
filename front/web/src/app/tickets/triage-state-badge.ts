import { Component, computed, input } from '@angular/core';
import { TriageState, triageStateLabels } from './triage-state';

const dotClasses: Readonly<Record<TriageState, string>> = {
  queued: 'border border-text-muted',
  classifying: 'bg-primary motion-safe:animate-pulse',
  retrying: 'bg-warning-text',
  triaged: 'bg-success',
  'gave-up': 'bg-danger',
};

const labelClasses: Readonly<Record<TriageState, string>> = {
  queued: 'text-text-muted',
  classifying: 'text-primary',
  retrying: 'text-warning-text',
  triaged: 'text-success-text',
  'gave-up': 'text-danger',
};

@Component({
  selector: 'app-triage-state-badge',
  template: `
    <span class="inline-flex items-center gap-1.5 text-xs font-medium whitespace-nowrap">
      <span [class]="dotClass()" aria-hidden="true"></span>
      <span [class]="labelClass()">{{ label() }}</span>
    </span>
  `,
})
export class TriageStateBadge {
  readonly state = input.required<TriageState>();

  protected readonly label = computed(() => triageStateLabels[this.state()]);
  protected readonly dotClass = computed(
    () => `inline-block size-2 shrink-0 rounded-full ${dotClasses[this.state()]}`,
  );
  protected readonly labelClass = computed(() => labelClasses[this.state()]);
}
