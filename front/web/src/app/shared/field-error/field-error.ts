import { Component, computed, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';

// The host element always renders, so an input's aria-describedby never points at a missing id.
@Component({
  selector: 'app-field-error',
  host: { class: 'block text-sm text-danger', 'aria-live': 'polite' },
  template: `
    @if (visible()) {
      @for (message of messages(); track message) {
        <p>{{ message }}</p>
      }
    }
  `,
})
export class FieldError {
  readonly field = input.required<FieldTree<string>>();

  private readonly state = computed(() => this.field()());
  protected readonly visible = computed(() => this.state().touched() && this.state().invalid());
  protected readonly messages = computed(() =>
    this.state()
      .errors()
      .map((error) => error.message ?? 'This field is not valid.'),
  );
}
